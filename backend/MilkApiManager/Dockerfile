FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MilkApiManager.csproj", "."]
RUN dotnet restore "MilkApiManager.csproj"
COPY . .
RUN dotnet publish "MilkApiManager.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*
RUN curl -L https://github.com/grafana/k6/releases/download/v0.50.0/k6-v0.50.0-linux-amd64.tar.gz -o k6.tar.gz && \
    tar -xzf k6.tar.gz && \
    mv k6-v0.50.0-linux-amd64/k6 /usr/local/bin/ && \
    rm -rf k6*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MilkApiManager.dll"]
