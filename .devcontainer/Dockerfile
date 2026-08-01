FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

# Add PostgreSQL 17 client tools (pg_restore, psql, etc.)
RUN sh -c 'echo "deb https://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" > /etc/apt/sources.list.d/pgdg.list' \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | tee /etc/apt/trusted.gpg.d/postgresql.asc > /dev/null \
    && rm -f /etc/apt/sources.list.d/yarn.list \
    && apt-get update -qq \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*

# Install Entity Framework Core for .NET 8, matching the runtime we have installed (through NuGet)
USER vscode
RUN dotnet tool install --global dotnet-ef --version 8.0.20