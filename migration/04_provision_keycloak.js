#!/usr/bin/env node
// Phase 3: Provision Keycloak users from the migrated Members table.
//
// For members WITH a Koala user account (encrypted_password not null, deleted_at null):
//   → creates Keycloak user with legacy_bcrypt_hash attribute
//   → user logs in with old password → auto-migrates to PBKDF2 on first login
//
// For members WITHOUT a Koala user account (~1,113):
//   → creates Keycloak user with UPDATE_PASSWORD required action
//   → member uses "Forgot password" flow on first login
//
// After provisioning, writes Keycloak UUIDs back to Members.AuthSystemUserId.

'use strict';

const { Client } = require('pg');
const https = require('https');
const http = require('http');

const KC_URL = 'http://tavern-keycloak:8080';
const KC_CLIENT_ID = 'backend-tavern';
const KC_CLIENT_SECRET = process.env.KeycloakClientSecret || (() => { throw new Error('KeycloakClientSecret env var is required'); })();
const REALM = 'master';
const BATCH_SIZE = 50;
const DELAY_MS = 50; // small delay between batches to avoid overwhelming Keycloak

const DB_CONFIG = {
    host: 'db', port: 5432,
    database: 'postgres', user: 'postgres', password: 'postgres'
};

// ── HTTP helpers ──────────────────────────────────────────────────────────────

function request(method, url, body, token) {
    return new Promise((resolve, reject) => {
        const parsed = new URL(url);
        const options = {
            hostname: parsed.hostname,
            port: parsed.port || (parsed.protocol === 'https:' ? 443 : 80),
            path: parsed.pathname + parsed.search,
            method,
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        };
        const bodyStr = body ? JSON.stringify(body) : undefined;
        if (bodyStr) options.headers['Content-Length'] = Buffer.byteLength(bodyStr);

        const lib = parsed.protocol === 'https:' ? https : http;
        const req = lib.request(options, (res) => {
            let data = '';
            res.on('data', c => data += c);
            res.on('end', () => {
                const json = data ? (() => { try { return JSON.parse(data); } catch { return data; } })() : null;
                resolve({ status: res.statusCode, body: json, headers: res.headers });
            });
        });
        req.on('error', reject);
        if (bodyStr) req.write(bodyStr);
        req.end();
    });
}

