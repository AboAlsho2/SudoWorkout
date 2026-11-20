
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/NotificationService/NotificationService.csproj", "src/Services/NotificationService/"]
RUN dotnet restore "src/Services/NotificationService/NotificationService.csproj"

COPY . .
WORKDIR "/src/src/Services/NotificationService"
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
