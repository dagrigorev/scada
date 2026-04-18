# Multi-stage build for RapidScada WebAPI
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["RapidScada.sln", "./"]
COPY ["src/Presentation/RapidScada.WebApi/RapidScada.WebApi.csproj", "src/Presentation/RapidScada.WebApi/"]
COPY ["src/Core/RapidScada.Domain/RapidScada.Domain.csproj", "src/Core/RapidScada.Domain/"]
COPY ["src/Core/RapidScada.Application/RapidScada.Application.csproj", "src/Core/RapidScada.Application/"]
COPY ["src/Infrastructure/RapidScada.Persistence/RapidScada.Persistence.csproj", "src/Infrastructure/RapidScada.Persistence/"]

# Restore dependencies
RUN dotnet restore "src/Presentation/RapidScada.WebApi/RapidScada.WebApi.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/src/Presentation/RapidScada.WebApi"
RUN dotnet build "RapidScada.WebApi.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "RapidScada.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RapidScada.WebApi.dll"]
