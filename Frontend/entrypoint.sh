#!/bin/sh
set -e

ROOT_DIR="/app/build/client"
CONFIG_FILE="$ROOT_DIR/env-config.js"

echo "Generating runtime env-config.js in $ROOT_DIR..."
mkdir -p "$ROOT_DIR"

echo "window._env_ = {" > "$CONFIG_FILE"

for line in $(env | grep '^VITE_'); do
  key=$(echo "$line" | cut -d '=' -f 1)
  value=$(echo "$line" | cut -d '=' -f 2-)
  
  echo "  $key: \"$value\"," >> "$CONFIG_FILE"
  echo "Added $key to config"
done

echo "};" >> "$CONFIG_FILE"

echo "Configuration generated. Final file content:"
cat "$CONFIG_FILE"

echo "Starting Node server..."
exec "$@"