function formPost(url, params) {
    return new Promise((resolve, reject) => {
        const parsed = new URL(url);
        const body = Object.entries(params).map(([k,v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&');
        const options = {
            hostname: parsed.hostname,
            port: parsed.port || 80,
            path: parsed.pathname,
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'Content-Length': Buffer.byteLength(body) }
        };
        const lib = parsed.protocol === 'https:' ? https : http;
        const req = lib.request(options, (res) => {
            let data = '';
            res.on('data', c => data += c);
            res.on('end', () => resolve({ status: res.statusCode, body: JSON.parse(data) }));
        });
        req.on('error', reject);
        req.write(body);
        req.end();
    });
}

async function getToken() {
    const res = await formPost(`${KC_URL}/realms/${REALM}/protocol/openid-connect/token`, {
        grant_type: 'client_credentials',
        client_id: KC_CLIENT_ID,
        client_secret: KC_CLIENT_SECRET
    });
    if (!res.body.access_token) throw new Error(`Token error: ${JSON.stringify(res.body)}`);
    return res.body.access_token;
}

async function createKeycloakUser(token, { email, firstName, lastName, bcryptHash, tavernUuid, isAdmin }) {
    const attributes = {
        koala_user_id: [tavernUuid],
        access_level: ['paid'],
        is_admin: [isAdmin ? 'true' : 'false'],
        ...(bcryptHash ? { legacy_bcrypt_hash: [bcryptHash] } : {})
    };
    const payload = {
        username: email,
        email,
        firstName: firstName || '',
        lastName: lastName || '',
        emailVerified: true,
        enabled: true,
        attributes,
        ...(!bcryptHash ? { requiredActions: ['UPDATE_PASSWORD'], emailVerified: false } : {})
    };

    const res = await request('POST', `${KC_URL}/admin/realms/${REALM}/users`, payload, token);
    if (res.status === 201) {
        // Location header: .../users/{uuid}
        return res.headers.location.split('/').pop();
    }
    if (res.status === 409) {
        // Already exists — look it up and ensure koala_user_id and is_admin are up to date
        const search = await request('GET', `${KC_URL}/admin/realms/${REALM}/users?email=${encodeURIComponent(email)}&exact=true`, null, token);
        if (search.body && search.body.length > 0) {
            const existing = search.body[0];
            const currentKoalaId = (existing.attributes?.koala_user_id ?? [])[0];
            const currentIsAdmin = (existing.attributes?.is_admin ?? [])[0];
            const wantIsAdmin = isAdmin ? 'true' : 'false';
            if (currentKoalaId !== tavernUuid || currentIsAdmin !== wantIsAdmin) {
                const updatedAttrs = { ...(existing.attributes || {}), koala_user_id: [tavernUuid], access_level: ['paid'], is_admin: [wantIsAdmin] };
                await request('PUT', `${KC_URL}/admin/realms/${REALM}/users/${existing.id}`, { ...existing, attributes: updatedAttrs }, token);
            }
            return existing.id;
        }
    }
    throw new Error(`Create user ${email} failed: HTTP ${res.status} ${JSON.stringify(res.body)}`);
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

// ── Main ──────────────────────────────────────────────────────────────────────

async function main() {
    const db = new Client(DB_CONFIG);
    await db.connect();

    // Determine admin group IDs and current board year
    const { rows: settingsRows } = await db.query(`
        SELECT "Name", "Value" FROM "Settings"
        WHERE "Name" IN ('BoardGroupId', 'CandidateBoardGroupId', 'AdminGroupIds')
    `);
    const settings = Object.fromEntries(settingsRows.map(r => [r.Name, r.Value]));
    const boardGroupId = parseInt(settings['BoardGroupId'] || '1');
    const candidateBoardGroupId = parseInt(settings['CandidateBoardGroupId'] || '2');
    const extraAdminGroupIds = (settings['AdminGroupIds'] || '').split(',').map(s => parseInt(s.trim())).filter(id => !isNaN(id) && id > 0);
    const allAdminGroupIds = [boardGroupId, candidateBoardGroupId, ...extraAdminGroupIds];

    // Board year: on or after Aug 1 → currentYear+1, else → currentYear
    const now = new Date();
    const boardYear = (now.getMonth() + 1 > 8 || (now.getMonth() + 1 === 8 && now.getDate() >= 1))
        ? now.getFullYear() + 1 : now.getFullYear();

    const { rows: adminRows } = await db.query(
        `SELECT DISTINCT "MemberId"::text FROM "GroupMemberships" WHERE "GroupId" = ANY($1) AND "MembershipYear" = $2`,
        [allAdminGroupIds, boardYear]
    );
    const adminMemberIds = new Set(adminRows.map(r => r.MemberId));
    console.log(`Admin groups: ${allAdminGroupIds.join(', ')} | Board year: ${boardYear} | Admins: ${adminMemberIds.size}`);

    console.log('Fetching members to provision...');
    const { rows } = await db.query(`
        SELECT
            m."Id"          AS tavern_uuid,
            m."Email"       AS email,
            m."FirstName"   AS first_name,
            m."LastName"    AS last_name,
            u.encrypted_password AS bcrypt_hash
        FROM "Members" m
        LEFT JOIN koala.users u
            ON u.credentials_type = 'Member'
           AND u.credentials_id = (
               SELECT km.id FROM koala.members km WHERE km.email = m."Email" LIMIT 1
           )
           AND u.deleted_at IS NULL
        WHERE m."AuthSystemUserId" IS NULL
          AND m."Email" != 'bestuur@svsticky.nl'
        ORDER BY m."Email"
    `);

    console.log(`Found ${rows.length} members to provision.`);

    let token = await getToken();
    let tokenRefreshAt = Date.now() + 55_000; // refresh every 55 seconds

    let created = 0, failed = 0, withBcrypt = 0, withoutPassword = 0;
    const errors = [];

    for (let i = 0; i < rows.length; i += BATCH_SIZE) {
        const batch = rows.slice(i, i + BATCH_SIZE);

        // Refresh token before it expires
        if (Date.now() > tokenRefreshAt) {
            token = await getToken();
            tokenRefreshAt = Date.now() + 55_000;
        }

        await Promise.all(batch.map(async (row) => {
            try {
                const kcId = await createKeycloakUser(token, {
                    email: row.email,
                    firstName: row.first_name,
                    lastName: row.last_name,
                    bcryptHash: row.bcrypt_hash || null,
                    tavernUuid: row.tavern_uuid,
                    isAdmin: adminMemberIds.has(row.tavern_uuid)
                });

                await db.query(
                    `UPDATE "Members" SET "AuthSystemUserId" = $1 WHERE "Id" = $2`,
                    [kcId, row.tavern_uuid]
                );

                created++;
                if (row.bcrypt_hash) withBcrypt++; else withoutPassword++;
            } catch (err) {
                failed++;
                errors.push(`${row.email}: ${err.message}`);
            }
        }));

        if ((i + BATCH_SIZE) % 500 === 0 || i + BATCH_SIZE >= rows.length) {
            console.log(`  Progress: ${Math.min(i + BATCH_SIZE, rows.length)}/${rows.length} (${created} ok, ${failed} failed)`);
        }

        if (DELAY_MS > 0) await sleep(DELAY_MS);
    }

    console.log('\n=== Keycloak provisioning complete ===');
    console.log(`  Created: ${created} (${withBcrypt} with bcrypt hash, ${withoutPassword} with UPDATE_PASSWORD)`);
    console.log(`  Failed:  ${failed}`);

    if (errors.length > 0) {
        console.log('\nErrors:');
        errors.slice(0, 20).forEach(e => console.log(' ', e));
        if (errors.length > 20) console.log(`  ... and ${errors.length - 20} more`);
    }

    // Final count
    const { rows: [{ count }] } = await db.query(`SELECT COUNT(*) FROM "Members" WHERE "AuthSystemUserId" IS NOT NULL`);
    console.log(`\nMembers with AuthSystemUserId: ${count}`);

    await db.end();
}

main().catch(err => { console.error(err); process.exit(1); });
