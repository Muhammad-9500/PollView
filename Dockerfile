# Build the ASP.NET Core API from the repository root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/PollViews/PollViews.csproj backend/PollViews/
RUN dotnet restore backend/PollViews/PollViews.csproj

COPY backend/PollViews/ backend/PollViews/
RUN dotnet publish backend/PollViews/PollViews.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Small runtime image for Render.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql may load Kerberos/GSSAPI support when opening PostgreSQL connections.
RUN apt-get update \
    && apt-get install --no-install-recommends --yes libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

# Render provides PORT. Fall back to its usual web-service port locally.
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet PollViews.dll --urls http://0.0.0.0:${PORT:-10000}"]
