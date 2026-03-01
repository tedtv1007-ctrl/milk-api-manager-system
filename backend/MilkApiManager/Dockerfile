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
RUN wget https://github.com/grafana/k6/releases/download/v0.50.0/k6-v0.50.0-linux-amd64.tar.gz && \
    tar -xzf k6-v0.50.0-linux-amd64.tar.gz && \
    mv k6-v0.50.0-linux-amd64/k6 /usr/bin/k6 && \
    rm -rf k6-v0.50.0-linux-amd64*
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "MilkApiManager.dll"]
