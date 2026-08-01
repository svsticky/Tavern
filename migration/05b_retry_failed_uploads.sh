#!/bin/bash
# Retry only the failed poster blob uploads from 05_upload_images.sh.
# Reads key list from /tmp/failed_keys.txt, queries the DB for each key's
# filename and content_type, converts PDFs to PNG, and uploads to S3.

set -euo pipefail

LOCALSTACK_URL="http://tavern-localstack:4566"
BUCKET="posters"
STORAGE_ROOT="/tmp/koala-storage/storage"
CONVERT_DIR="/tmp/koala-converted-retry"
FAILED_KEYS="/tmp/failed_keys.txt"

export PGHOST=db PGUSER=postgres PGDATABASE=postgres PGPASSWORD=postgres
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
export AWS_REQUEST_CHECKSUM_CALCULATION=when_required
export AWS_RESPONSE_CHECKSUM_VALIDATION=when_required

if [[ ! -f "$FAILED_KEYS" ]]; then
    echo "ERROR: $FAILED_KEYS not found"
    exit 1
fi

KEY_COUNT=$(wc -l < "$FAILED_KEYS")
echo "==> Retrying $KEY_COUNT failed poster uploads"

mkdir -p "$CONVERT_DIR"

# Build a quoted CSV of keys for the SQL IN clause
KEYS_CSV=$(awk '{printf "%s'\''%s'\''", (NR>1?",":""), $0}' "$FAILED_KEYS")

# Query blob metadata for just these keys
psql -t -A -F$'\t' -c "
SELECT b.key, b.filename, b.content_type
FROM koala.active_storage_blobs b
WHERE b.key IN ($KEYS_CSV)
" > /tmp/retry_blobs.tsv

FOUND=$(wc -l < /tmp/retry_blobs.tsv)
echo "    DB rows found: $FOUND / $KEY_COUNT"

UPLOADED=0
CONVERTED=0
MISSING=0
SKIPPED=0

while IFS=$'\t' read -r key filename content_type; do
    DISK_PATH="$STORAGE_ROOT/${key:0:2}/${key:2:2}/$key"

    if [[ ! -f "$DISK_PATH" ]]; then
        echo "  MISS: not on disk: $key ($DISK_PATH)"
        MISSING=$((MISSING + 1))
        continue
    fi

    case "$content_type" in
        image/*)
            UPLOAD_FILE="$DISK_PATH"
            UPLOAD_CONTENT_TYPE="$content_type"
            ;;
        application/pdf)
            PREFIX="$CONVERT_DIR/$key"
            pdftoppm -r 150 -f 1 -l 1 -png "$DISK_PATH" "$PREFIX" 2>/dev/null
            CONVERTED_FILE=$(ls "${PREFIX}"-*.png 2>/dev/null | head -1)
            if [[ -z "$CONVERTED_FILE" ]]; then
                echo "  FAIL: PDF→PNG conversion failed for $key"
                SKIPPED=$((SKIPPED + 1))
                continue
            fi
            UPLOAD_FILE="$CONVERTED_FILE"
            UPLOAD_CONTENT_TYPE="image/png"
            CONVERTED=$((CONVERTED + 1))
            ;;
        *)
            echo "  SKIP: unsupported type '$content_type' for $key"
            SKIPPED=$((SKIPPED + 1))
            continue
            ;;
    esac

    echo -n "  UP: $key ... "
    if aws --endpoint-url "$LOCALSTACK_URL" s3 cp "$UPLOAD_FILE" "s3://$BUCKET/$key" \
        --content-type "$UPLOAD_CONTENT_TYPE" --no-progress --quiet; then
        echo "ok"
        UPLOADED=$((UPLOADED + 1))
    else
        echo "FAILED"
    fi
done < /tmp/retry_blobs.tsv

echo ""
echo "==> Results: uploaded=$UPLOADED  pdf-converted=$CONVERTED  missing-on-disk=$MISSING  skipped=$SKIPPED"

rm -rf "$CONVERT_DIR" /tmp/retry_blobs.tsv
echo "==> Done."
