#!/bin/bash
# Phase 0: Load koala_prod.pgdump into the 'koala' schema of the local postgres database.
set -euo pipefail

PGHOST=db
PGPORT=5432
PGUSER=postgres
PGPASSWORD=postgres
PGDB=postgres
DUMP=/workspaces/koala_prod.pgdump

export PGPASSWORD

echo "==> Dropping and recreating koala schema..."
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDB" -c "DROP SCHEMA IF EXISTS koala CASCADE;"
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDB" -c "CREATE SCHEMA koala;"

echo "==> Extracting SQL from custom-format dump..."
pg_restore --no-owner --no-acl --no-comments -f /tmp/koala_raw.sql "$DUMP"

echo "==> Rewriting schema references: public -> koala..."
sed \
  -e 's/SET search_path = public, pg_catalog;/SET search_path = koala, pg_catalog;/g' \
  -e 's/\bpublic\.\([a-z_]\)/koala.\1/g' \
  /tmp/koala_raw.sql > /tmp/koala_schemed.sql

echo "==> Loading into postgres (koala schema)..."
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDB" \
  -v ON_ERROR_STOP=0 \
  -f /tmp/koala_schemed.sql 2>&1 | grep -i "error\|fatal" | grep -v "already exists" | head -20 || true

echo "==> Verifying table row counts..."
psql -h "$PGHOST" -U "$PGUSER" -d "$PGDB" -c "
SELECT
  'members'         AS tbl, COUNT(*) FROM koala.members UNION ALL
  SELECT 'users',           COUNT(*) FROM koala.users UNION ALL
  SELECT 'groups',          COUNT(*) FROM koala.groups UNION ALL
  SELECT 'activities',      COUNT(*) FROM koala.activities UNION ALL
  SELECT 'participants',    COUNT(*) FROM koala.participants UNION ALL
  SELECT 'payments',        COUNT(*) FROM koala.payments UNION ALL
  SELECT 'educations',      COUNT(*) FROM koala.educations UNION ALL
  SELECT 'group_members',   COUNT(*) FROM koala.group_members UNION ALL
  SELECT 'tags',            COUNT(*) FROM koala.tags UNION ALL
  SELECT 'posts',           COUNT(*) FROM koala.posts UNION ALL
  SELECT 'active_storage_attachments', COUNT(*) FROM koala.active_storage_attachments;
"
echo "==> Done. Koala data is in the 'koala' schema."
