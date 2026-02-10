FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MilkApiManager.csproj", "."]
RUN dotnet restore "MilkApiManager.csproj"
COPY . .
RUN dotnet publish "MilkApiManager.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MilkApiManager.dll"]
