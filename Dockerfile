# ===================================================
# Stage 1: Build Stage
# ===================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and csproj files first for better layer caching
COPY GenericAgents.sln ./
COPY ["src/Agent.AI/Agent.AI.csproj", "src/Agent.AI/"]
COPY ["src/Agent.Communication/Agent.Communication.csproj", "src/Agent.Communication/"]
COPY ["src/Agent.Configuration/Agent.Configuration.csproj", "src/Agent.Configuration/"]
COPY ["src/Agent.Core/Agent.Core.csproj", "src/Agent.Core/"]
COPY ["src/Agent.DI/Agent.DI.csproj", "src/Agent.DI/"]
COPY ["src/Agent.Observability/Agent.Observability.csproj", "src/Agent.Observability/"]
COPY ["src/Agent.Orchestration/Agent.Orchestration.csproj", "src/Agent.Orchestration/"]
COPY ["src/Agent.Registry/Agent.Registry.csproj", "src/Agent.Registry/"]
COPY ["src/Agent.Security/Agent.Security.csproj", "src/Agent.Security/"]
COPY ["src/Agent.Tools/Agent.Tools.csproj", "src/Agent.Tools/"]
COPY ["src/Agent.Tools.Samples/Agent.Tools.Samples.csproj", "src/Agent.Tools.Samples/"]

# Restore dependencies for main project
RUN dotnet restore "src/Agent.Core/Agent.Core.csproj"

# Copy source code
COPY src/ src/

# Build the main application project and its dependencies
RUN dotnet build "src/Agent.Core/Agent.Core.csproj" -c Release --no-restore
RUN dotnet publish "src/Agent.Core/Agent.Core.csproj" -c Release -o /app/publish --no-restore --self-contained false

# ===================================================
# Stage 2: Test Stage (for CI/CD testing)
# ===================================================
FROM build AS test
WORKDIR /src

# Copy test projects
COPY ["tests/Agent.AI.Tests/Agent.AI.Tests.csproj", "tests/Agent.AI.Tests/"]
COPY ["tests/Agent.Configuration.Tests/Agent.Configuration.Tests.csproj", "tests/Agent.Configuration.Tests/"]
COPY ["tests/Agent.Core.Tests/Agent.Core.Tests.csproj", "tests/Agent.Core.Tests/"]
COPY ["tests/Agent.DI.Tests/Agent.DI.Tests.csproj", "tests/Agent.DI.Tests/"]
COPY ["tests/Agent.Observability.Tests/Agent.Observability.Tests.csproj", "tests/Agent.Observability.Tests/"]
COPY ["tests/Agent.Orchestration.Tests/Agent.Orchestration.Tests.csproj", "tests/Agent.Orchestration.Tests/"]
COPY ["tests/Agent.Registry.Tests/Agent.Registry.Tests.csproj", "tests/Agent.Registry.Tests/"]
COPY ["tests/Agent.Security.Tests/Agent.Security.Tests.csproj", "tests/Agent.Security.Tests/"]
COPY ["tests/Agent.Tools.Tests/Agent.Tools.Tests.csproj", "tests/Agent.Tools.Tests/"]
COPY ["tests/Agent.Tools.Samples.Tests/Agent.Tools.Samples.Tests.csproj", "tests/Agent.Tools.Samples.Tests/"]
COPY ["tests/Integration/Agent.Integration.Tests/Agent.Integration.Tests.csproj", "tests/Integration/Agent.Integration.Tests/"]
COPY ["tests/Performance/Agent.Performance.Tests/Agent.Performance.Tests.csproj", "tests/Performance/Agent.Performance.Tests/"]

# Restore test dependencies
RUN dotnet restore

# Copy test source code
COPY tests/ tests/

# Build tests
RUN dotnet build GenericAgents.sln -c Release --no-restore

# Set environment variables for testing
ENV ASPNETCORE_ENVIRONMENT=Testing
ENV JWT_SIGNING_KEY="docker-test-signing-key-for-ci-cd-only"

# Default command for test stage
CMD ["dotnet", "test", "--no-build", "-c", "Release", "--verbosity", "normal"]

# ===================================================
# Stage 3: Runtime Stage
# ===================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks and other utilities
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN addgroup --system agent && adduser --system --ingroup agent agent
RUN chown -R agent:agent /app
USER agent

# Copy published application
COPY --from=build --chown=agent:agent /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_ENABLE_DUMP_ON_CTRL_BREAK=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "Agent.Core.dll"]