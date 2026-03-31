# MilkDemo 系統功能操作手冊

> **版本**: 1.0  
> **建立日期**: 2025 年  
> **適用對象**: 系統管理員、開發人員、測試人員  
> **架構**: .NET 8 Blazor WebAssembly + ASP.NET Core Web API + Milk API Manager System

---

## 目錄

1. [系統概述](#1-系統概述)
2. [環境需求與啟動方式](#2-環境需求與啟動方式)
3. [認證系統 (Authentication)](#3-認證系統-authentication)
4. [儀表板 (Dashboard)](#4-儀表板-dashboard)
5. [商品管理 (Products)](#5-商品管理-products)
6. [訂單管理 (Orders)](#6-訂單管理-orders)
7. [API 閘道 (Gateway)](#7-api-閘道-gateway)
8. [路由管理 (Routes)](#8-路由管理-routes)
9. [安全防護 (Security)](#9-安全防護-security)
10. [稽核日誌 (Audit)](#10-稽核日誌-audit)
11. [API 測試工具 (API Test)](#11-api-測試工具-api-test)
12. [關於系統 (About)](#12-關於系統-about)
13. [API 端點參考](#13-api-端點參考)
14. [Docker 部署](#14-docker-部署)
15. [故障排除](#15-故障排除)

---

## 1. 系統概述

### 1.1 系統目的

MilkDemo 是一個完整的前後端分離展示專案，用於**驗證和呈現 Milk API Manager System 的核心功能**。透過模擬真實的商品與訂單管理業務 API，展示 API 閘道如何進行流量管理、安全防護、稽核追蹤等企業級 API 治理能力。

### 1.2 系統架構

```
┌─────────────────┐     ┌──────────────────┐     ┌────────────────────┐
│  Blazor WASM    │────▶│  APISIX Gateway  │────▶│  MilkDemo.Api      │
│  (Frontend)     │     │  (API Manager)   │     │  (Business API)    │
│  Port: 5002     │     │  Port: 9080      │     │  Port: 5003        │
└─────────────────┘     └──────────────────┘     └────────────────────┘
                               │                          │
                        ┌──────┴──────┐           ┌───────┴───────┐
                        │ Milk API    │           │ InMemory DB   │
                        │ Manager     │           │ (Products,    │
                        │ Port: 5001  │           │  Orders)      │
                        └─────────────┘           └───────────────┘
```

### 1.3 技術堆疊

| 層級 | 技術 | 版本 |
|------|------|------|
| 前端 | Blazor WebAssembly (Standalone) | .NET 8 |
| 後端 API | ASP.NET Core Web API | .NET 8 |
| 資料庫 | EF Core InMemory | 8.0 |
| API 閘道 | Apache APISIX | 3.11 |
| API 管理 | Milk API Manager | Custom |
| 認證機制 | JWT Bearer Token | - |
| 容器化 | Docker + nginx | Alpine |
| 監控 | Prometheus + Grafana | - |
| 日誌 | ELK Stack (Elasticsearch / Logstash / Kibana) | 9.2.3 |

### 1.4 專案結構

```
demo/
├── MilkDemo.slnx                  # Solution 檔案
├── MilkDemo.Shared/               # 共享模型與 DTO
│   ├── Models/
│   │   ├── Product.cs             # 商品模型
│   │   └── Order.cs               # 訂單與訂單項目模型
│   └── DTOs/
│       ├── AuthDtos.cs            # 認證相關 DTO
│       └── BusinessDtos.cs        # 業務操作 DTO
├── MilkDemo.Api/                  # 後端 Web API
│   ├── Program.cs                 # 應用程式入口（含種子資料）
│   ├── Dockerfile                 # Docker 建置檔
│   ├── appsettings.json           # 配置檔
│   ├── Data/DemoDbContext.cs      # 資料庫上下文
│   ├── Services/                  # 商品/訂單業務邏輯
│   └── Controllers/               # API 端點控制器
├── MilkDemo.WebApp/               # Blazor WASM 前端
│   ├── Program.cs                 # WASM 入口
│   ├── Dockerfile                 # Docker 建置檔 (nginx)
│   ├── nginx.conf                 # nginx 配置
│   ├── Layout/                    # 版面配置
│   ├── Pages/                     # 頁面元件 (9 頁)
│   ├── Services/                  # API 呼叫服務
│   └── wwwroot/                   # 靜態資源 + CSS
└── MilkDemo.Tests/                # 單元測試 (18 個測試)
    └── Services/                  # 服務層測試
```

---

## 2. 環境需求與啟動方式

### 2.1 系統需求

- **.NET 8 SDK** (8.0.x 或更新版本)
- **Docker Desktop** (用於容器化部署)
- **Node.js 18+** (用於 E2E 測試)
- **瀏覽器**: Chrome / Edge / Firefox（支援 WebAssembly）

### 2.2 本機開發啟動

#### 啟動後端 API

```bash
cd demo/MilkDemo.Api
dotnet run
# API 運行於 http://localhost:5003
# Swagger UI: http://localhost:5003/swagger
```

#### 啟動前端 WebApp

```bash
cd demo/MilkDemo.WebApp
dotnet run
# WebApp 運行於 http://localhost:5002
```

### 2.3 Docker 啟動（完整環境）

```bash
# 從專案根目錄啟動所有服務（含 API Manager、APISIX 等）
docker compose up -d

# 設置 APISIX 路由（將 Demo API 註冊到閘道）
bash scripts/setup-demo-routes.sh
```

### 2.4 執行測試

```bash
# 單元測試
cd demo
dotnet test

# E2E 測試
cd e2e
npx playwright test tests/demo-api.spec.js   # API 測試
npx playwright test tests/demo-ui.spec.js    # UI 測試
```

### 2.5 服務端口對照表

| 服務 | 端口 | 用途 |
|------|------|------|
| MilkDemo.WebApp | 5002 | Blazor 前端 |
| MilkDemo.Api | 5003 | 業務 API |
| Milk API Manager | 5001 | API 管理後端 |
| Milk Admin UI | 5000 | 管理介面 |
| APISIX Gateway | 9080 | API 閘道 |
| APISIX Dashboard | 9000 | APISIX 管理介面 |
| Swagger | 5003/swagger | API 文件 |
| Prometheus | 9090 | 監控指標 |
| Grafana | 3000 | 監控儀表板 |
| Kibana | 5601 | 日誌查詢 |
| Jaeger | 16686 | 分散式追蹤 |

---

## 3. 認證系統 (Authentication)

### 3.1 功能說明

- **路由**: `/login`
- **認證方式**: JWT Bearer Token
- **Token 有效期**: 8 小時
- **支援角色**: Admin、Operator、Viewer

### 3.2 Demo 帳號

| 帳號 | 密碼 | 角色 | 權限說明 |
|------|------|------|----------|
| `admin` | `admin` | Admin, Operator, Viewer | 完整管理權限 |
| `operator` | `operator` | Operator, Viewer | 操作與查閱權限 |
| `viewer` | `viewer` | Viewer | 唯讀查閱權限 |
| `demo` | `demo` | Viewer | 基本展示權限 |

### 3.3 操作步驟

1. 開啟瀏覽器前往 `http://localhost:5002`
2. 系統自動導向登入頁面 `/login`
3. 輸入帳號與密碼
4. 點擊 **Login** 按鈕
5. 成功後自動跳轉至 Dashboard

### 3.4 技術細節

- Token 儲存在前端記憶體中（非 localStorage，提升安全性）
- 每次 API 請求自動附帶 `Authorization: Bearer {token}` 標頭
- Token 過期後需重新登入
- 登出後清除所有認證狀態

### 3.5 API 端點

```
POST /api/auth/login
Request Body: { "username": "admin", "password": "admin" }
Response: {
  "token": "eyJhbG...",
  "expiresAt": "2025-...",
  "displayName": "Admin User",
  "roles": ["Admin", "Operator", "Viewer"]
}
```

---

## 4. 儀表板 (Dashboard)

### 4.1 功能說明

- **路由**: `/`
- **用途**: 系統總覽頁面，即時展示關鍵指標與系統狀態

### 4.2 功能區塊

#### 4.2.1 指標卡片 (Metrics Grid)

顯示五個即時指標：

| 指標 | 說明 | 顏色 |
|------|------|------|
| Total Products | 商品總數 | 藍色 |
| Total Orders | 訂單總數 | 綠色 |
| Pending Orders | 待處理訂單數 | 黃色 |
| Gateway Status | APISIX 閘道狀態 (Online/Offline) | 青色 |
| API Manager | Milk API Manager 狀態 (Online/Offline) | 紫色 |

#### 4.2.2 最近商品表格 (Recent Products)

顯示最新 5 筆商品資訊：
- Name（名稱）
- Category（分類）
- Price（價格）
- Stock（庫存）

#### 4.2.3 最近訂單表格 (Recent Orders)

顯示最新 5 筆訂單資訊：
- Customer（客戶名稱）
- Total（訂單金額）
- Status（訂單狀態，含顏色標記）

#### 4.2.4 系統架構圖

以 ASCII Art 展示前後端分離架構圖，說明資料流向。

### 4.3 資料更新

- 頁面載入時自動平行載入所有資料
- 所有 API 呼叫獨立進行，單一 API 失敗不影響其他區塊顯示

---

## 5. 商品管理 (Products)

### 5.1 功能說明

- **路由**: `/products`
- **用途**: 完整的商品 CRUD（新增、讀取、更新、刪除）管理介面

### 5.2 功能清單

#### 5.2.1 商品列表

- 表格欄位：ID、Name、Description、Category、Price、Stock、Status、Actions
- 支援**分頁瀏覽**（每頁 10 筆，顯示頁碼與總數）
- 支援**按分類篩選**（Dairy、Snacks、Beverages、Pantry 等）
- 商品狀態顯示：Active（綠色）/ Inactive（紅色）

#### 5.2.2 新增商品

1. 點擊右上角 **+ New Product** 按鈕
2. 在彈出對話框中填寫：
   - **Name**（必填）：商品名稱
   - **Description**：商品描述
   - **Price**（必填）：價格（支援小數點）
   - **Stock**（必填）：庫存數量
   - **Category**：商品分類
3. 點擊 **Create** 完成新增

#### 5.2.3 編輯商品

1. 在商品列表中點擊 **Edit** 按鈕
2. 在對話框中修改欄位值
3. 點擊 **Update** 儲存變更

#### 5.2.4 刪除商品

1. 在商品列表中點擊 **Delete** 按鈕
2. 商品立即被刪除（軟刪除）
3. 列表自動重新載入

### 5.3 種子資料

系統啟動時自動載入 10 筆示範商品：

| 商品名稱 | 分類 | 價格 | 庫存 |
|----------|------|------|------|
| Premium Milk | Dairy | $89.00 | 500 |
| Low-Fat Yogurt | Dairy | $65.00 | 300 |
| Cheddar Cheese | Dairy | $120.00 | 150 |
| Butter Cookies | Snacks | $199.00 | 200 |
| Green Tea | Beverages | $250.00 | 100 |
| Coffee Beans | Beverages | $380.00 | 80 |
| Organic Honey | Pantry | $320.00 | 60 |
| Dark Chocolate | Snacks | $95.00 | 250 |
| Oat Milk | Dairy | $79.00 | 400 |
| Sparkling Water | Beverages | $35.00 | 1000 |

### 5.4 API 端點

| 方法 | 路徑 | 認證 | 說明 |
|------|------|------|------|
| GET | `/api/products` | 不需要 | 取得商品列表（分頁、分類篩選） |
| GET | `/api/products/{id}` | 不需要 | 取得單一商品 |
| GET | `/api/products/categories` | 不需要 | 取得所有分類 |
| POST | `/api/products` | **需要** | 新增商品 |
| PUT | `/api/products/{id}` | **需要** | 更新商品 |
| DELETE | `/api/products/{id}` | **需要** | 刪除商品 |

查詢參數：
- `page`（預設 1）
- `pageSize`（預設 10）
- `category`（篩選分類）

---

## 6. 訂單管理 (Orders)

### 6.1 功能說明

- **路由**: `/orders`
- **用途**: 訂單建立、查詢、狀態管理與取消操作

### 6.2 功能清單

#### 6.2.1 訂單列表

- 表格欄位：ID、Customer、Email、Phone、Amount、Status、Created、Actions
- 支援**按狀態篩選**（All / Pending / Confirmed / Shipped / Delivered / Cancelled）
- 狀態顏色標記：
  - Pending → 黃色
  - Confirmed → 藍色
  - Shipped → 青色
  - Delivered → 綠色
  - Cancelled → 紅色

#### 6.2.2 建立訂單

1. 點擊 **+ New Order** 按鈕
2. 填寫客戶資訊：
   - **Customer Name**（必填）
   - **Email**
   - **Phone**
3. 選擇商品與數量：
   - 從下拉選單選擇商品
   - 輸入購買數量
   - 點擊 **Add Item** 加入訂單
   - 可新增多個商品項目
4. 確認訂單小計後點擊 **Submit Order**

#### 6.2.3 訂單詳情

1. 在訂單列表中點擊 **Detail** 按鈕
2. 彈出對話框顯示：
   - 客戶資訊（姓名、Email、電話）
   - 訂單狀態與建立/更新時間
   - 訂單項目明細（商品名、數量、單價、小計）
   - 訂單總金額

#### 6.2.4 取消訂單

1. 在訂單列表中點擊 **Cancel** 按鈕
2. 僅限 Pending 或 Confirmed 狀態的訂單可以取消
3. 取消後**自動回補商品庫存**
4. 已出貨 (Shipped) 或已送達 (Delivered) 的訂單無法取消

### 6.3 庫存聯動

- 建立訂單時自動**扣減商品庫存**
- 庫存不足時拋出錯誤，無法下單
- 取消訂單時自動**回補庫存**到對應商品

### 6.4 種子資料

系統啟動時載入 2 筆示範訂單：
- Alice Wang 的訂單（Confirmed, $254.00）
- Bob Chen 的訂單（Pending, $630.00）

### 6.5 API 端點

| 方法 | 路徑 | 認證 | 說明 |
|------|------|------|------|
| GET | `/api/orders` | **需要** | 取得訂單列表 |
| GET | `/api/orders/{id}` | **需要** | 取得訂單詳情（含項目） |
| POST | `/api/orders` | **需要** | 建立新訂單 |
| PUT | `/api/orders/{id}/status` | **需要** | 更新訂單狀態 |
| PUT | `/api/orders/{id}/cancel` | **需要** | 取消訂單 |

---

## 7. API 閘道 (Gateway)

### 7.1 功能說明

- **路由**: `/gateway`
- **用途**: 展示 APISIX 閘道與 Milk API Manager 的即時連線狀態

### 7.2 功能區塊

#### 7.2.1 APISIX Gateway 狀態

- 顯示 APISIX 閘道是否在線 (Online/Offline)
- 顯示閘道 URL（預設 `http://localhost:9080`）
- 即時偵測連線狀態

#### 7.2.2 API Manager 狀態

- 顯示 Milk API Manager 是否在線
- 顯示管理端 URL（預設 `http://localhost:5001`）
- 透過 Demo API 代理查詢

#### 7.2.3 路由配置 (Routes JSON)

- 展示從 API Manager 取得的路由配置 JSON
- 使用程式碼格式化顯示
- 點擊 **Refresh** 重新載入

#### 7.2.4 連線資訊表

| 欄目 | 說明 |
|------|------|
| APISIX 閘道 | 閘道位址與端口 |
| API Manager | 管理端位址 |
| Demo API | 業務 API 端點 |
| 閘道路由前綴 | `/demo/api/*` |

### 7.3 閘道流量路徑

透過閘道存取 Demo API 的路由對應：

| 閘道路徑 | 目標路徑 | 說明 |
|----------|----------|------|
| `GET /demo/api/products` | `GET /api/products` | 商品列表 |
| `POST /demo/api/products` | `POST /api/products` | 新增商品 |
| `GET /demo/api/orders` | `GET /api/orders` | 訂單列表 |
| `POST /demo/api/orders` | `POST /api/orders` | 建立訂單 |

---

## 8. 路由管理 (Routes)

### 8.1 功能說明

- **路由**: `/routes`
- **用途**: 查看 API Manager 中的路由配置與管理說明

### 8.2 功能區塊

- 從 API Manager 後端取得已設定的路由規則列表
- 顯示每條路由的 URI Pattern、上游服務、啟用的 Plugin
- 說明如何透過 API Manager 或 APISIX Dashboard 管理路由

### 8.3 路由管理方式

1. **APISIX Dashboard** (`http://localhost:9000`)：圖形化管理路由
2. **Milk API Manager** (`http://localhost:5001`)：透過管理 API 自動同步
3. **Admin API** (`http://localhost:9180`)：APISIX REST Admin API 直接操作

---

## 9. 安全防護 (Security)

### 9.1 功能說明

- **路由**: `/security`
- **用途**: 展示 API Manager 的安全防護功能

### 9.2 功能區塊

#### 9.2.1 IP 黑名單 (Blacklist)

- 從 API Manager 取得當前 IP 黑名單清單
- 顯示被封鎖的 IP 地址列表
- 展示黑名單與 APISIX `traffic-blocker` Plugin 的同步機制

#### 9.2.2 安全功能一覽表

| 功能 | 說明 | 實現方式 |
|------|------|----------|
| JWT 認證 | JSON Web Token 驗證 | ASP.NET Core JwtBearer |
| IP 黑名單 | 封鎖惡意 IP | traffic-blocker.lua Plugin |
| PII 遮罩 | 個人資訊遮蔽 | pii-masker.lua Plugin |
| 速率限制 | API 呼叫頻率限制 | limit-count Plugin |
| CORS | 跨域資源共享控制 | ASP.NET CORS Middleware |
| HTTPS | 傳輸加密 | APISIX SSL / Kestrel |

#### 9.2.3 PII 遮罩示範

展示 PII (Personally Identifiable Information) 遮罩功能的效果：

| 原始資料 | 遮罩後 |
|----------|--------|
| `alice@example.com` | `a***@example.com` |
| `0912345678` | `091****678` |
| `A123456789` | `A1234****9` |

---

## 10. 稽核日誌 (Audit)

### 10.1 功能說明

- **路由**: `/audit`
- **用途**: 查看 API Manager 記錄的操作稽核日誌

### 10.2 功能區塊

#### 10.2.1 稽核日誌列表

- 從 API Manager 取得稽核日誌記錄
- 支援設定查詢筆數（10, 25, 50, 100）
- 每筆日誌包含：
  - 操作時間 (Timestamp)
  - 操作者 (Actor)
  - 動作 (Action)
  - 目標資源 (Target)
  - 結果 (Result)
  - IP 位址 (Source IP)

#### 10.2.2 稽核功能說明表

| 功能 | 說明 |
|------|------|
| 自動記錄 | 所有 API 管理操作自動記錄 |
| 不可竄改 | 日誌一經寫入無法修改 |
| ELK 整合 | 透過 Logstash 轉發至 Elasticsearch |
| Grafana 儀表板 | 視覺化稽核數據 |
| Outbox Pattern | 確保日誌不遺失的持久化機制 |

### 10.3 稽核範圍

以下操作會自動記錄：
- 路由的建立、修改、刪除
- 黑名單的新增、移除
- 認證嘗試（成功與失敗）
- 系統配置變更
- API Key 的建立與撤銷

---

## 11. API 測試工具 (API Test)

### 11.1 功能說明

- **路由**: `/api-test`
- **用途**: 互動式 HTTP 請求建構器，可直接在前端測試 API 端點

### 11.2 功能區塊

#### 11.2.1 請求建構器

- **HTTP Method** 選擇：GET、POST、PUT、DELETE
- **URL** 輸入：完整的 API URL
- **Request Body**（JSON 格式，用於 POST/PUT）
- **Send Request** 按鈕發送請求

#### 11.2.2 快速預設 (Quick Presets)

提供常用 API 端點的快速填入按鈕：

| 預設名稱 | Method | URL |
|----------|--------|-----|
| Health Check | GET | `http://localhost:5003/health` |
| Products | GET | `http://localhost:5003/api/products` |
| Orders (Auth) | GET | `http://localhost:5003/api/orders` |
| Categories | GET | `http://localhost:5003/api/products/categories` |
| Gateway (APISIX) | GET | `http://localhost:9080/demo/api/products` |

#### 11.2.3 回應檢視器

- 顯示 HTTP 回應狀態碼（含顏色標記：2xx 綠色、4xx 黃色、5xx 紅色）
- 回應時間（毫秒）
- 回應 Body（格式化 JSON 顯示）

### 11.3 使用範例

**測試 Health Check:**
1. 點擊 **Health Check** 預設按鈕
2. URL 自動填入 `http://localhost:5003/health`
3. 點擊 **Send Request**
4. 預期回傳 `200 OK` 與 `{ "status": "Healthy", "timestamp": "..." }`

**測試商品建立:**
1. 選擇 **POST** 方法
2. 輸入 URL: `http://localhost:5003/api/products`
3. 填入 Body:
   ```json
   {
     "name": "Test Product",
     "description": "Created via API Test",
     "price": 50.00,
     "stockQuantity": 100,
     "category": "Test"
   }
   ```
4. 點擊 **Send Request**
5. 預期回傳 `201 Created`（需先登入取得 Token）

---

## 12. 關於系統 (About)

### 12.1 功能說明

- **路由**: `/about`
- **用途**: 專案概述、架構說明、功能列表、技術堆疊

### 12.2 內容區塊

- **專案介紹**: MilkDemo 系統的目的與設計理念
- **系統架構圖**: ASCII Art 架構圖
- **功能特色表**: 9 大功能的詳細說明
- **技術堆疊表**: 所有使用的框架與工具版本

---

## 13. API 端點參考

### 13.1 認證端點

| 端點 | 方法 | 認證 | 說明 |
|------|------|------|------|
| `/api/auth/login` | POST | 不需要 | 使用者登入 |
| `/health` | GET | 不需要 | 健康檢查 |

### 13.2 商品端點

| 端點 | 方法 | 認證 | 說明 |
|------|------|------|------|
| `/api/products` | GET | 不需要 | 分頁查詢商品 |
| `/api/products/{id}` | GET | 不需要 | 取得單一商品 |
| `/api/products/categories` | GET | 不需要 | 取得分類列表 |
| `/api/products` | POST | 需要 | 新增商品 |
| `/api/products/{id}` | PUT | 需要 | 更新商品 |
| `/api/products/{id}` | DELETE | 需要 | 刪除商品 |

### 13.3 訂單端點

| 端點 | 方法 | 認證 | 說明 |
|------|------|------|------|
| `/api/orders` | GET | 需要 | 分頁查詢訂單 |
| `/api/orders/{id}` | GET | 需要 | 取得訂單詳情 |
| `/api/orders` | POST | 需要 | 建立訂單 |
| `/api/orders/{id}/status` | PUT | 需要 | 更新訂單狀態 |
| `/api/orders/{id}/cancel` | PUT | 需要 | 取消訂單 |

### 13.4 Gateway 端點

| 端點 | 方法 | 認證 | 說明 |
|------|------|------|------|
| `/api/gateway/status` | GET | 需要 | 取得閘道狀態 |
| `/api/gateway/routes` | GET | 需要 | 取得路由配置 |
| `/api/gateway/audit-logs` | GET | 需要 | 取得稽核日誌 |
| `/api/gateway/blacklist` | GET | 需要 | 取得 IP 黑名單 |

---

## 14. Docker 部署

### 14.1 Demo API Dockerfile

- 基於 `mcr.microsoft.com/dotnet/sdk:8.0-alpine` 建置
- 使用 `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` 運行
- 運行使用者：非 root (appuser:1654)
- 健康檢查：每 15 秒檢查 `/health`

### 14.2 Demo WebApp Dockerfile

- 使用 .NET SDK 建置 Blazor WASM
- 使用 `nginx:alpine` 部署靜態檔案
- nginx 配置支援 SPA 路由 (`try_files $uri /index.html`)
- 安全標頭：X-Content-Type-Options、X-Frame-Options、Referrer-Policy

### 14.3 Docker Compose 服務

```yaml
milk-demo-api:     # 業務 API  (5003 → 8080)
milk-demo-webapp:  # Blazor UI  (5002 → 80)
```

兩個服務均加入 `apisix` 網路，可直接與 APISIX 閘道和 API Manager 通訊。

### 14.4 環境變數

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `JWT_SECRET` | `milk-demo-jwt-secret-key-change-in-production-32chars!` | JWT 簽章密鑰 |
| `API_AUTH_KEY` | `milk-admin-secret-key-change-me` | API Manager 認證金鑰 |
| `APISIX_ADMIN_KEY` | `edd1c9f034335f136f87ad84b625c88b` | APISIX Admin API Key |

---

## 15. 故障排除

### 15.1 常見問題

#### Q: 無法登入
- 確認 Demo API 是否運行於 `http://localhost:5003`
- 確認使用正確的 Demo 帳號密碼
- 檢查瀏覽器 Console 是否有 CORS 錯誤

#### Q: Gateway 顯示 Offline
- 確認 Docker 服務是否全部啟動：`docker compose ps`
- 確認 APISIX 容器是否正常：`curl http://localhost:9080/apisix/status`
- 確認 API Manager 是否正常：`curl http://localhost:5001/health/ready`

#### Q: 商品列表為空
- Demo API 使用 InMemory 資料庫，重啟後資料會重置
- 種子資料在啟動時自動載入 10 筆商品

#### Q: E2E 測試失敗
- 確認 Demo API 和 WebApp 都在運行
- 確認 Playwright 已安裝瀏覽器：`npx playwright install`
- 檢查測試截圖：`e2e/test-results/`

#### Q: Docker 建置失敗
- 確認 Docker Desktop 已啟動
- 確認 .NET 8 SDK 在 Docker 中可用
- 檢查 `demo/` 目錄下所有 csproj 路徑是否正確

### 15.2 日誌查看

```bash
# Demo API 日誌
docker compose logs milk-demo-api -f

# WebApp (nginx) 日誌
docker compose logs milk-demo-webapp -f

# 所有服務日誌
docker compose logs -f
```

---

*本手冊最後更新：2025 年*
