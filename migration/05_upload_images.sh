#!/bin/bash
# Phase 10: Upload activity poster images from the Koala storage zip to LocalStack S3.
#
# Usage: ./05_upload_images.sh /path/to/koala-storage.zip
#
# PDFs are converted to PNG (first page) before upload.
# Each blob is uploaded flat (key = blob key) so PosterPath matches
# what S3StorageService.GetFileAsync("posters", posterPath) expects.

set -euo pipefail

ZIP="${1:-}"
LOCALSTACK_URL="http://tavern-localstack:4566"
BUCKET="posters"
EXTRACT_DIR="/tmp/koala-storage"
CONVERT_DIR="/tmp/koala-converted"

export PGHOST=db PGUSER=postgres PGDATABASE=postgres PGPASSWORD=postgres

if [[ -z "$ZIP" ]]; then
    echo "Usage: $0 /path/to/koala-storage.zip"
    exit 1
fi
if [[ ! -f "$ZIP" ]]; then
    echo "Error: file not found: $ZIP"
    exit 1
fi

export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
# AWS CLI v2 sends x-amz-trailer checksums by default; LocalStack doesn't support them
export AWS_REQUEST_CHECKSUM_CALCULATION=when_required
export AWS_RESPONSE_CHECKSUM_VALIDATION=when_required

echo "==> Installing required tools..."
apt-get install -y -qq --no-install-recommends poppler-utils 2>/dev/null || true

if ! command -v pdftoppm &>/dev/null; then
    echo "ERROR: pdftoppm not available — cannot convert PDFs"
    exit 1
fi

if [[ -d "$EXTRACT_DIR" && -n "$(ls -A "$EXTRACT_DIR" 2>/dev/null)" ]]; then
    echo "==> Skipping extraction — $EXTRACT_DIR already exists"
else
    echo "==> Extracting $ZIP ..."
    rm -rf "$EXTRACT_DIR"
    mkdir -p "$EXTRACT_DIR"
    unzip -q "$ZIP" -d "$EXTRACT_DIR" || tar -xzf "$ZIP" -C "$EXTRACT_DIR"
fi
mkdir -p "$CONVERT_DIR"

# The storage root is the directory containing the 2-char hex subdirs.
# Use pipefail-safe find: || true prevents SIGPIPE from killing the script.
FIRST_FILE=$(find "$EXTRACT_DIR" -type f 2>/dev/null | head -1 || true)
STORAGE_ROOT=$(dirname "$(dirname "$(dirname "$FIRST_FILE")")")
echo "==> Storage root: $STORAGE_ROOT"

echo "==> Ensuring S3 bucket exists..."
aws --endpoint-url "$LOCALSTACK_URL" s3 mb "s3://$BUCKET" 2>/dev/null || true

echo "==> Querying poster blobs from Koala DB..."
psql -t -A -F$'\t' -c "
SELECT DISTINCT ON (a.record_id)
    b.key,
    b.filename,
    b.content_type
FROM koala.active_storage_attachments a
JOIN koala.active_storage_blobs b ON b.id = a.blob_id
WHERE a.record_type = 'Activity' AND a.name = 'poster'
ORDER BY a.record_id, b.created_at DESC
" > /tmp/koala_poster_blobs.tsv

TOTAL=$(wc -l < /tmp/koala_poster_blobs.tsv)
echo "    $TOTAL poster blobs to process"

UPLOADED=0
CONVERTED=0
MISSING=0
SKIPPED=0

