# SDK Generation Script for Milk API Manager
# This script generates a C# client SDK from the management API's OpenAPI definition.

$ErrorActionPreference = "Stop"

$SwaggerUrl = "http://localhost:5001/swagger/v1/swagger.json"
$OutputPath = "./sdk/MilkApi.Client.cs"
$Namespace = "MilkApi.Client"

Write-Host "Checking if NSwag is installed..." -ForegroundColor Cyan
if (!(Get-Command nswag -ErrorAction SilentlyContinue)) {
    Write-Host "NSwag CLI not found. You can install it via:" -ForegroundColor Yellow
    Write-Host "npm install -g nswag" -ForegroundColor Gray
    Write-Host "Or use the .NET tool version." -ForegroundColor Gray
    exit 1
}

if (!(Test-Path "./sdk")) {
    New-Item -ItemType Directory -Path "./sdk"
}

Write-Host "Downloading Swagger definition from $SwaggerUrl..." -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri $SwaggerUrl -OutFile "./swagger.json"
} catch {
    Write-Host "Failed to download Swagger. Is the backend running at :5001?" -ForegroundColor Red
    exit 1
}

Write-Host "Generating C# Client SDK..." -ForegroundColor Cyan
nswag openapi2csclient /input:swagger.json /output:$OutputPath /namespace:$Namespace

Write-Host "Done! SDK generated at: $OutputPath" -ForegroundColor Green
Remove-Item "./swagger.json"
