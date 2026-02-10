@echo off
echo 🥛 Starting Milk API Manager System for Windows...

:: 1. Start Infrastructure (APISIX, Etcd, ELK, Monitoring)
echo 🚀 Launching infrastructure containers...
docker-compose up -d

:: 2. Wait for APISIX to be ready
echo ⏳ Waiting for APISIX Admin API...
:wait_apisix
curl -s http://localhost:9080/apisix/admin/routes -H "X-API-KEY: edd1c9f034335f136f87ad84b625c88b" >nul 2>&1
if %errorlevel% neq 0 (
    timeout /t 2 >nul
    goto wait_apisix
)

:: 3. Setup Initial Seed Data
echo 🌱 Injecting seed data...
:: Note: Assuming curl is available in Win11 (it usually is)
:: Running the shell script logic via curl commands if needed, 
:: but for MVP we assume manual or existing etcd state.

:: 4. Start Backend (MilkApiManager)
echo ⚙️ Starting Backend API...
start /b dotnet run --project backend/MilkApiManager/MilkApiManager.csproj

:: 5. Start Blazor UI (MilkAdminBlazor)
echo 🖥️ Starting Admin Dashboard...
start /b dotnet run --project backend/MilkAdminBlazor/MilkAdminBlazor.csproj

echo ✅ System is up!
echo -----------------------------------
echo APISIX Gateway: http://localhost:9080
echo APISIX Dashboard: http://localhost:9000
echo Milk Admin UI: http://localhost:5000
echo Grafana: http://localhost:3000
echo Kibana: http://localhost:5601
echo -----------------------------------
echo Press Ctrl+C in this window to stop (Note: containers will remain running).
pause
