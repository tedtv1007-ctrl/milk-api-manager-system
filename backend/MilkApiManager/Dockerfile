FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["MilkApiManager/MilkApiManager.csproj", "MilkApiManager/"]
COPY ["MilkShared/MilkShared.csproj", "MilkShared/"]
RUN dotnet restore "MilkApiManager/MilkApiManager.csproj"
COPY . .
WORKDIR "/src/MilkApiManager"
RUN dotnet publish "MilkApiManager.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "MilkApiManager.dll"]
