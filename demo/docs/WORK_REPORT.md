# MilkDemo 專案工作報告

> **專案名稱**: MilkDemo - API Manager System 展示專案  
> **報告日期**: 2025 年  
> **技術架構師**: Senior Architect  
> **專案類型**: 新建展示專案 (Demo / Showcase)

---

## 1. 專案目標

建立一個完整的前後端分離展示專案，使用 .NET 8 Blazor WebAssembly 開發：

- **驗證** Milk API Manager System 的核心功能
- **呈現** API 閘道管理、安全防護、稽核追蹤等企業級 API 治理能力
- **提供** 可操作的 Demo 環境，用於系統驗收與客戶展示

---

## 2. 交付成果摘要

| 項目 | 狀態 | 說明 |
|------|------|------|
| 系統架構設計 | ✅ 完成 | 前後端分離 + API Gateway 中間層 |
| Solution 專案建置 | ✅ 完成 | 4 個專案、slnx、NuGet 套件 |
| 共享模型/DTO | ✅ 完成 | 2 Models、4 DTOs、1 泛型分頁 |
| TDD 單元測試 | ✅ 完成 | 18 個測試全數通過 (GREEN) |
| 後端 Web API | ✅ 完成 | 3 Controllers、2 Services、JWT Auth、Seed Data |
| Blazor WASM 前端 | ✅ 完成 | 9 Pages、5 Services、2 Layouts、Dark Theme CSS |
| Docker 容器化 | ✅ 完成 | 2 Dockerfiles、docker-compose 整合 |
| API Manager 整合 | ✅ 完成 | APISIX 路由設定腳本、Gateway Proxy Controller |
| E2E 測試 | ✅ 完成 | API 測試 (17 cases) + UI 測試 (12 cases) |
| 功能操作手冊 | ✅ 完成 | 15 章、涵蓋全功能詳細說明 |
| 情境操作手冊 | ✅ 完成 | 10 個操作情境、23 張截圖位置 |

---

## 3. 架構設計

### 3.1 系統架構圖

```
┌─────────────────┐     ┌──────────────────┐     ┌────────────────────┐
│  Blazor WASM    │────▶│  APISIX Gateway  │────▶│  MilkDemo.Api      │
│  (Frontend)     │     │  (API Manager)   │     │  (Business API)    │
│  Port: 5002     │     │  Port: 9080      │     │  Port: 5003        │
└─────────────────┘     └──────────────────┘     └────────────────────┘
                               │                          │
                        ┌──────┴──────┐           ┌───────┴───────┐
                        │ Milk API    │           │ InMemory DB   │
                        │ Manager     │           │ (EF Core)     │
                        │ Port: 5001  │           └───────────────┘
                        └─────────────┘
```

### 3.2 專案結構

```
demo/
├── MilkDemo.slnx              # Solution (XML format)
├── MilkDemo.Shared/           # 共享層：Models + DTOs
├── MilkDemo.Api/              # 後端：Web API + JWT Auth
├── MilkDemo.WebApp/           # 前端：Blazor WASM (Standalone)
└── MilkDemo.Tests/            # 測試：xUnit + FluentAssertions + Moq
```

### 3.3 技術選型

| 技術決策 | 選擇 | 理由 |
|----------|------|------|
| 前端架構 | Blazor WASM (Standalone) | .NET 8 生態系、前後端分離、免伺服器 |
| 資料庫 | EF Core InMemory | Demo 用途，零配置，重啟重置 |
| 認證 | JWT Bearer Token | 產業標準、與 API Manager 一致 |
| UI 主題 | Dark Trading Terminal | 與現有 MilkAdmin 風格一致 |
| Docker 部署 | nginx (WASM) + aspnet (API) | 輕量 Alpine 映像 |
| 測試框架 | xUnit + Playwright | .NET 標準 + 業界主流 E2E |

---

## 4. 開發方法論

### 4.1 TDD (Test-Driven Development)

採用 **RED → GREEN → REFACTOR** 循環：

1. **RED**: 先撰寫單元測試（18 個測試案例）
2. **GREEN**: 實作 Service 層使測試通過
3. **REFACTOR**: 優化程式碼結構

#### 測試覆蓋

| 服務 | 測試類別 | 測試數量 | 狀態 |
|------|----------|----------|------|
| ProductService | ProductServiceTests | 10 | ✅ PASS |
| OrderService | OrderServiceTests | 8 | ✅ PASS |
| **合計** | | **18** | **全數通過** |

#### 測試案例清單

**ProductServiceTests (10):**
- CreateProduct_ShouldAddProduct
- GetProductById_Existing_ShouldReturn
- GetProductById_NonExisting_ShouldReturnNull
- GetProducts_ShouldReturnPaged
- GetProducts_WithCategory_ShouldFilter
- UpdateProduct_Existing_ShouldUpdate
- UpdateProduct_NonExisting_ShouldReturnNull
- DeleteProduct_Existing_ShouldDelete
- DeleteProduct_NonExisting_ShouldReturnFalse
- GetCategories_ShouldReturnDistinct

