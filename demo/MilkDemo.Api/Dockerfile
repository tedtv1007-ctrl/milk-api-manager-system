FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["MilkDemo.Api/MilkDemo.Api.csproj", "MilkDemo.Api/"]
COPY ["MilkDemo.Shared/MilkDemo.Shared.csproj", "MilkDemo.Shared/"]
RUN dotnet restore "MilkDemo.Api/MilkDemo.Api.csproj"
COPY . .
WORKDIR "/src/MilkDemo.Api"
RUN dotnet publish "MilkDemo.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
RUN apk add --no-cache curl
WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R app:app /app

USER app
HEALTHCHECK --interval=15s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "MilkDemo.Api.dll"]
