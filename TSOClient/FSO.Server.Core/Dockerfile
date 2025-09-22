FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install runtime dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends mariadb-client openssl jq && \
    rm -rf /var/lib/apt/lists/*

# Copy prebuilt server DLLs (built on Windows by user)
COPY ./bin/Release/net9.0/ ./

# Assert main DLL exists
RUN [ -f "FSO.Server.Core.dll" ] || (echo "FSO.Server.Core.dll not found! Build the project in Release mode." && exit 1)

# Copy sample config and entrypoint
COPY ./config.sample.json /app/config.sample.json
COPY ./docker.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Volumes for persistence
VOLUME ["/app/nfs"]

# Expose server ports
EXPOSE 9000 33100 34100 35100

# Environment defaults (overridable)
ENV DB_HOST=fso-db
ENV DB_PORT=3306
ENV DB_USER=fsoserver
ENV DB_PASSWORD=fsopass
ENV DB_NAME=fso
ENV DB_CONNECTION_STRING=""
ENV GAME_LOCATION=/app/game
ENV SIM_NFS=/app/nfs
ENV USER_API_PORT=9000
ENV CITY_PORT=33100
ENV LOT_PORT=34100
ENV TASK_PORT=35100
ENV CITY_CALLSIGN=ganymede
ENV LOT_CALLSIGN=europa
ENV TASK_CALLSIGN=callisto
ENV SSL_CERT=/app/certs/server.pfx
ENV SSL_PASSWORD=password

ENTRYPOINT ["/app/entrypoint.sh"]