**OrderServiceTests (8):**
- CreateOrder_ShouldCalculateTotals
- CreateOrder_ShouldDeductStock
- CreateOrder_InsufficientStock_ShouldThrow
- GetOrders_WithStatusFilter_ShouldFilter
- UpdateOrderStatus_ShouldUpdate
- CancelOrder_ShouldRestoreStock
- CancelOrder_ShippedOrder_ShouldReturnFalse
- GetOrderById_ShouldIncludeItems

### 4.2 前後端分離原則

- 前端（Blazor WASM）完全獨立部署，透過 HTTP 呼叫後端
- 後端 API 無狀態設計，可水平擴展
- CORS 配置允許指定來源
- JWT Token 在前端記憶體管理

---

## 5. 功能實現明細

### 5.1 後端 API (MilkDemo.Api)

#### Controllers

| Controller | 端點數 | 功能 |
|-----------|--------|------|
| ProductsController | 6 | 商品 CRUD + 分類查詢 |
| OrdersController | 5 | 訂單 CRUD + 取消 |
| GatewayController | 4 | 閘道狀態 + 路由 + 稽核 + 黑名單代理 |

#### Services

| Service | 方法數 | 功能 |
|---------|--------|------|
| ProductService | 6 | 商品業務邏輯、分頁、分類 |
| OrderService | 5 | 訂單業務邏輯、庫存聯動、狀態管理 |

#### 種子資料

- 10 筆商品（4 個分類：Dairy, Snacks, Beverages, Pantry）
- 2 筆訂單（含訂單項目關聯）
- 4 個 Demo 帳號（admin, operator, viewer, demo）

### 5.2 前端 WebApp (MilkDemo.WebApp)

#### Pages (9 頁)

| 頁面 | 路由 | 功能 |
|------|------|------|
| Login | `/login` | JWT 登入、Demo 帳號資訊 |
| Dashboard | `/` | 5 指標卡片、最近商品/訂單、架構圖 |
| Products | `/products` | CRUD 表格、分頁、分類篩選、Modal 編輯 |
| Orders | `/orders` | 訂單列表、建立訂單、詳情、取消、狀態篩選 |
| Gateway | `/gateway` | APISIX/API Manager 狀態、路由 JSON、連線資訊 |
| Routes | `/routes` | 路由配置瀏覽、管理說明 |
| Security | `/security` | IP 黑名單、安全功能表、PII 遮罩示範 |
| Audit | `/audit` | 稽核日誌查詢、功能說明 |
| ApiTest | `/api-test` | HTTP 請求建構器、快速預設、回應檢視 |
| About | `/about` | 專案說明、架構、功能、技術堆疊 |

#### Services (5 個)

| Service | 用途 |
|---------|------|
| AuthService | 登入/登出、Token 管理 |
| DemoAuthStateProvider | Blazor 認證狀態管理 |
| ProductApiService | 商品 API 呼叫 |
| OrderApiService | 訂單 API 呼叫 |
| GatewayApiService | 閘道代理 API 呼叫 |

#### UI 設計

- **Dark Trading Terminal** 風格主題
- 背景色：#0d1117、表面色：#161b22
- 強調色：#58a6ff (藍)、#3fb950 (綠)、#f0883e (橘)
- 等寬字型用於數值顯示
- 完整響應式佈局

### 5.3 Docker 部署

| 檔案 | 用途 |
|------|------|
| `demo/MilkDemo.Api/Dockerfile` | API 服務映像 (aspnet:8.0-alpine) |
| `demo/MilkDemo.WebApp/Dockerfile` | 前端映像 (nginx:alpine) |
| `demo/MilkDemo.WebApp/nginx.conf` | SPA 路由 + 安全標頭 |
| `scripts/setup-demo-routes.sh` | APISIX 路由初始化腳本 |

Docker Compose 新增 2 個服務：
- `milk-demo-api` (5003:8080)
- `milk-demo-webapp` (5002:80)

### 5.4 E2E 測試

| 測試檔 | 測試數 | 範圍 |
|--------|--------|------|
| demo-api.spec.js | 17 | 認證、商品 CRUD、訂單、Gateway 整合 |
| demo-ui.spec.js | 12 | 登入流程、9 頁面載入、商品列表驗證 |
| **合計** | **29** | |

### 5.5 文件

| 文件 | 檔案 | 內容 |
|------|------|------|
| 功能操作手冊 | `demo/docs/FUNCTIONAL_MANUAL.md` | 15 章完整功能說明 |
| 情境操作手冊 | `demo/docs/SCENARIO_MANUAL.md` | 10 個操作情境 + 截圖位置 |
| 工作報告 | `demo/docs/WORK_REPORT.md` | 本文件 |

