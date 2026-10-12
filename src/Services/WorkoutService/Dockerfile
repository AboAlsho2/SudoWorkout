
# Stage 1: Runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Stage 2: Restore and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first to improve Docker layer caching
COPY ["src/Services/WorkoutService/WorkoutService.csproj", "src/Services/WorkoutService/"]

RUN dotnet restore "src/Services/WorkoutService/WorkoutService.csproj"

# Copy the remaining source files
COPY . .

WORKDIR "/src/src/Services/WorkoutService"

RUN dotnet publish "WorkoutService.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Stage 3: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkoutService.dll"]