#!/bin/bash
set -e

CONFIG_FILE="/app/config.json"
CLEAN_CONFIG="/app/config.cleaned.json"

# Ensure DB environment variables are set
: "${DB_HOST:=fso-db}"
: "${DB_USER:=fsoserver}"
: "${DB_PASSWORD:=fsopass}"
: "${DB_NAME:=fso}"
: "${GAME_LOCATION:=/app/game}"
: "${SIM_NFS:=/app/nfs}"

# Generate config.json from sample if missing
if [ ! -f "$CONFIG_FILE" ]; then
    echo "Generating config.json from sample..."

    # Remove any comment lines starting with //
    sed '/^\s*\/\//d' /app/config.sample.json > "$CLEAN_CONFIG"

    # Copy cleaned config as starting point
    cp "$CLEAN_CONFIG" "$CONFIG_FILE"

    # Update database connection and paths
    jq --arg db "server=${DB_HOST};uid=${DB_USER};pwd=${DB_PASSWORD};database=${DB_NAME};" \
       --arg game "$GAME_LOCATION" \
       --arg nfs "$SIM_NFS" \
       '.database.connectionString = $db | .gameLocation = $game | .simNFS = $nfs' \
       "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"
fi

# Wait for DB to be ready
echo "Waiting for database to be ready..."
until mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" -e "SELECT 1;" >/dev/null 2>&1; do
    echo -n "."
    sleep 1
done
echo "Database ready, waiting a few seconds for full startup..."
sleep 5

# Initialize DB if empty
if [ -z "$DB_CONNECTION_STRING" ]; then
    TABLE_COUNT=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" -D "$DB_NAME" -sNe \
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME';")

    if [ "$TABLE_COUNT" -eq 0 ]; then
        echo "Database empty. Running db-init..."
        MAX_RETRIES=3
        for i in $(seq 1 $MAX_RETRIES); do
            echo "Attempt $i: Running db-init..."
            if dotnet exec FSO.Server.Core.dll db-init; then
                echo "db-init succeeded!"
                break
            fi
            echo "db-init failed, retrying in 3s..."
            sleep 3
        done
    else
        echo "Database already initialized. Skipping db-init."
    fi
fi

# Start the server
exec dotnet exec FSO.Server.Core.dll run
