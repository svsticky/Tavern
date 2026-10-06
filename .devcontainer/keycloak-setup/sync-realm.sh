#!/bin/bash
# Keycloak's `--import-realm` only imports a realm the first time it sees it - once the
# "tavern" realm exists in the (persistent) postgres-data volume, edits to realm-export.json
# (new clients, new protocol mappers, changed mapper config, etc.) are silently ignored on
# every later container start/rebuild. This script re-applies just the `clients` section of
# realm-export.json (including their protocolMappers) via Keycloak's Admin REST API
# partialImport endpoint with ifResourceExists=OVERWRITE, so client config always converges
# on what's in the repo - regardless of what was already imported previously.
#
# Runs as the devcontainer's postStartCommand, so it fires on every start, not just the first.

KEYCLOAK_URL="http://keycloak:8080"
REALM="${KeycloakRealm:-tavern}"
# Use the backend's own service account (same one KeycloakAPIService.GetServiceAccountToken
# already relies on for its Admin API calls) rather than the Keycloak bootstrap admin user -
# that account needs interactive setup (email verification) via the web UI before it's usable,
# which breaks unattended sync on a fresh container. The service account already holds
# realm-management rights in the "tavern" realm (see realm-export.json).
CLIENT_ID="${KeycloakBackendClientId:-backend-tavern}"
CLIENT_SECRET="${KeycloakClientSecret}"
REALM_EXPORT_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/realm-export.json"

echo "Waiting for Keycloak to be ready before syncing client config..."
ready=false
for i in $(seq 1 60); do
    if curl -sf "${KEYCLOAK_URL}/realms/${REALM}" >/dev/null 2>&1; then
        ready=true
        break
    fi
    sleep 2
done

if [ "$ready" != "true" ]; then
    echo "Warning: Keycloak did not become ready in time, skipping client config sync." >&2
    exit 0
fi

TOKEN=$(curl -sf -X POST "${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials" \
    -d "client_id=${CLIENT_ID}" \
    -d "client_secret=${CLIENT_SECRET}" \
    | node -e "
let d = '';
process.stdin.on('data', c => d += c);
process.stdin.on('end', () => {
  const parsed = JSON.parse(d || '{}');
  if (!parsed.access_token) {
    console.error('No access_token in Keycloak token response: ' + d);
    process.exit(1);
  }
  process.stdout.write(parsed.access_token);
});
")

if [ -z "$TOKEN" ]; then
    echo "Warning: could not obtain a Keycloak admin token, skipping client config sync." >&2
    exit 0
fi

PAYLOAD=$(node -e "
const fs = require('fs');
const realm = JSON.parse(fs.readFileSync('${REALM_EXPORT_FILE}', 'utf8'));
// Overwriting the backend-tavern client recreates its service-account user from scratch,
// wiping the realm-management role grants this very script authenticates with. Including that
// user's role mapping in the same partialImport call re-applies them atomically, so the
// service account heals itself instead of locking the next sync run out.
const serviceAccountUser = (realm.users || []).find(
  (u) => u.username === 'service-account-backend-tavern',
);
const payload = { ifResourceExists: 'OVERWRITE', clients: realm.clients };
if (serviceAccountUser) payload.users = [serviceAccountUser];
process.stdout.write(JSON.stringify(payload));
")

if ! curl -sf -X POST "${KEYCLOAK_URL}/admin/realms/${REALM}/partialImport" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d "${PAYLOAD}" >/dev/null; then
    echo "Warning: Keycloak client config sync (partialImport) failed." >&2
    exit 0
fi

echo "Keycloak client config (clients + protocol mappers) synced from realm-export.json."
