FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["MilkDemo.WebApp/MilkDemo.WebApp.csproj", "MilkDemo.WebApp/"]
COPY ["MilkDemo.Shared/MilkDemo.Shared.csproj", "MilkDemo.Shared/"]
RUN dotnet restore "MilkDemo.WebApp/MilkDemo.WebApp.csproj"
COPY . .
WORKDIR "/src/MilkDemo.WebApp"
RUN dotnet publish "MilkDemo.WebApp.csproj" -c Release -o /app/publish

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY MilkDemo.WebApp/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
