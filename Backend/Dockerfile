# Pepare base layer which will be used in the final step
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Build everything in a completely different stage to not clutter the base stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Backend/Backend.csproj", "Backend/"]
RUN dotnet restore "Backend/Backend.csproj"
COPY . .
WORKDIR "/src/Backend"
RUN dotnet build "Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Introduce a new stage from the bulding stage to improve caching and allow future expansions for things such as tests
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Finally, copy over the published project into the base stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG HostUrl
ARG KeycloakUrl
ARG KeycloakRealm
ENV HostUrl=$HostUrl
ENV KeycloakUrl=$KeycloakUrl
ENV KeycloakRealm=$KeycloakRealm
ENTRYPOINT ["dotnet", "Backend.dll"]
