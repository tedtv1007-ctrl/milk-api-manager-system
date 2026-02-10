# Use the official .NET 8.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MilkApiManager.csproj", "."]
RUN dotnet restore "MilkApiManager.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "MilkApiManager.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "MilkApiManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8.0 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MilkApiManager.dll"]