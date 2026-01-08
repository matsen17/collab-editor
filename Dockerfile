# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for dependency restoration
COPY CollabEditor.API/CollabEditor.API.csproj CollabEditor.API/
COPY CollabEditor.Application/CollabEditor.Application.csproj CollabEditor.Application/
COPY CollabEditor.Domain/CollabEditor.Domain.csproj CollabEditor.Domain/
COPY CollabEditor.Infrastructure/CollabEditor.Infrastructure.csproj CollabEditor.Infrastructure/
COPY CollabEditor.Messaging/CollabEditor.Messaging.csproj CollabEditor.Messaging/
COPY CollabEditor.Utilities/CollabEditor.Utilities.csproj CollabEditor.Utilities/

# Restore dependencies for API project (includes all dependencies)
RUN dotnet restore CollabEditor.API/CollabEditor.API.csproj

# Copy remaining source code
COPY . .

# Build and publish the application
WORKDIR /src/CollabEditor.API
RUN dotnet publish \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install required libraries for PostgreSQL client
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -u 1000 appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Set ownership and switch to non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expose port (ASP.NET Core default)
EXPOSE 8080

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CollabEditor.API.dll"]