---

## 6. 品質保證

### 6.1 編譯驗證

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

全部 4 個專案零警告零錯誤編譯通過。

### 6.2 單元測試

```
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18
```

18 個測試全數通過。

### 6.3 安全措施

| 措施 | 實現 |
|------|------|
| 非 root 運行 | Dockerfile 使用 appuser (UID 1654) |
| Security Headers | nginx: X-Content-Type-Options, X-Frame-Options, Referrer-Policy |
| JWT 認證 | 寫入操作需認證、Token 8 小時過期 |
| CORS | 僅允許指定來源 |
| no-new-privileges | Docker security_opt 限制 |
| 密鑰管理 | 環境變數注入，非硬編碼 |

### 6.4 Docker Compose 驗證

`docker compose config` 語法驗證通過。

---

## 7. 已知限制

| 項目 | 說明 | 建議改進 |
|------|------|----------|
| InMemory DB | 重啟後資料重置 | 正式環境改用 PostgreSQL |
| Token 記憶體儲存 | 頁面重新整理後需重新登入 | 可加入 localStorage + Refresh Token |
| API Key 硬編碼預設值 | docker-compose 使用預設值 | 正式環境必須透過環境變數或密鑰管理覆寫 |
| 前端直呼後端 | 部分功能未經閘道 | 可配置全部流量走 APISIX |
| 無 HTTPS | 開發環境使用 HTTP | 正式環境配置 TLS/SSL |

---

## 8. 檔案清單

### 新增檔案（共 35 個）

```
demo/
├── MilkDemo.slnx
├── docs/
│   ├── FUNCTIONAL_MANUAL.md
│   ├── SCENARIO_MANUAL.md
│   └── WORK_REPORT.md
├── MilkDemo.Shared/
│   ├── MilkDemo.Shared.csproj
│   ├── GlobalUsings.cs
│   ├── Models/Product.cs
│   ├── Models/Order.cs
│   └── DTOs/AuthDtos.cs, BusinessDtos.cs
├── MilkDemo.Api/
│   ├── MilkDemo.Api.csproj
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   ├── Data/DemoDbContext.cs
│   ├── Services/IProductService.cs, ProductService.cs
│   ├── Services/IOrderService.cs, OrderService.cs
│   └── Controllers/ProductsController.cs, OrdersController.cs, GatewayController.cs
├── MilkDemo.WebApp/
│   ├── MilkDemo.WebApp.csproj
│   ├── Program.cs
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── _Imports.razor
│   ├── App.razor
│   ├── Layout/MainLayout.razor, EmptyLayout.razor
│   ├── Services/AuthService.cs, DemoAuthStateProvider.cs
│   ├── Services/ProductApiService.cs, OrderApiService.cs, GatewayApiService.cs
│   ├── Pages/Login.razor, Dashboard.razor, Products.razor, Orders.razor
│   ├── Pages/Gateway.razor, Routes.razor, Security.razor, Audit.razor
│   ├── Pages/ApiTest.razor, About.razor
│   ├── wwwroot/index.html, css/app.css, appsettings.json
└── MilkDemo.Tests/
    ├── MilkDemo.Tests.csproj
    ├── Services/ProductServiceTests.cs
    └── Services/OrderServiceTests.cs

修改檔案（1 個）:
└── docker-compose.yml (新增 milk-demo-api、milk-demo-webapp 服務)

E2E 測試新增（2 個）:
├── e2e/tests/demo-api.spec.js
└── e2e/tests/demo-ui.spec.js

腳本新增（1 個）:
└── scripts/setup-demo-routes.sh
```

---

## 9. 總結

本專案成功交付一個**完整的前後端分離 Demo 系統**，採用：

- **.NET 8 Blazor WebAssembly** 前端，具備 9 個功能頁面
- **ASP.NET Core Web API** 後端，提供 15 個 API 端點
- **TDD** 開發方法，18 個單元測試全數通過
- **E2E 自動化測試** 29 個測試案例
- **Docker Compose** 一鍵部署整合現有 API Manager 基礎設施
- **完整文件** 功能操作手冊 + 情境操作手冊

該 Demo 系統可有效展示 Milk API Manager System 的以下能力：

1. **API Gateway 流量管理** — 透過 APISIX 代理與路由重寫
2. **認證授權** — JWT Token + 角色權限控制
3. **安全防護** — IP 黑名單、PII 遮罩、速率限制
4. **稽核追蹤** — 操作日誌不可竄改、ELK 整合
5. **監控可觀測性** — Prometheus + Grafana + Jaeger
6. **容器化部署** — Docker Compose 一鍵啟動全部服務

---

*報告結束*
