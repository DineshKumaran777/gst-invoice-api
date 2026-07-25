# =============================================================================
# Dockerfile — GST Invoice API (ASP.NET Core 8.0)
# Multi-stage build for production deployment on Render
# =============================================================================

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY ["GSTInvoice.slnx", "."]
COPY ["GSTInvoice.API/GSTInvoice.API.csproj", "GSTInvoice.API/"]
COPY ["GSTInvoice.Shared/GSTInvoice.Shared.csproj", "GSTInvoice.Shared/"]
COPY ["GSTInvoice.API.Tests/GSTInvoice.API.Tests.csproj", "GSTInvoice.API.Tests/"]

# Restore NuGet packages
RUN dotnet restore "GSTInvoice.API/GSTInvoice.API.csproj"

# Copy all source code
COPY . .

# Build and publish the API project (Release, self-contained for aot-compatibility)
WORKDIR /src/GSTInvoice.API
RUN dotnet publish "GSTInvoice.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1000 dotnetuser \
    && adduser --system --uid 1000 --gid 1000 dotnetuser

# Copy published app from build stage
COPY --from=build /app/publish .

# Create logs directory with proper permissions
RUN mkdir -p /app/logs && chown -R dotnetuser:dotnetuser /app/logs

# Switch to non-root user
USER dotnetuser

# Environment variables (set via Render dashboard or Docker run)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port (Render uses 8080)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GSTInvoice.API.dll"]
