# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared projects first
COPY Shared/DDD.BuildingBlocks/DDD.BuildingBlocks.csproj Shared/DDD.BuildingBlocks/

# Copy main project file
COPY DietApi/DietApi.csproj DietApi/

# Restore dependencies
RUN dotnet restore DietApi/DietApi.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/DietApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is required by the HEALTHCHECK below; the aspnet image does not ship it
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -s /bin/bash appuser

# Create directory for SQLite database with proper permissions
RUN mkdir -p /app/data && chown -R appuser:appuser /app/data

COPY --from=build /app/publish .

# Set ownership of app files
RUN chown -R appuser:appuser /app

USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "DietApi.dll"]
