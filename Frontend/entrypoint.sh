#!/bin/sh
set -e

ROOT_DIR=/app/build/client

echo "Generating runtime env-config.js in $ROOT_DIR..."

mkdir -p $ROOT_DIR

echo "window._env_ = {" > $ROOT_DIR/env-config.js

env | grep '^VITE_' | while read -r line; do
  key=$(echo $line | cut -d '=' -f 1)
  value=$(echo $line | cut -d '=' -f 2-)
  echo "  $key: \"$value\"," >> $ROOT_DIR/env-config.js
  echo "Added $key to config"
done

echo "};" >> $ROOT_DIR/env-config.js

echo "Configuration generated. Starting Node server..."

exec "$@"