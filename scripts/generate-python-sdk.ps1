# SDK Generation Script for Milk API Manager (Python)
# This script generates a Python client SDK from the management API's OpenAPI definition using openapi-generator-cli.

$ErrorActionPreference = "Stop"

$SwaggerUrl = "http://localhost:5001/swagger/v1/swagger.json"
$OutputPath = "./sdk/python"
$PackageName = "milk_api_client"

Write-Host "Checking for Docker (required for openapi-generator-cli)..." -ForegroundColor Cyan
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker not found. Please install Docker." -ForegroundColor Red
    exit 1
}

Write-Host "Downloading Swagger definition from $SwaggerUrl..." -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri $SwaggerUrl -OutFile "./swagger.json"
}
catch {
    Write-Host "Failed to download Swagger. Is the backend running at :5001?" -ForegroundColor Red
    exit 1
}

Write-Host "Generating Python Client SDK using Docker..." -ForegroundColor Cyan
docker run --rm -v "${PWD}:/local" openapitools/openapi-generator-cli generate -i /local/swagger.json -g python -o /local/$OutputPath --additional-properties=packageName=$PackageName

Write-Host "Done! Python SDK generated at: $OutputPath" -ForegroundColor Green
Remove-Item "./swagger.json"
