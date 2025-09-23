#!/bin/bash
set -e

CONFIG_FILE="./config.json"
CLEAN_CONFIG="./config.cleaned.json"

: "${DB_HOST:=fso-db}"
: "${DB_USER:=fsoserver}"
: "${DB_PASSWORD:=fsopass}"
: "${DB_NAME:=fso}"

# Always enforce correct paths (ignore bad ENV overrides)
GAME_LOCATION="./game/"
SIM_NFS="./nfs/"

echo "Ensuring config.json is fresh..."
# Remove comment lines from sample
sed '/^\s*\/\//d' ./config.sample.json > "$CLEAN_CONFIG"

# Copy cleaned config as starting point
cp "$CLEAN_CONFIG" "$CONFIG_FILE"

# Update database connection and paths
jq --arg db "server=${DB_HOST};uid=${DB_USER};pwd=${DB_PASSWORD};database=${DB_NAME};" \
   --arg game "$GAME_LOCATION" \
   --arg nfs "$SIM_NFS" \
   --arg secret "$(openssl rand -hex 32)" \
   '.database.connectionString = $db
    | .gameLocation = $game
    | .simNFS = $nfs
    | .secret = $secret' \
   "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"

# Wait for DB
echo "Waiting for database to be ready..."
until mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" -e "SELECT 1;" >/dev/null 2>&1; do
    echo -n "."
    sleep 1
done
echo "Database ready."
sleep 3

# Initialize DB if empty
TABLE_COUNT=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" -D "$DB_NAME" -sNe \
    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME';")

if [ "$TABLE_COUNT" -eq 0 ]; then
    echo "Database empty. Running db-init..."
    for i in $(seq 1 3); do
        echo "Attempt $i: Running db-init..."
        if dotnet exec FSO.Server.Core.dll db-init <<< "y"; then
            echo "db-init succeeded!"
            break
        fi
        echo "db-init failed, retrying in 3s..."
        sleep 3
    done
else
    echo "Database already initialized. Skipping db-init."
fi

# Start the server
exec dotnet exec FSO.Server.Core.dll run
