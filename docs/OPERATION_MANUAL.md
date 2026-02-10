# Milk API Manager System Operation Manual

This manual provides instructions for deploying, operating, and verifying the Milk API Manager System.

## 1. Project Overview

The Milk API Manager System is an enterprise-grade API management platform built on Apache APISIX. It features a custom security automation layer, a Blazor-based admin dashboard, and full observability with the Elastic Stack, Prometheus, and Grafana.

## 2. Prerequisites

Before starting, ensure you have the following installed:
- **Docker** and **Docker Compose**
- **.NET 8.0 SDK** (for running backend services)
- **bash** (for running setup scripts)
- **curl** (for API testing)

## 3. Installation & Startup

### Step 1: Clone the Repository
```bash
git clone <repository-url>
cd milk-api-manager-system
```

### Step 2: Start Infrastructure
The system uses Docker Compose to manage core infrastructure (APISIX, Etcd, Elasticsearch, etc.).
```bash
docker-compose up -d
```

### Step 3: Run Startup Scripts
Setup initial consumers and traffic control policies:
```bash
chmod +x scripts/*.sh
./scripts/setup-consumers.sh
```

### Step 4: Launch Backend & Dashboard
The system requires both the `MilkApiManager` (API layer) and `MilkAdminBlazor` (UI layer) to be running. You can use the provided launcher script:
```bash
./scripts/start-all.sh
```
*Alternatively, run them manually in separate terminals:*
```bash
# Terminal 1: Backend
cd backend/MilkApiManager
dotnet run --urls "http://localhost:5000"

# Terminal 2: Dashboard
cd backend/MilkAdminBlazor
dotnet run --urls "http://localhost:5001"
```

## 4. Key Service Endpoints

| Service | URL | Description |
| :--- | :--- | :--- |
| **APISIX Gateway** | `http://localhost:9080` | Data plane for API traffic |
| **APISIX Dashboard** | `http://localhost:9000` | Native APISIX management UI |
| **Milk Admin UI** | `http://localhost:5001` | Custom Blazor Dashboard |
| **Milk Backend API** | `http://localhost:5000` | Management API & Security Automation |
| **Grafana** | `http://localhost:3000` | Performance & Audit Dashboards |
| **Kibana** | `http://localhost:5601` | Log analysis and search |
| **Jaeger UI** | `http://localhost:16686` | Distributed tracing |
| **Prometheus** | `http://localhost:9090` | Metrics collection |

## 5. Basic Usage & Functional Verification

### 5.1 Accessing the Admin UI
Open `http://localhost:5001` in your browser. You can view the API inventory, managed consumers, and active blacklists.

### 5.2 Registering an API (Example)
You can register a new route through the Milk Admin UI or via the APISIX Admin API directly:
```bash
curl http://127.0.0.1:9080/apisix/admin/routes/1 -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c88b' -X PUT -d '
{
    "uri": "/get",
    "upstream": {
        "type": "roundrobin",
        "nodes": {
            "httpbin.org:80": 1
        }
    }
}'
```

### 5.3 Verifying Rate Limiting
Test the pre-configured Gold/Silver tier limits:
```bash
# Test Gold Key (Limit: 1000/min)
curl -i http://localhost:9080/get -H "apikey: gold-key"

# Test Silver Key (Limit: 100/min)
curl -i http://localhost:9080/get -H "apikey: silver-key"
```

### 5.4 Checking Observability
- **Logs:** Go to Kibana (`http://localhost:5601`) and look for indices starting with `apisix-`.
- **Metrics:** Visit Grafana (`http://localhost:3000`) to view the "Audit & Security" dashboard.

## 6. Maintenance & Troubleshooting

- **Check Container Status:** `docker-compose ps`
- **View Infrastructure Logs:** `docker-compose logs -f <service-name>`
- **Reset Environment:** `docker-compose down -v` (Caution: this deletes all data in Etcd and Elasticsearch).