echo "==> Processing and uploading to s3://$BUCKET ..."
while IFS=$'\t' read -r key filename content_type; do
    DISK_PATH="$STORAGE_ROOT/${key:0:2}/${key:2:2}/$key"

    if [[ ! -f "$DISK_PATH" ]]; then
        echo "  WARN: not found on disk: $DISK_PATH"
        MISSING=$((MISSING + 1))
        continue
    fi

    case "$content_type" in
        image/*)
            # Already an image — upload as-is
            UPLOAD_FILE="$DISK_PATH"
            UPLOAD_CONTENT_TYPE="$content_type"
            UPLOAD_FILENAME="$filename"
            ;;
        application/pdf)
            # Convert first page to PNG
            PREFIX="$CONVERT_DIR/$key"
            pdftoppm -r 150 -f 1 -l 1 -png "$DISK_PATH" "$PREFIX" 2>/dev/null
            # pdftoppm outputs {prefix}-1.png (or -01.png for multi-page)
            CONVERTED_FILE=$(ls "${PREFIX}"-*.png 2>/dev/null | head -1)
            if [[ -z "$CONVERTED_FILE" ]]; then
                echo "  WARN: PDF conversion failed for $key"
                SKIPPED=$((SKIPPED + 1))
                continue
            fi
            UPLOAD_FILE="$CONVERTED_FILE"
            UPLOAD_CONTENT_TYPE="image/png"
            # Replace .pdf extension with .png in filename
            UPLOAD_FILENAME="${filename%.pdf}.png"
            CONVERTED=$((CONVERTED + 1))
            ;;
        *)
            echo "  SKIP: unsupported type '$content_type' for key $key"
            SKIPPED=$((SKIPPED + 1))
            continue
            ;;
    esac

    if aws --endpoint-url "$LOCALSTACK_URL" s3 cp "$UPLOAD_FILE" "s3://$BUCKET/$key" \
        --content-type "$UPLOAD_CONTENT_TYPE" --no-progress --quiet; then
        UPLOADED=$((UPLOADED + 1))
    else
        echo "  WARN: upload failed for $key — will retry on next run"
        MISSING=$((MISSING + 1))
    fi
done < /tmp/koala_poster_blobs.tsv

echo "    Uploaded: $UPLOADED  (of which $CONVERTED were PDF→PNG conversions)"
echo "    Missing on disk: $MISSING  Skipped (unsupported): $SKIPPED"

echo "==> Updating Activities.PosterPath and PosterFileName in Tavern DB..."
# PosterFileName for PDFs is stored as the converted .png name.
# We detect PDFs by content_type and rename in the SQL as well.
psql -c "
UPDATE \"Activities\" ta
SET
    \"PosterPath\"     = poster.key,
    \"PosterFileName\" = CASE
        WHEN poster.content_type = 'application/pdf'
        THEN regexp_replace(poster.filename, '\.pdf$', '.png', 'i')
        ELSE poster.filename
    END
FROM (
    -- Reconstruct koala_id → tavern_id by joining on name.
    -- DISTINCT ON handles the rare duplicate-name case.
    SELECT DISTINCT ON (ka.id)
        ka.id    AS koala_id,
        ta.\"Id\"  AS tavern_id
    FROM koala.activities ka
    JOIN \"Activities\" ta ON ta.\"Name\" = ka.name
    ORDER BY ka.id, ta.\"Id\"
) id_map
JOIN (
    -- One poster per Koala activity (most recent blob wins)
    SELECT DISTINCT ON (a.record_id)
        a.record_id::int AS koala_activity_id,
        b.key,
        b.filename,
        b.content_type
    FROM koala.active_storage_attachments a
    JOIN koala.active_storage_blobs b ON b.id = a.blob_id
    WHERE a.record_type = 'Activity' AND a.name = 'poster'
    ORDER BY a.record_id, b.created_at DESC
) poster ON poster.koala_activity_id = id_map.koala_id
WHERE ta.\"Id\" = id_map.tavern_id;
"

POSTER_COUNT=$(psql -t -c \
    "SELECT COUNT(*) FROM \"Activities\" WHERE \"PosterPath\" IS NOT NULL;" | tr -d ' ')
TOTAL_ACT=$(psql -t -c \
    "SELECT COUNT(*) FROM \"Activities\";" | tr -d ' ')
echo "    Activities with posters: $POSTER_COUNT / $TOTAL_ACT"

rm -rf "$CONVERT_DIR" /tmp/koala_poster_blobs.tsv
echo "==> Done."
