#!/bin/bash
# sync-openapi-to-apisix.sh

# APISIX Admin API Configuration
APISIX_ADMIN_URL=${APISIX_ADMIN_URL:-"http://localhost:9180/apisix/admin"}
APISIX_ADMIN_KEY=${APISIX_ADMIN_KEY:-"edd1c9f034335f136f87ad84b625c8f1"}
OPENAPI_FILE="../portal/public/openapi.yaml"

# Target Service ID for the routes
TARGET_SERVICE_ID="insure-tech-backend"

# Check if yq is installed
if ! command -v yq &> /dev/null
then
    echo "yq could not be found. Please install yq to parse YAML."
    echo "e.g., sudo wget https://github.com/mikefarah/yq/releases/latest/download/yq_linux_amd64 -O /usr/bin/yq && sudo chmod +x /usr/bin/yq"
    exit 1
fi

echo "Starting OpenAPI to APISIX Sync..."

# Parse OpenAPI file and create/update routes
yq e '.paths | keys | .[]' "$OPENAPI_FILE" | while read -r path; do
    echo "Processing path: $path"
    
    # Extract methods for the path
    methods=$(yq e ".paths[\"$path\"] | keys | .[]" "$OPENAPI_FILE" | awk '{print toupper($0)}' | jq -R . | jq -s .)

    # Define route properties
    route_name=$(echo "$path" | sed 's/[^a-zA-Z0-9]/-/g' | sed 's/^-//' | sed 's/-$//')
    route_id="openapi-$route_name"
    route_uri=$path

    echo "  - Route ID: $route_id"
    echo "  - URI: $route_uri"
    echo "  - Methods: $methods"

    # Construct APISIX Route JSON payload
    ROUTE_PAYLOAD=$(cat <<EOF
{
    "name": "openapi-$route_name",
    "uri": "$route_uri",
    "methods": $methods,
    "service_id": "$TARGET_SERVICE_ID",
    "status": 1
}
EOF
)

    # Use curl to create/update the route in APISIX
    curl -i -X PUT "$APISIX_ADMIN_URL/routes/$route_id" \
    -H "X-API-KEY: $APISIX_ADMIN_KEY" \
    -H 'Content-Type: application/json' \
    -d "$ROUTE_PAYLOAD"

    echo "  - Synced route $route_id to APISIX."
    echo "---"
done

echo "Sync finished."
