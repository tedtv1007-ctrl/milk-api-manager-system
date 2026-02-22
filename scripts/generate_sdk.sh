#!/usr/bin/env bash
set -e
# generate_sdk.sh - generate C# SDK from artifacts/openapi.json using openapi-generator
OPENAPI_FILE="$(dirname "$0")/../artifacts/openapi.json"
OUT_DIR="$(dirname "$0")/../artifacts/sdk/csharp-client"
mkdir -p "$OUT_DIR"
if command -v openapi-generator >/dev/null 2>&1; then
  echo "Using openapi-generator CLI to generate SDK..."
  openapi-generator generate -i "$OPENAPI_FILE" -g csharp -o "$OUT_DIR" --additional-properties=packageName=MilkApiClient
  echo "SDK generated to $OUT_DIR"
else
  echo "openapi-generator CLI not found. Please install it or run this script in CI with openapi-generator available."
  echo "As an alternative, use Docker:"
  echo " docker run --rm -v $(pwd):/local openapitools/openapi-generator-cli generate -i /local/milk-api-manager-system/artifacts/openapi.json -g csharp -o /local/milk-api-manager-system/artifacts/sdk/csharp-client --additional-properties=packageName=MilkApiClient"
  exit 0
fi
