#!/usr/bin/env node
// Fixes stale koala_user_id attributes in Keycloak for members whose Keycloak
// user was not re-created during a full migration reset.
//
// Run this after 04_provision_keycloak.js if the /account page returns 404
// (symptom: JWT UserId doesn't match any Members.Id in the database).

'use strict';

const { Client } = require('pg');
const https = require('https');
const http = require('http');

const KC_URL = 'http://tavern-keycloak:8080';
const KC_CLIENT_ID = 'backend-tavern';
const KC_CLIENT_SECRET = process.env.KeycloakClientSecret || (() => { throw new Error('KeycloakClientSecret env var is required'); })();
const REALM = 'master';

const DB_CONFIG = {
    host: 'db', port: 5432,
    database: 'postgres', user: 'postgres', password: 'postgres'
};

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
        const body = Object.entries(params).map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&');
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

async function ensureIsAdminAttribute(token) {
    const res = await request('GET', `${KC_URL}/admin/realms/${REALM}/users/profile`, null, token);
    const profile = res.body;
    if (profile.attributes?.some(a => a.name === 'is_admin')) return;
    profile.attributes = profile.attributes || [];
    profile.attributes.push({
        name: 'is_admin',
        displayName: 'is_admin',
        validations: {},
        annotations: {},
        permissions: { view: ['admin'], edit: ['admin'] },
        multivalued: false
    });
    const put = await request('PUT', `${KC_URL}/admin/realms/${REALM}/users/profile`, profile, token);
    if (put.status !== 200) throw new Error(`Failed to add is_admin to user profile: HTTP ${put.status}`);
    console.log('Added is_admin attribute to Keycloak user profile schema.');
}

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

    const now = new Date();
    const boardYear = (now.getMonth() + 1 > 8 || (now.getMonth() + 1 === 8 && now.getDate() >= 1))
        ? now.getFullYear() + 1 : now.getFullYear();

    const { rows: adminRows } = await db.query(
        `SELECT DISTINCT "MemberId"::text FROM "GroupMemberships" WHERE "GroupId" = ANY($1) AND "MembershipYear" = $2`,
        [allAdminGroupIds, boardYear]
    );
    const adminMemberIds = new Set(adminRows.map(r => r.MemberId));
    console.log(`Admin groups: ${allAdminGroupIds.join(', ')} | Board year: ${boardYear} | Admins: ${adminMemberIds.size}`);

    const { rows } = await db.query(`
        SELECT "Id"::text AS tavern_uuid, "Email" AS email, "AuthSystemUserId"::text AS kc_id
        FROM "Members"
        WHERE "AuthSystemUserId" IS NOT NULL
        ORDER BY "Email"
    `);

    console.log(`Checking ${rows.length} members with Keycloak IDs...`);

    const token = await getToken();
    await ensureIsAdminAttribute(token);
    let fixed = 0, ok = 0, failed = 0;

    for (const row of rows) {
        try {
            const res = await request('GET', `${KC_URL}/admin/realms/${REALM}/users/${row.kc_id}`, null, token);
            if (res.status === 404) {
                console.warn(`  MISSING in Keycloak: ${row.email} (KC ID ${row.kc_id})`);
                failed++;
                continue;
            }
            if (res.status !== 200) {
                console.warn(`  Error fetching ${row.email}: HTTP ${res.status}`);
                failed++;
                continue;
            }

            const kcUser = res.body;
            const currentKoalaId = (kcUser.attributes?.koala_user_id ?? [])[0];
            const currentIsAdmin = (kcUser.attributes?.is_admin ?? [])[0];
            const wantIsAdmin = adminMemberIds.has(row.tavern_uuid) ? 'true' : 'false';

            if (currentKoalaId === row.tavern_uuid && currentIsAdmin === wantIsAdmin) {
                ok++;
                continue;
            }

            const changes = [];
            if (currentKoalaId !== row.tavern_uuid) changes.push(`koala_user_id ${currentKoalaId ?? '(unset)'} → ${row.tavern_uuid}`);
            if (currentIsAdmin !== wantIsAdmin) changes.push(`is_admin ${currentIsAdmin ?? '(unset)'} → ${wantIsAdmin}`);
            console.log(`  Fixing ${row.email}: ${changes.join(', ')}`);

            const updatedAttrs = { ...(kcUser.attributes || {}), koala_user_id: [row.tavern_uuid], is_admin: [wantIsAdmin] };
            const putRes = await request('PUT', `${KC_URL}/admin/realms/${REALM}/users/${row.kc_id}`,
                { ...kcUser, attributes: updatedAttrs }, token);
            if (putRes.status === 204) {
                fixed++;
            } else {
                console.error(`  Failed to update ${row.email}: HTTP ${putRes.status} ${JSON.stringify(putRes.body)}`);
                failed++;
            }
        } catch (err) {
            console.error(`  Error processing ${row.email}: ${err.message}`);
            failed++;
        }
    }

    console.log(`\nDone. ${ok} already correct, ${fixed} fixed, ${failed} errors.`);
    if (fixed > 0) console.log('Log out and log back in to get a fresh JWT with the updated is_admin claim.');

    await db.end();
}

main().catch(err => { console.error(err); process.exit(1); });
