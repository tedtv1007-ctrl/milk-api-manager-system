FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files first to leverage Docker cache
COPY ["MilkAdminBlazor/MilkAdminBlazor.csproj", "MilkAdminBlazor/"]
COPY ["MilkApiManager/MilkApiManager.csproj", "MilkApiManager/"]
RUN dotnet restore "MilkAdminBlazor/MilkAdminBlazor.csproj"

# Copy the rest of the source code
COPY . .

# Explicitly build and publish ONLY the Blazor project to avoid appsettings.json collision
WORKDIR "/src/MilkAdminBlazor"
RUN dotnet publish "MilkAdminBlazor.csproj" -c Release -o /app/publish /p:ErrorOnDuplicatePublishOutputFiles=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "MilkAdminBlazor.dll"]
