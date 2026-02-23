# Milk API Manager SDK

本目錄包含透過 OpenAPI 規格自動生成的客戶端 SDK。

## C# SDK

檔案：`MilkApi.Client.cs`

### 使用方式

```csharp
// 1. 將 MilkApi.Client.cs 加入您的 .NET 專案

// 2. 呼叫 API
var client = new MilkApiClient("http://localhost:5001");
client.DefaultRequestHeaders.Add("X-API-KEY", "your-api-key");

var blacklist = await client.GetBlacklistAsync();
```

## Python SDK

資料夾：`python/`

### 安裝與使用

```bash
# 安裝
cd sdk/python
pip install .

# 使用
from openapi_client import ApiClient, Configuration
from openapi_client.api import blacklist_api

config = Configuration(host="http://localhost:5001")
config.api_key["X-API-KEY"] = "your-api-key"

client = ApiClient(config)
api = blacklist_api.BlacklistApi(client)
result = api.get_blacklist()
```

## 重新生成 SDK

```powershell
# C# SDK
./scripts/generate-sdk.ps1

# Python SDK
./scripts/generate-python-sdk.ps1
```

> **注意**：生成 SDK 前需確保後端服務已啟動（Swagger JSON 可用）。
