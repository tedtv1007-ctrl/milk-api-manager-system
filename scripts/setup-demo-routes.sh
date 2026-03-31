#!/bin/bash
# Setup APISIX routes for MilkDemo API
# This script registers the Demo API as an upstream in APISIX and creates gateway routes
# so that the demo project traffic flows through the API Manager gateway.

APISIX_ADMIN_URL=${APISIX_ADMIN_URL:-"http://127.0.0.1:9180"}
APISIX_ADMIN_KEY=${APISIX_ADMIN_KEY:-"edd1c9f034335f136f87ad84b625c88b"}

echo "=== Setting up APISIX routes for MilkDemo ==="

# 1. Create upstream for MilkDemo API
echo "[1/3] Creating upstream: milk-demo-api..."
curl -s -o /dev/null -w "%{http_code}" -X PUT "$APISIX_ADMIN_URL/apisix/admin/upstreams/demo-api" \
  -H "X-API-KEY: $APISIX_ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "milk-demo-api",
    "desc": "MilkDemo Business API Backend",
    "type": "roundrobin",
    "nodes": {
      "milk-demo-api:8080": 1
    },
    "timeout": {
      "connect": 5,
      "send": 10,
      "read": 10
    },
    "retries": 2
  }'
echo ""

# 2. Create route for demo products API (through gateway)
echo "[2/3] Creating route: /demo/products..."
curl -s -o /dev/null -w "%{http_code}" -X PUT "$APISIX_ADMIN_URL/apisix/admin/routes/demo-products" \
  -H "X-API-KEY: $APISIX_ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "demo-products",
    "desc": "MilkDemo Products API via Gateway",
    "uri": "/demo/api/products*",
    "methods": ["GET", "POST", "PUT", "DELETE"],
    "upstream_id": "demo-api",
    "plugins": {
      "proxy-rewrite": {
        "regex_uri": ["^/demo(/.*)", "$1"]
      },
      "limit-count": {
        "count": 100,
        "time_window": 60,
        "rejected_code": 429
      }
    },
    "status": 1
  }'
echo ""

# 3. Create route for demo orders API (through gateway)
echo "[3/3] Creating route: /demo/orders..."
curl -s -o /dev/null -w "%{http_code}" -X PUT "$APISIX_ADMIN_URL/apisix/admin/routes/demo-orders" \
  -H "X-API-KEY: $APISIX_ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "demo-orders",
    "desc": "MilkDemo Orders API via Gateway",
    "uri": "/demo/api/orders*",
    "methods": ["GET", "POST", "PUT", "DELETE"],
    "upstream_id": "demo-api",
    "plugins": {
      "proxy-rewrite": {
        "regex_uri": ["^/demo(/.*)", "$1"]
      },
      "limit-count": {
        "count": 50,
        "time_window": 60,
        "rejected_code": 429
      }
    },
    "status": 1
  }'
echo ""

echo ""
echo "=== APISIX Demo Routes Setup Complete ==="
echo "Demo Products API: http://localhost:9080/demo/api/products"
echo "Demo Orders API:   http://localhost:9080/demo/api/orders"
echo ""
echo "Test with:"
echo "  curl http://localhost:9080/demo/api/products"
echo "  curl http://localhost:9080/demo/api/orders"
