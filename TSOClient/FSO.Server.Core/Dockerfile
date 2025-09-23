FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:1af4114db9ba87542a3f23dbb5cd9072cad7fcc8505f6e9131d1feb580286a6f AS runtime

# Environment defaults (can be overridden in docker-compose.yml or .env)
ENV DB_HOST=fso-db
ENV DB_PORT=3306
ENV DB_USER=fsoserver
ENV DB_PASSWORD=fsopass
ENV DB_NAME=fso
ENV DB_CONNECTION_STRING=""
ENV GAME_LOCATION=./game
ENV SIM_NFS=./nfs
ENV USER_API_PORT=9000
ENV CITY_PORT=33100
ENV LOT_PORT=34100
ENV TASK_PORT=35100
ENV CITY_CALLSIGN=ganymede
ENV LOT_CALLSIGN=europa
ENV TASK_CALLSIGN=callisto
ENV SSL_CERT=/app/certs/server.pfx
ENV SSL_PASSWORD=password

# Install runtime dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        mariadb-client \
        openssl \
        jq \
        sqlite3 \
        libsqlite3-dev && \
    rm -rf /var/lib/apt/lists/*
    
# Copy prebuilt server DLLs (built on your Windows dev box)
COPY ./bin/Release/net9.0/ /app/src/
WORKDIR /app/src

# Assert main DLL exists
RUN [ -f "FSO.Server.Core.dll" ] || (echo "FSO.Server.Core.dll not found! Build the project in Release mode." && exit 1)

# Copy sample config and entrypoint
COPY ./config.sample.json ./config.sample.json
COPY docker.sh ./entrypoint.sh
RUN sed -i 's/\r$//' ./entrypoint.sh && chmod +x ./entrypoint.sh

# Volumes for persistence (mount at runtime with docker-compose)
VOLUME ["/app/src/nfs", "/app/src/game"]

# Expose server ports
EXPOSE 9000 33100 34100 35100

ENTRYPOINT ["./entrypoint.sh"]
