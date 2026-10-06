FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0

# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#   && apt-get -y install --no-install-recommends neovim # Essential tools here

# Install Entity Framework Core for .NET 10, matching the runtime we have installed (through NuGet)
USER vscode
RUN dotnet tool install --global dotnet-ef --version 10.0.11