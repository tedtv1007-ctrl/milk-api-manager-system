#!/bin/bash
# Milk API Manager System - MVP Launcher

echo "🥛 Starting Milk API Manager System..."

# 1. Start Infrastructure (APISIX, Etcd, ELK, Monitoring)
echo "🚀 Launching infrastructure containers..."
docker-compose up -d

# 2. Wait for APISIX to be ready
echo "⏳ Waiting for APISIX Admin API..."
until curl -s http://localhost:9080/apisix/admin/routes -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c88b'; do
  sleep 2
done

# 3. Setup Initial Seed Data (Optional)
echo "🌱 Injecting seed data..."
./scripts/setup-consumers.sh

# 4. Start Backend (MilkApiManager)
echo "⚙️ Starting Backend API..."
cd backend/MilkApiManager
dotnet run &
BACKEND_PID=$!
cd ../..

# 5. Start Blazor UI (MilkAdminBlazor)
echo "🖥️ Starting Admin Dashboard..."
cd backend/MilkAdminBlazor
dotnet run &
UI_PID=$!
cd ../..

echo "✅ System is up!"
echo "-----------------------------------"
echo "APISIX Gateway: http://localhost:9080"
echo "APISIX Dashboard: http://localhost:9000"
echo "Milk Admin UI: http://localhost:5000 (or as configured in dotnet)"
echo "Grafana: http://localhost:3000"
echo "Kibana: http://localhost:5601"
echo "-----------------------------------"

# Keep script running to monitor logs or wait for exit
trap "kill $BACKEND_PID $UI_PID; docker-compose down" EXIT
wait
