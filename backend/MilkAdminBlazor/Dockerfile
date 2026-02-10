FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MilkAdminBlazor.csproj", "."]
RUN dotnet restore "MilkAdminBlazor.csproj"
COPY . .
RUN dotnet publish "MilkAdminBlazor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MilkAdminBlazor.dll"]
