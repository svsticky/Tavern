#!/bin/bash
# Phase 0b: Apply EF Core migrations to create Tavern tables in the public schema.
set -euo pipefail

cd /workspaces/Backend

echo "==> Applying EF Core migrations..."
~/.dotnet/tools/dotnet-ef database update --connection "Host=db;Port=5432;Database=postgres;Username=postgres;Password=postgres"

echo "==> Tavern tables created:"
PGPASSWORD=postgres psql -h db -U postgres -d postgres -c \
  "SELECT tablename FROM pg_tables WHERE schemaname='public' AND tablename NOT LIKE 'kc_%' ORDER BY tablename;" \
  | grep -v "databasechange\|admin_event\|auth\|broker\|client\|component\|composite\|credential\|default_client\|event_entity\|fed_user\|keycloak\|migration\|offline\|policy\|protocol\|realm\|resource\|role_\|scope_\|user_\|web_origins" \
  | head -30
