# Multi-stage build for .NET 8 backend services
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and build dependencies
COPY ["build/Directory.Build.props", "build/"]
COPY ["build/Directory.Build.targets", "build/"]

# Copy all csproj files for dependency resolution
COPY ["src/BuildingBlocks/Common/BuildingBlocks.Common.csproj", "src/BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/Observability/BuildingBlocks.Observability.csproj", "src/BuildingBlocks/Observability/"]
COPY ["src/Core/Domain/Core.Domain.csproj", "src/Core/Domain/"]
COPY ["src/Core/Application/Core.Application.csproj", "src/Core/Application/"]
COPY ["src/Infrastructure/Persistence/Infrastructure.Persistence.csproj", "src/Infrastructure/Persistence/"]
COPY ["src/Infrastructure/Messaging/Infrastructure.Messaging.csproj", "src/Infrastructure/Messaging/"]
COPY ["src/Lambdas/Recipe/Recipe.csproj", "src/Lambdas/Recipe/"]
COPY ["src/Lambdas/User/User.csproj", "src/Lambdas/User/"]
COPY ["src/Lambdas/Authorizer/Authorizer.csproj", "src/Lambdas/Authorizer/"]

# Restore dependencies for each Lambda project individually
RUN dotnet restore src/Lambdas/Recipe/Recipe.csproj
RUN dotnet restore src/Lambdas/User/User.csproj
RUN dotnet restore src/Lambdas/Authorizer/Authorizer.csproj

# Copy all source code
COPY . .

# Build and publish each Lambda service in one step
RUN dotnet publish src/Lambdas/Recipe/Recipe.csproj -c Release -o /app/recipe
RUN dotnet publish src/Lambdas/User/User.csproj -c Release -o /app/user
RUN dotnet publish src/Lambdas/Authorizer/Authorizer.csproj -c Release -o /app/authorizer

# Final runtime stage
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=build /app/recipe ./recipe/
COPY --from=build /app/user ./user/
COPY --from=build /app/authorizer ./authorizer/

# Environment variables for Docker development
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DATABASE__CONNECTIONSTRING="Server=mysql;Database=RecipeApp_Dev;User=admin;Password=pass1234;"
ENV DATABASE__USEIMMEMORYFALLBACK=true
ENV DATABASE_TYPE=mysql

# Create a startup script with sequential startup to avoid database conflicts
RUN echo '#!/bin/bash\n\
set -e\n\
echo "Starting services sequentially to avoid database conflicts..."\n\
echo "Starting User service on port 5001..."\n\
cd /app/user\n\
ASPNETCORE_URLS="http://+:5001" dotnet User.dll &\n\
USER_PID=$!\n\
sleep 5\n\
echo "Starting Recipe service on port 5000..."\n\
cd /app/recipe\n\
ASPNETCORE_URLS="http://+:5000" dotnet Recipe.dll &\n\
RECIPE_PID=$!\n\
sleep 3\n\
echo "Starting Authorizer service on port 5002..."\n\
cd /app/authorizer\n\
ASPNETCORE_URLS="http://+:5002" dotnet Authorizer.dll &\n\
AUTH_PID=$!\n\
echo "All services started. PIDs: Recipe=$RECIPE_PID User=$USER_PID Auth=$AUTH_PID"\n\
echo "Waiting for services..."\n\
wait' > /app/start.sh && chmod +x /app/start.sh

ENTRYPOINT ["/app/start.sh"]