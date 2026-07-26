# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and every project file first so `dotnet restore` is cached independently of source.
COPY com.jobsite.chat.slnx ./
COPY src/com.jobsite.chat.Domain/com.jobsite.chat.Domain.csproj src/com.jobsite.chat.Domain/
COPY src/com.jobsite.chat.Shared/com.jobsite.chat.Shared.csproj src/com.jobsite.chat.Shared/
COPY src/com.jobsite.chat.Service/com.jobsite.chat.Service.csproj src/com.jobsite.chat.Service/
COPY src/com.jobsite.chat.Repository/com.jobsite.chat.Repository.csproj src/com.jobsite.chat.Repository/
COPY src/com.jobsite.chat.Api/com.jobsite.chat.Api.csproj src/com.jobsite.chat.Api/
RUN dotnet restore src/com.jobsite.chat.Api/com.jobsite.chat.Api.csproj

# Copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/com.jobsite.chat.Api/com.jobsite.chat.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is required by the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# Run as the non-root user shipped in the runtime image.
USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "com.jobsite.chat.Api.dll"]
