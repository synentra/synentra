ARG APP_VERSION="1.0.0"

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION="1.0.0"
WORKDIR /src

COPY Synentra.slnx .

COPY src/Synentra/Synentra.csproj src/Synentra/
COPY src/Synentra.Domain/Synentra.Domain.csproj src/Synentra.Domain/
COPY src/Synentra.Application/Synentra.Application.csproj src/Synentra.Application/
COPY src/Synentra.Persistence/Synentra.Persistence.csproj src/Synentra.Persistence/
COPY src/Synentra.Infrastructure/Synentra.Infrastructure.csproj src/Synentra.Infrastructure/
COPY src/Synentra.Persistence.PostgreSql/Synentra.Persistence.PostgreSql.csproj src/Synentra.Persistence.PostgreSql/
COPY src/Synentra.Persistence.Sqlite/Synentra.Persistence.Sqlite.csproj src/Synentra.Persistence.Sqlite/

RUN dotnet restore src/Synentra/Synentra.csproj --runtime linux-x64

COPY src/ src/

# Copy legal files that are required during publish and must be in the final image
COPY LICENSE THIRD-PARTY-NOTICES.md ./
COPY licenses/ ./licenses/

RUN dotnet publish "./src/Synentra/Synentra.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=${APP_VERSION} \
    --runtime "linux-x64"

# Runtime stage (IMPORTANT: match glibc runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Static OCI image metadata
LABEL org.opencontainers.image.title="Synentra"
LABEL org.opencontainers.image.description="Intent-Aware Runtime Governance for AI Agents"
LABEL org.opencontainers.image.source="https://github.com/synentra/synentra"
LABEL org.opencontainers.image.licenses="Apache-2.0"

ENV ASPNETCORE_URLS=http://+:7080
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 7080

ARG APP_VERSION
ARG REVISION
LABEL org.opencontainers.image.version="${APP_VERSION}"
LABEL org.opencontainers.image.revision="${REVISION}"

ENTRYPOINT ["dotnet", "synentra.dll"]