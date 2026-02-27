FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MilkWorker/MilkWorker.csproj", "MilkWorker/"]
COPY ["MilkShared/MilkShared.csproj", "MilkShared/"]
RUN dotnet restore "MilkWorker/MilkWorker.csproj"

COPY . .
WORKDIR "/src/MilkWorker"
RUN dotnet build "MilkWorker.csproj" -c Release -o /app/build
RUN dotnet publish "MilkWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MilkWorker.dll"]
