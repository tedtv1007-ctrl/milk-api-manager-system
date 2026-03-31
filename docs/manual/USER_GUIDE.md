# 📖 Milk API Manager — 功能操作手冊 v3.0

> **Blazor Server + MudBlazor 8 + Apache APISIX 3.11** — 企業級 API 全生命週期管理與安全治理平台完整操作指南  
> UI 風格：Dark Trading Terminal Theme | 管理介面：`http://localhost:5000` | API 端點：`http://localhost:5001`  
> 最後更新：2026-03-31

---

## 目錄

1. [系統概述](#1-系統概述)
2. [快速開始](#2-快速開始)
3. [認證與權限管理 (RBAC)](#3-認證與權限管理-rbac)
4. [Gateway Dashboard — 系統總覽](#4-gateway-dashboard--系統總覽)
5. [網關核心管理 (GATEWAY)](#5-網關核心管理-gateway)
   - 5.1 Routes Management
   - 5.2 Services Management
   - 5.3 Upstreams Management
   - 5.4 SSL Certificate Management
   - 5.5 Global Plugin Rules
6. [API 治理與開發者體驗 (API / DEVELOPER)](#6-api-治理與開發者體驗-api--developer)
   - 6.1 API Inventory
   - 6.2 API List
   - 6.3 Developer Hub
   - 6.4 Mock Lab
   - 6.5 Access Request (自助申請)
7. [安全防護與合規 (OPERATIONS)](#7-安全防護與合規-operations)
   - 7.1 PII Protection
   - 7.2 IP Blacklist
   - 7.3 IP Whitelist (路由級)
   - 7.4 Consumers 管理
   - 7.5 Consumer Groups (Traffic Tiers)
   - 7.6 Audit Logs
   - 7.7 Alert Rules
   - 7.8 Reports
8. [監控與可觀測性](#8-監控與可觀測性)
   - 8.1 Traffic Intelligence Center
   - 8.2 Stress Test Center
   - 8.3 Sync Status
   - 8.4 外部監控服務
9. [進階功能 (REST API)](#9-進階功能-rest-api)
   - 9.1 Canary Release 灰度發布
   - 9.2 Circuit Breaker 熔斷器
   - 9.3 Cache Policy 快取策略
   - 9.4 Health Check 健康檢查
   - 9.5 Transform Rules 請求轉換
   - 9.6 API Lifecycle 生命週期管理
   - 9.7 Test Execution 測試執行
10. [REST API 完整參考](#10-rest-api-完整參考)
11. [SDK 整合指南](#11-sdk-整合指南)
12. [疑難排解](#12-疑難排解)
13. [附錄 A — 頁面路由總覽](#附錄-a--頁面路由總覽)
14. [附錄 B — REST API 端點速查表](#附錄-b--rest-api-端點速查表)
15. [附錄 C — 環境變數參考](#附錄-c--環境變數參考)

---

## 1. 系統概述

**Milk API Manager System** 是基於 Apache APISIX 構建的企業級 API 管理平台，專為企業內部網路 (Intranet) 設計。系統提供從 API 設計、測試、防禦到分析的一站式解決方案。

### 核心架構

| 層級 | 技術 | 說明 |
|------|------|------|
| **控制平面 (Control Plane)** | .NET 8 + Blazor Server + MudBlazor 8 | 管理 UI + RESTful API (30 個 Controller) |
| **資料平面 (Data Plane)** | Apache APISIX 3.11 + Custom Lua Plugins | 高效能 API Gateway |
| **資料層** | PostgreSQL 17 + Entity Framework Core | 持久化儲存 |
| **可觀測層** | Prometheus + Grafana + Jaeger + ELK 9.2.3 | 全方位監控 |
| **身份驗證** | LDAP/AD + JWT + API Key | 企業 SSO 整合 |

### 系統元件與端口

| 服務 | Image | 端口 | 用途 |
|------|-------|------|------|
| **Blazor Admin UI** | MilkAdminBlazor | `5000` | 全功能管理控制面板 |
| **Backend API** | MilkApiManager | `5001` | REST API 端點 |
| **Auto-Defense Worker** | MilkWorker | — | 自動封鎖 + Outbox 同步 |
| **APISIX Gateway** | apache/apisix:3.11.0 | `9080` | 流量入口 |
| **APISIX Admin** | (同上) | `9180` | 管理 API |
| **etcd** | coreos/etcd:v3.5.15 | `2379` | 配置儲存 |
| **PostgreSQL** | postgres:17-alpine | `5432` | 應用資料庫 |
| **Prometheus** | prom/prometheus:v3.2.1 | `9090` | 指標收集 |
| **Grafana** | grafana/grafana:11.5.2 | `3000` | 可視化儀表板 |
| **Elasticsearch** | elasticsearch:9.2.3 | `9200` | 日誌儲存 |
| **Logstash** | logstash:9.2.3 | `5044` | 日誌管道 |
| **Kibana** | kibana:9.2.3 | `5601` | 日誌查詢 UI |
| **Jaeger** | jaeger:1.62 | `16686` | 分散式鏈路追蹤 |

---

## 2. 快速開始

### 環境需求

| 元件 | 版本 | 用途 |
|------|------|------|
| Docker Desktop | 最新穩定版 | 容器化服務 |
| .NET SDK | 8.0+ | 後端與 UI 編譯 |
| Node.js | 18+ | E2E 測試 (Playwright) |
| Git | 最新版 | 版本控制 |

### 方式一：Docker Compose 全容器化啟動 (推薦)

```powershell
# 一鍵啟動所有服務
docker-compose up -d

# 或使用啟動腳本
.\start-all.bat
```

啟動後服務自動可用：
- 管理介面 → `http://localhost:5000`
- API 端點 → `http://localhost:5001`
- Swagger 文件 → `http://localhost:5001/swagger`

### 方式二：本機開發模式

```powershell
# 1. 啟動基礎設施 (APISIX, etcd, PostgreSQL, Prometheus, Grafana, Jaeger, ELK)
docker-compose up -d apisix etcd milk-db prometheus grafana elasticsearch logstash kibana jaeger

# 2. 執行資料庫遷移
cd backend/MilkApiManager
dotnet ef database update

# 3. 啟動後端 API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5001"

# 4. 啟動 Blazor 管理介面 (另一個終端)
cd backend/MilkAdminBlazor
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5000"
```

### 方式三：全系統驗證

```powershell
# Windows
.\scripts\verify-all.ps1

# Linux / macOS
./scripts/verify-all.sh
```

---

## 3. 認證與權限管理 (RBAC)

系統採用 **JWT + API Key** 雙重認證機制，並整合 LDAP/Active Directory。

### 3.1 認證方式

| 方式 | 用途 | 說明 |
|------|------|------|
| **JWT Token** | Web UI / API 呼叫 | 透過 `/api/auth/login` 登入取得，Token 內包含角色聲明 |
| **API Key** | 機器對機器 (M2M) | 透過 HTTP Header `X-API-KEY` 傳遞，適合 SDK / 自動化腳本 |
| **LDAP/AD** | 企業 SSO | 連接 Active Directory，將 AD Group 映射為系統角色 |

### 3.2 角色與權限矩陣

系統定義三級角色權限，嚴格控管操作範圍：

| 角色 | 權限等級 | 可執行操作 |
|------|----------|------------|
| **Admin** | 最高 | 所有操作（含 API Key 管理、黑名單 CRUD、帳號管理、Reconcile） |
| **Operator** | 中等 | 路由/服務/Upstream CRUD、PII 規則管理、白名單管理、Mock 設定 |
| **Viewer** | 唯讀 | 查看所有頁面數據，但不可建立、修改或刪除 |

### 3.3 登入方式

**Web UI 登入：**
```
http://localhost:5000 → 系統自動導向認證
```

**API 登入 (取得 JWT Token)：**
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'

# 回應範例
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-04-01T00:00:00Z",
  "displayName": "Admin User",
  "roles": ["Admin", "Operator", "Viewer"]
}
```

**API Key 方式：**
```bash
curl http://localhost:5001/api/route \
  -H "X-API-KEY: milk-admin-secret-key-change-me"
```

### 3.4 Demo 預設帳號 (TEST MODE)

| 帳號 | 密碼 | 角色 | 權限範圍 |
|------|------|------|----------|
| `admin` | `admin` | Admin, Operator, Viewer | 完整管理 |
| `operator` | `operator` | Operator, Viewer | 操作管理 |
| `viewer` | `viewer` | Viewer | 唯讀瀏覽 |

### 3.5 LDAP 群組映射

| AD Group | 系統角色 |
|----------|----------|
| `api-admins` | Admin |
| `api-operators` | Operator |
| `api-viewers` | Viewer |

---

## 4. Gateway Dashboard — 系統總覽

**路徑：** `/gateway`

Gateway Dashboard 是整個管理系統的儀表板首頁，以 Dark Trading Terminal 風格呈現 APISIX 控制平面即時狀態。

### 功能區塊

| 區塊 | 說明 | 視覺設計 |
|------|------|----------|
| **Stats Cards (6 張)** | Routes / Services / Upstreams / Consumers / SSL Certs / Global Rules 即時數量 | 彩色左邊框 (Blue/Green/Purple/Cyan/Yellow/Orange)，Monospace 大字體 |
| **APISIX Server Info** | 網關版本 (3.11.0)、Hostname、Boot Time | 表格佈局 |
| **Quick Navigation** | 一鍵跳轉至各管理頁面 | 圖標列表 |

### 操作

1. 訪問 `/gateway`，系統自動載入即時統計
2. 點擊右上角 **Refresh** 手動更新
3. 點擊 Quick Navigation 項目直接跳轉

### API 端點

```
GET /api/serverinfo/dashboard  → 聚合統計數據
GET /api/serverinfo            → APISIX Server 原始資訊
```

---

## 5. 網關核心管理 (GATEWAY)

### 5.1 Routes Management — 路由管理

**路徑：** `/routes-management`  
**API：** `GET/POST/PUT/DELETE /api/route`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

APISIX 路由的完整生命週期管理，包含 URI 比對、HTTP Method 過濾、Upstream 綁定與 Plugin 配置。

**操作流程：**

1. 點擊 **"Create Route"** 開啟新增對話框
2. 填寫：
   - **Name** — 路由名稱 (如 `order-api-v2`)
   - **URI** — 路徑比對規則 (如 `/api/v2/orders/*`)
   - **Methods** — 勾選 HTTP Method (GET / POST / PUT / DELETE / PATCH)
   - **Service ID** 或 **Inline Upstream** — 流量導向目標
   - **Plugins (JSON)** — 選用，以 JSON 格式配置插件
3. 點擊 **Save** → APISIX 即時生效

**功能特色：**
- **搜尋過濾** — 即時關鍵字篩選路由
- **Method 色彩標籤** — GET=綠、POST=藍、PUT=黃、DELETE=紅
- **JSON Viewer** — 點擊按鈕查看完整路由 JSON 配置
- **Audit Trail** — 所有操作自動記錄至 Audit Log

**Plugin JSON 範例：**
```json
{
  "prometheus": {},
  "request-id": {},
  "limit-count": {
    "count": 1000,
    "time_window": 60,
    "rejected_code": 429
  }
}
```

### 5.2 Services Management — 服務管理

**路徑：** `/services-management`  
**API：** `GET/PUT/DELETE /api/service`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

將多條路由歸納至共用的 Service 定義，統一管理 Upstream 與 Plugin。

**操作流程：**

1. 點擊 **"Create Service"**
2. 填入 Service Name、Description
3. 配置 Upstream Nodes (`host:port`) + Weight (權重)
4. 選擇負載均衡演算法：

| 演算法 | 說明 | 適用場景 |
|--------|------|----------|
| `roundrobin` | 加權輪詢 | 通用場景 |
| `chash` | 一致性 Hash | Session 親和性 |
| `ewma` | 指數加權平均 | 自適應負載 |
| `least_conn` | 最少連線 | 長連線服務 |

5. 儲存後，Route 可透過 Service ID 引用

### 5.3 Upstreams Management — 上游管理

**路徑：** `/upstreams-management`  
**API：** `GET/PUT/DELETE /api/upstream`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

獨立管理後端節點群組，支援多種負載均衡策略。

**操作流程：**

1. 點擊 **"Create Upstream"**
2. 配置：
   - **Name** — 上游名稱
   - **Type** — 負載均衡演算法 (roundrobin / chash / ewma / least_conn)
   - **Scheme** — 協定 (http / https / grpc)
   - **Nodes** — 一或多個節點 (`host:port` + `weight`)
   - **Retries** — 失敗重試次數
   - **Pass Host** — 上游 Host 傳遞策略 (pass / node / rewrite)

### 5.4 SSL Certificate Management — 憑證管理

**路徑：** `/ssl-management`  
**API：** `GET/PUT/DELETE /api/ssl`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

TLS/SSL 憑證管理，實現 HTTPS SNI 路由。

**操作流程：**

1. 點擊 **"Upload Certificate"**
2. 填入：
   - **SNIs** — 域名列表 (以逗號分隔，如 `api.example.com, *.example.com`)
   - **Certificate** — PEM 格式的憑證內容 (含中繼憑證)
   - **Private Key** — PEM 格式的私鑰
3. 儲存後 APISIX 即時啟用 SNI 匹配

**狀態顯示：**
- 🟢 **Enabled** — 憑證生效中
- 🔴 **Disabled** — 憑證已停用

**安全性：** SSL 列表 API 自動移除 cert/key 內容，避免敏感資訊洩漏。

### 5.5 Global Plugin Rules — 全域插件

**路徑：** `/global-plugins`  
**API：** `GET/PUT/DELETE /api/globalrule`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

定義套用到所有路由的全域插件規則。全域規則作用於所有經過 APISIX 的流量。

**操作流程：**

1. 點擊 **"Create Global Rule"**
2. 輸入 Rule ID (數字)
3. 以 JSON 格式配置 Plugins
4. 儲存後全域立即生效

**常用插件參考：**

| Plugin | 用途 | JSON 範例 |
|--------|------|-----------|
| `prometheus` | 匯出 Prometheus 指標 | `{"prometheus": {}}` |
| `request-id` | 為每個請求注入唯一 ID | `{"request-id": {"header_name": "X-Request-Id"}}` |
| `cors` | 跨域資源共享 | `{"cors": {"allow_origins": "*"}}` |
| `ip-restriction` | IP 存取控制 | `{"ip-restriction": {"blacklist": ["1.2.3.4"]}}` |
| `limit-req` | 請求速率限制 | `{"limit-req": {"rate": 10, "burst": 20}}` |
| `limit-count` | 固定時間窗口限流 | `{"limit-count": {"count": 600, "time_window": 60}}` |

**頁面內建 Plugin 參考清單：** 頁面右側提供常用 Plugin 列表，點擊可自動填入 JSON 範本。

---

## 6. API 治理與開發者體驗 (API / DEVELOPER)

### 6.1 API Inventory — API 資產清冊與治理

**路徑：** `/api-inventory`  
**API：** `GET/POST /api/apicatalog`  
**權限：** Viewer 可查看、Operator 以上可註冊

對齊保險業資安標準的 API 治理看板，列出所有 API 並標記風險等級。

**功能特色：**
- **風險分級** — L1 (高風險/紅色)、L2 (中風險/黃色)、L3 (低風險/綠色)
- **部門歸屬** — 追蹤 API 所屬團隊
- **稽核日期** — 最後稽核時間
- **匯出功能** — 一鍵匯出治理報告

**操作流程：**

1. 訪問 `/api-inventory` 查看完整 API 資產清冊
2. 使用搜尋框篩選特定 API
3. 查看風險等級 Badge (L1/L2/L3)
4. 點擊 **Export** 匯出合規報告

### 6.2 API List — 服務列表

**路徑：** `/apis`  
**API：** `GET/POST/PUT/DELETE /api/api`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

服務目錄清單，顯示所有內部 API 的端點 URI、安全層級與團隊負責人。

**功能特色：**
- 內嵌 **RouteWhitelistManager** 元件 — 點擊設定圖標可管理路由級別的 IP 白名單
- **分析按鈕** — 快速跳轉至該 API 的流量分析頁面

### 6.3 Developer Hub — 開發者自助門戶

**路徑：** `/dev-portal`  
**權限：** Viewer 以上

開發者的一站式自助平台，以三個 Tab 頁籤呈現：

| Tab | 功能 | 說明 |
|-----|------|------|
| **API Explorer** | Swagger 文件瀏覽 | 左側服務清單 + 右側 iframe 嵌入 Swagger UI |
| **Run Tests** | API 測試沙盒 | 選擇服務，執行預定義測試場景，即時顯示延遲與結果 |
| **Request Access** | 存取權限申請 | 填寫專案名、Email、選擇 Tier，系統自動通知管理員 |

### 6.4 Mock Lab — API 模擬實驗室

**路徑：** `/mock-lab`  
**API：** `GET/POST/PUT/DELETE /api/mock`  
**權限：** Viewer 可查看、Operator 以上可 CRUD

無需後端程式碼即可模擬 API 回應，適合前端獨立開發與整合測試。

**操作流程：**

1. 點擊 **"Create Mock Response"**
2. 填入：
   - **Route ID** — 要 Mock 的路由
   - **HTTP Status Code** — 回應狀態碼 (如 200、201、404)
   - **Content-Type** — 回應格式 (如 `application/json`)
   - **Response Body** — Mock 回應內容 (JSON)
3. 使用 **Toggle Switch** 啟用/停用 Mock
4. 變更即時同步至 APISIX

### 6.5 Access Request — 自助申請

**API：** `GET/POST /api/accessrequest`  
**權限：** Admin 審核

開發團隊可透過 Developer Hub 或 API 自助申請 API 存取權限。

**流程：**
1. 開發者在 `/dev-portal` → **Request Access** 提交申請
2. 系統記錄申請並透過 Webhook 通知管理員
3. 管理員登入後台審核：
   - **Approve** — 系統自動撥備 APISIX Consumer + API Key
   - **Reject** — 駁回並記錄原因

---

## 7. 安全防護與合規 (OPERATIONS)

### 7.1 PII Protection — 個資隱私防護

**路徑：** `/pii-management`  
**API：** `GET/POST/PUT/DELETE /api/piimasking`  
**權限：** Operator 以上

透過自研的 `pii-masker.lua` APISIX 插件，以 Regex 規則即時遮蔽 API 回應中的敏感資訊，**無需修改後端程式碼**。

**操作流程：**

1. 點擊 **"Add New Rule"**
2. 填入：
   - **Route ID** — 目標路由
   - **Field Path** — JSON 欄位路徑 (如 `email`, `phone`, `ssn`)
   - **Pattern (Regex)** — 比對規則 (如 `(.+)@(.+)` 遮蔽郵件)
   - **Mask With** — 替換字元 (預設 `***`)
3. 系統自動驗證 Regex 語法正確性
4. 儲存後即時生效

**使用範例：**

| 欄位 | Regex | 原始值 | 遮蔽後 |
|------|-------|--------|--------|
| `email` | `(.+)@(.+)` | `user@mail.com` | `***@***.com` |
| `phone` | `.*` | `0912-345-678` | `***` |
| `card_number` | `\d{12}` | `4111111111111234` | `***1234` |

### 7.2 IP Blacklist — 全域 IP 封鎖

**路徑：** `/blacklist`  
**API：** `GET/POST /api/blacklist`  
**權限：** Viewer 查看、Admin 管理

管理全域 IP 封鎖清單，被封鎖的 IP 所有請求均回傳 403 Forbidden。

**操作流程：**

1. 在輸入框填入 IP 位址或 CIDR (如 `192.168.1.0/24`)
2. (選用) 填入封鎖原因、到期日期
3. 點擊 **"Add to Blacklist"**
4. 移除封鎖：找到目標 IP，點擊 **Delete**

**自動封鎖：** MilkWorker 背景服務監控 Prometheus 指標，自動偵測高頻攻擊行為並封鎖惡意 IP。

**Drift 偵測：** `GET /api/syncstatus/blacklist-drift` 可比對 DB 與 APISIX 的黑名單差異。

### 7.3 IP Whitelist — 路由級 IP 白名單

**UI：** 在 API List (`/apis`) 頁面，點擊路由的設定圖標  
**API：** `GET/POST /api/whitelist/route/{routeId}`  
**權限：** Operator 以上

針對特定路由設定 IP 白名單，只允許特定 IP 存取，同步至 APISIX 的 `ip-restriction` 插件。

**操作流程：**

1. 在 `/apis` 找到目標 API，點擊 **⚙ 設定** 圖標
2. RouteWhitelistManager 元件開啟
3. 輸入 IP (CIDR 格式)、到期日期
4. 點擊 **Add**，系統即時同步至 APISIX

### 7.4 Consumers 管理

**路徑：** `/consumers`  
**API：** `GET/POST/DELETE /api/consumer`  
**權限：** Viewer 查看、Operator 以上 CRUD

管理 API 消費者（Client），包含角色、Scopes、配額設定。

**操作流程：**

1. 點擊 **"新增消費者"**
2. 填入：
   - **Username** — 消費者唯一識別
   - **Description** — 用途描述
   - **Roles** — admin / developer / viewer
   - **Scopes** — read / write / delete
   - **Labels** — 自定義標籤 (逗號分隔)
3. **配額管理** — 可設定每日請求上限 (Quota)
4. **Rate Limit** — 配置速率限制 (limit-count / limit-req 插件)

### 7.5 Consumer Groups — 流量分級

**路徑：** `/consumer-groups`  
**API：** `GET/PUT/DELETE /api/consumergroup`  
**權限：** Viewer 查看、Operator 以上 CRUD

建立消費者群組，以 Tier (Gold / Silver / Free) 統一管理配額與限速。

**操作流程：**

1. 點擊 **"Create Group"**
2. 填入群組名稱 (如 `tier-gold`)
3. 以 JSON 配置共用 Plugins：
   ```json
   {
     "limit-count": { "count": 100000, "time_window": 86400 },
     "limit-req": { "rate": 500, "burst": 100 }
   }
   ```
4. 在 Consumer 建立時指定所屬 Group

### 7.6 Audit Logs — 審計日誌

**路徑：** `/audit-logs`  
**API：** `GET /api/auditlogs` | `GET /api/auditlogs/stats` | `GET /api/auditlogs/export`  
**權限：** Operator 以上

記錄所有管理操作的完整審計軌跡，支援合規查詢與報表匯出。

**功能區塊：**

| 區塊 | 說明 |
|------|------|
| **KPI Cards** | 總事件數、違規次數、最活躍使用者 |
| **Activity Log** | 可篩選的操作記錄表格 |
| **CSV Export** | 匯出最近 1000 筆記錄 |

**操作流程：**

1. 訪問 `/audit-logs` 查看最近操作紀錄
2. 使用搜尋框或篩選器定位特定記錄
3. 點擊 **"Export CSV Report"** 下載合規報表

**Audit 涵蓋範圍：** Route/Service/Upstream/SSL CRUD、黑白名單變更、PII 規則異動、Consumer 管理等 14 類操作。

### 7.7 Alert Rules — 告警規則

**路徑：** `/alert-rules`  
**API：** `GET/POST/DELETE/PUT /api/alertrules`  
**權限：** Viewer 查看、Operator 以上管理

配置基於 Prometheus 指標的自動告警規則。

**操作流程：**

1. 填入規則名稱
2. 選擇指標類型：
   - **5xx Error Spike** — HTTP 5xx 錯誤突增
   - **High Frequency IP** — 高頻 IP 偵測
3. 設定 **Threshold** (閾值) 與 **Duration** (監控窗口)
4. 勾選通知管道：
   - ✅ Mattermost (Webhook)
   - ✅ Email
5. 點擊 **"Add Rule"**
6. 使用 **Toggle** 切換規則啟用/停用

### 7.8 Reports — 統計報表

**路徑：** `/reports`  
**API：** `GET /api/analytics/*`  
**權限：** Viewer 以上

消費者使用量統計報表，整合 Prometheus 指標數據。

**功能：**
- **Consumer Filter** — 依消費者篩選
- **24h 統計** — 請求數、錯誤率 (色彩標記異常)
- **CSV Export** — 匯出報表資料
- **Grafana 連結** — 一鍵跳轉 Grafana Dashboard

---

## 8. 監控與可觀測性

### 8.1 Traffic Intelligence Center — 流量智慧中心

**路徑：** `/consumer-analytics`  
**API：** `GET /api/analytics/*`  
**權限：** Viewer 以上

即時流量分析儀表板，整合延遲監控、吞吐量趨勢與 SLA 追蹤。

**功能區塊：**

| 區塊 | 說明 | 資料來源 |
|------|------|----------|
| **Latency Trend** | P95 延遲折線圖 | Prometheus `apisix_http_latency` |
| **Throughput Chart** | 每秒請求量 (RPS) 柱狀圖 | Prometheus `apisix_http_status` |
| **Error Rate** | 非 2xx/3xx 回應百分比 | Prometheus 計算 |
| **SLA Badge** | 24h 可用性百分比 (Gold/Silver/Critical) | Prometheus 計算 |
| **Top 5 Bottlenecks** | 延遲最高的 5 條路由 | Prometheus Histogram |

**操作：**
- **Consumer Filter** — 下拉選擇或搜尋特定消費者
- **Route Filter** — 輸入路由關鍵字篩選
- **Date Range Picker** — 選擇分析時段
- **Auto-Refresh (15s)** — 自動更新開關

### 8.2 Stress Test Center — 壓力測試

**路徑：** `/load-testing`  
**API：** `POST /api/loadtest/run`  
**權限：** Operator 以上

內建 k6 壓測引擎，可直接在管理介面發起效能測試。

**操作流程：**

1. 填入 **Target URL** (如 `http://apisix:9080/api/v1/health`)
2. 拖曳 **Virtual Users** 滑桿 (1–100 VUs)
3. 拖曳 **Duration** 滑桿 (10–300 秒)
4. 點擊 **"Start Stress Test"**
5. **Live Execution Console** 即時串流 k6 輸出
6. 報告包含：平均延遲、P95、RPS、錯誤率

### 8.3 Sync Status — 系統同步狀態

**路徑：** `/sync-status`  
**API：** `GET /api/syncstatus` | `GET /api/syncstatus/blacklist-drift` | `POST /api/syncstatus/reconcile-blacklist`  
**權限：** Viewer 查看、Admin 可 Reconcile

監控 AD 群組同步與資料庫-Gateway 一致性。

**狀態指示：**
- 🟢 **Success** — 同步健康
- 🟡 **Syncing** — 同步進行中
- 🔴 **Failed** — 同步異常
- ⚪ **Idle** — 尚未執行

**Blacklist Drift 偵測：** 比較 PostgreSQL 與 APISIX traffic-blocker 的黑名單差異，Admin 可一鍵 Reconcile 修復。

### 8.4 外部監控服務

| 服務 | 位址 | 用途 | 預設帳密 |
|------|------|------|----------|
| **Grafana** | `http://localhost:3000` | 流量與阻斷數據可視化 | admin / admin |
| **Prometheus** | `http://localhost:9090` | 指標收集與 PromQL 查詢 | — |
| **Jaeger** | `http://localhost:16686` | 分散式鏈路追蹤 | — |
| **Kibana** | `http://localhost:5601` | ELK 日誌深度查詢 | — |
| **APISIX Dashboard** | `http://localhost:9000` | 原生 Dashboard (備用) | 見 conf.yaml |

### 8.5 SLO Metrics (Prometheus Exporter)

系統暴露 SLO 指標供 Prometheus 抓取：

```
GET /metrics/slo

# 回傳 Prometheus text format:
milk_success_rate_percent 99.85
milk_sync_latency_p95_seconds 0.342
milk_blacklist_drift_count 0
```

---

## 9. 進階功能 (REST API)

以下功能透過 REST API 提供，適合進階自動化與 CI/CD 整合。

### 9.1 Canary Release — 灰度發布

**API：** `/api/canaryrelease`  
**權限：** Viewer 查看、Operator 以上操作

支援灰度發布管理，按權重將流量分配至 Stable 與 Canary 版本。

| 操作 | API | 說明 |
|------|-----|------|
| 建立灰度 | `POST /api/canaryrelease` | StableWeight + CanaryWeight = 100 |
| 調整權重 | `PUT /api/canaryrelease/{id}` | 動態調整流量比例 |
| Rollback | `POST /api/canaryrelease/{id}/rollback` | 回復至 Stable (100/0) |
| Promote | `POST /api/canaryrelease/{id}/promote` | 全量切換至 Canary (0/100) |

**使用範例：**
```bash
# 建立灰度：90% Stable / 10% Canary
curl -X POST http://localhost:5001/api/canaryrelease \
  -H "X-API-KEY: milk-admin-secret-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "order-api",
    "stableUpstreamId": "order-v1",
    "canaryUpstreamId": "order-v2",
    "stableWeight": 90,
    "canaryWeight": 10
  }'

# 觀察穩定後全量推進
curl -X POST http://localhost:5001/api/canaryrelease/1/promote \
  -H "X-API-KEY: milk-admin-secret-key-change-me"
```

### 9.2 Circuit Breaker — 熔斷器

**API：** `/api/circuitbreaker`  
**權限：** Viewer 查看、Operator 建立/修改、Admin 刪除

為路由配置熔斷策略，當後端錯誤率超過閾值時自動斷路保護。

| 操作 | API |
|------|-----|
| 查看所有 | `GET /api/circuitbreaker` |
| 查看路由 | `GET /api/circuitbreaker/{routeId}` |
| 建立 | `POST /api/circuitbreaker` |
| 更新 | `PUT /api/circuitbreaker/{routeId}` |
| 刪除 | `DELETE /api/circuitbreaker/{routeId}` |

**配置參數：** 錯誤閾值 (%)、熔斷持續時間 (秒)、半開狀態探測次數。

### 9.3 Cache Policy — 快取策略

**API：** `/api/cachepolicy`  
**權限：** Viewer 查看、Operator 建立/修改、Admin 刪除

為路由配置回應快取策略，減少後端負載。

| 操作 | API |
|------|-----|
| 查看所有 | `GET /api/cachepolicy` |
| 建立 | `POST /api/cachepolicy` |
| 更新 | `PUT /api/cachepolicy/{routeId}` |
| 刪除 | `DELETE /api/cachepolicy/{routeId}` |

### 9.4 Health Check — 健康檢查

**API：** `/api/healthcheck`  
**權限：** Viewer 查看、Operator 建立/修改、Admin 刪除

為 Upstream 配置主動 (Active) 與被動 (Passive) 健康檢查。

**配置參數範例：**
```json
{
  "upstreamId": "order-service",
  "type": "active",
  "httpPath": "/health",
  "interval": 10,
  "timeout": 5,
  "healthyThreshold": 3,
  "unhealthyThreshold": 3
}
```

### 9.5 Transform Rules — 請求/回應轉換

**API：** `/api/transform`  
**權限：** Viewer 查看、Operator 建立/修改、Admin 刪除

配置請求或回應的 Header/Body 轉換規則。

| 欄位 | 說明 |
|------|------|
| `routeId` | 目標路由 |
| `phase` | `request` 或 `response` |
| `operation` | `add` / `remove` / `replace` |
| `target` | `header` / `body` |
| `key` | 目標 Header 名或 Body 欄位 |
| `value` | 欲設定的值 |
| `priority` | 執行優先序 |

### 9.6 API Lifecycle — 生命週期管理

**API：** `/api/apilifecycle`  
**權限：** Viewer 查看、Operator 操作

追蹤 API 從規劃到退役的完整生命週期。

**狀態流程：** `Planning` → `Active` → `Deprecated` → `Retired`

| 操作 | API |
|------|-----|
| 查看歷史 | `GET /api/apilifecycle/api/{apiIdentifier}` |
| 列出已棄用 | `GET /api/apilifecycle/deprecated` |
| 標記棄用 | `POST /api/apilifecycle/{id}/deprecate` |

### 9.7 Test Execution — 測試執行

**API：** `/api/testexecution`  
**權限：** Viewer 查看、Operator 操作

管理與執行 API 測試場景，整合於 Developer Hub 的 "Run Tests" 功能。

| 操作 | API |
|------|-----|
| 查看場景 | `GET /api/testexecution/scenarios/{serviceId}` |
| 建立場景 | `POST /api/testexecution/scenarios` |
| 執行測試 | `POST /api/testexecution/run/{id}` |

---

## 10. REST API 完整參考

### 認證

所有 `/api/*` 端點需要下列任一認證方式：

| 方式 | Header | 範例 |
|------|--------|------|
| API Key | `X-API-KEY` | `X-API-KEY: milk-admin-secret-key-change-me` |
| JWT | `Authorization` | `Authorization: Bearer eyJhbGci...` |

### 公開端點 (不需認證)

| 端點 | 說明 |
|------|------|
| `POST /api/auth/login` | 登入取得 JWT Token (有 Rate Limiting) |
| `GET /health` | 系統健康檢查 |
| `GET /health/ready` | Readiness 探針 |
| `GET /metrics/slo` | SLO 指標 (Prometheus 格式) |

### 完整端點列表

系統共有 **30 個 Controller**，完整 API 文件請存取：
```
http://localhost:5001/swagger
```

---

## 11. SDK 整合指南

### C# SDK

```csharp
// 安裝：使用生成的 MilkApi.Client.cs
var client = new MilkApiClient("http://localhost:5001", "milk-admin-secret-key-change-me");

// 取得所有路由
var routes = await client.GetRoutesAsync();

// 新增黑名單
await client.AddToBlacklistAsync("192.168.1.100");
```

### Python SDK

```python
# 安裝：pip install milk-api-client (或使用 sdk/python/)
from milk_api import MilkApiClient

client = MilkApiClient("http://localhost:5001", api_key="milk-admin-secret-key-change-me")

# 取得所有路由
routes = client.get_routes()

# 新增消費者
client.create_consumer(username="new-partner", plugins={...})
```

### SDK 自動生成

```powershell
# C# SDK
.\scripts\generate-sdk.ps1

# Python SDK
.\scripts\generate-python-sdk.ps1
```

---

## 12. 疑難排解

| 症狀 | 檢查方向 | 修復建議 |
|------|----------|----------|
| **Blazor UI 無法載入** | Port 5000 佔用 | `netstat -an \| findstr 5000`，終止衝突程序 |
| **API 回傳 401 Unauthorized** | API Key / JWT 錯誤 | 確認 `X-API-KEY` Header 或重新登入取得 Token |
| **API 回傳 403 Forbidden** | 角色權限不足 | 確認使用者角色符合操作要求 |
| **API 回傳 404** | 路由未同步 | 檢查 Route Sync 日誌，確認路由已下發至 APISIX |
| **Gateway Dashboard 數據為 0** | APISIX Admin API 不可達 | `curl http://localhost:9180/apisix/admin/routes` 測試連線 |
| **Routes/Services 建立失敗** | etcd 異常 | `docker-compose logs etcd` 檢查狀態 |
| **SSL 上傳失敗** | PEM 格式錯誤 | 確認 Certificate 和 Key 配對正確 |
| **PII 遮蔽不生效** | Plugin 未載入 | 確認 `pii-masker.lua` 已掛載至 APISIX 容器 |
| **Kibana 無數據** | Logstash 異常 | `docker-compose logs logstash` 檢查 pipeline |
| **Swagger 無法顯示** | 環境配置 | 設定 `ASPNETCORE_ENVIRONMENT=Development` |
| **壓測無輸出** | k6 未安裝 | 安裝 k6：`choco install k6` (Windows) |
| **黑名單 Drift** | DB 與 APISIX 不一致 | 執行 `POST /api/syncstatus/reconcile-blacklist` |
| **通知收不到** | Webhook URL 錯誤 | 檢查 `appsettings.json` Webhook 配置 |

---

## 附錄 A — 頁面路由總覽

| # | 頁面 | 路由 | 分類 | 說明 |
|---|------|------|------|------|
| 1 | Gateway Dashboard | `/gateway` | GATEWAY | 六指標概覽 + Server Info |
| 2 | Routes Management | `/routes-management` | GATEWAY | 路由 CRUD + Plugin JSON |
| 3 | Services Management | `/services-management` | GATEWAY | 服務定義 + Upstream |
| 4 | Upstreams Management | `/upstreams-management` | GATEWAY | 負載均衡 + 節點管理 |
| 5 | SSL Certificates | `/ssl-management` | GATEWAY | TLS 憑證上傳 + SNI |
| 6 | Global Plugins | `/global-plugins` | GATEWAY | 全域插件規則 |
| 7 | API Inventory | `/api-inventory` | API | 合規盤點 + 風險分級 |
| 8 | API List | `/apis` | API | 服務清單 + 白名單管理 |
| 9 | IP Blacklist | `/blacklist` | API | IP 封鎖管理 |
| 10 | Consumers | `/consumers` | API | 消費者 + 配額管理 |
| 11 | Traffic Tiers | `/consumer-groups` | API | 流量分級 + 限速 |
| 12 | Developer Portal | `/dev-portal` | DEVELOPER | 開發者自助門戶 |
| 13 | Mock Lab | `/mock-lab` | DEVELOPER | 模擬回應定義 |
| 14 | Traffic Intelligence | `/consumer-analytics` | OPERATIONS | 流量分析 + SLA |
| 15 | PII Protection | `/pii-management` | OPERATIONS | 動態脫敏規則 |
| 16 | Stress Test | `/load-testing` | OPERATIONS | 壓力測試控制台 |
| 17 | Audit Logs | `/audit-logs` | OPERATIONS | 稽核日誌 + CSV |
| 18 | Alert Rules | `/alert-rules` | OPERATIONS | 告警規則配置 |
| 19 | Reports | `/reports` | OPERATIONS | 統計報表 + CSV |
| 20 | Sync Status | `/sync-status` | SYSTEM | 系統同步狀態 |

---

## 附錄 B — REST API 端點速查表

### Gateway 管理
| 資源 | GET | POST | PUT | DELETE |
|------|-----|------|-----|--------|
| `/api/route` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | ✅ `/{id}` 刪除 |
| `/api/service` | ✅ 列表 | — | ✅ `/{id}` 建立/更新 | ✅ `/{id}` 刪除 |
| `/api/upstream` | ✅ 列表 | — | ✅ `/{id}` 建立/更新 | ✅ `/{id}` 刪除 |
| `/api/ssl` | ✅ 列表 | — | ✅ `/{id}` 建立/更新 | ✅ `/{id}` 刪除 |
| `/api/globalrule` | ✅ 列表 | — | ✅ `/{id}` 建立/更新 | ✅ `/{id}` 刪除 |

### API 管理
| 資源 | GET | POST | PUT | DELETE |
|------|-----|------|-----|--------|
| `/api/consumer` | ✅ 列表 | ✅ 建立/更新 | — | ✅ `/{username}` 刪除 |
| `/api/consumergroup` | ✅ 列表 | — | ✅ `/{id}` 建立/更新 | ✅ `/{id}` 刪除 |
| `/api/blacklist` | ✅ 列表 | ✅ 新增/移除 | — | — |
| `/api/whitelist/route/{routeId}` | ✅ 查看 | ✅ 新增/移除 | — | — |
| `/api/piimasking` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | ✅ `/{id}` 刪除 |

### 進階功能
| 資源 | GET | POST | PUT | DELETE |
|------|-----|------|-----|--------|
| `/api/canaryrelease` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | — |
| `/api/circuitbreaker` | ✅ 列表 | ✅ 建立 | ✅ `/{routeId}` 更新 | ✅ `/{routeId}` 刪除 |
| `/api/cachepolicy` | ✅ 列表 | ✅ 建立 | ✅ `/{routeId}` 更新 | ✅ `/{routeId}` 刪除 |
| `/api/healthcheck` | ✅ 列表 | ✅ 建立 | ✅ `/{upstreamId}` 更新 | ✅ `/{upstreamId}` 刪除 |
| `/api/transform` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | ✅ `/{id}` 刪除 |
| `/api/apilifecycle` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | — |
| `/api/mock` | ✅ 列表 | ✅ 建立 | ✅ `/{id}` 更新 | ✅ `/{id}` 刪除 |

### 監控與認證
| 資源 | 方法 | 說明 |
|------|------|------|
| `/api/auth/login` | POST | 登入 (AllowAnonymous) |
| `/api/auth/me` | GET | 取得當前使用者資訊 |
| `/api/analytics/requests` | GET | 請求數指標 |
| `/api/analytics/latency` | GET | P95 延遲指標 |
| `/api/analytics/errors` | GET | 錯誤率 |
| `/api/analytics/top-slow-routes` | GET | 最慢路由 Top N |
| `/api/analytics/sla` | GET | SLA 可用性 |
| `/api/auditlogs` | GET | 審計日誌 |
| `/api/auditlogs/export` | GET | CSV 匯出 |
| `/api/serverinfo/dashboard` | GET | Dashboard 聚合統計 |
| `/api/loadtest/run` | POST | 執行壓力測試 |
| `/api/keys` | GET/POST | API Key 管理 |
| `/metrics/slo` | GET | SLO 指標 (Prometheus) |

---

## 附錄 C — 環境變數參考

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `POSTGRES_USER` | `milk_user` | PostgreSQL 使用者 |
| `POSTGRES_PASSWORD` | `milk_password` | PostgreSQL 密碼 |
| `POSTGRES_DB` | `milk_db` | 資料庫名稱 |
| `APISIX_ADMIN_KEY` | `edd1c9f034335f136f87ad84b625c88b` | APISIX Admin API Key |
| `API_AUTH_KEY` | `milk-admin-secret-key-change-me` | 後端 API 認證 Key |
| `JWT_SECRET` | `milk-api-default-jwt-secret-...` | JWT 簽章密鑰 |
| `USE_DEMO_AUTH` | `true` | 啟用 Demo 帳號模式 |
| `GF_SECURITY_ADMIN_PASSWORD` | `admin` | Grafana 管理員密碼 |
| `ES_SECURITY_ENABLED` | `false` | Elasticsearch 安全模式 |
| `BackendApiUrl` | `http://milk-backend:8080` | Blazor → Backend 內部通訊 |

> ⚠️ **安全提醒：** 正式環境務必透過 `.env` 檔案覆蓋所有預設值，切勿使用預設密碼上線。

---

*欲了解更多架構細節，請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)。*  
*模擬操作情境，請參考 [SCENARIOS.md](./SCENARIOS.md)。*  
*開發者導覽，請參考 [ONBOARDING.md](../ONBOARDING.md)。*
