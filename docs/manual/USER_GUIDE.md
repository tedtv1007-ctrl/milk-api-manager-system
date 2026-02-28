# 📖 Milk API Manager — 使用操作手冊 v2.0

> **Blazor Server + MudBlazor + Apache APISIX** — 企業級 API 管理平台完整操作指南。  
> UI 風格：Dark Trading Terminal Theme | 存取入口：`http://localhost:5002`

---

## 目錄

1. [快速開始](#1--快速開始)
2. [系統總覽 — Gateway Dashboard](#2--系統總覽--gateway-dashboard)
3. [網關核心管理 (GATEWAY)](#3--網關核心管理-gateway)
4. [API 治理與開發者體驗 (API / DEVELOPER)](#4--api-治理與開發者體驗-api--developer)
5. [安全與合規 (OPERATIONS)](#5--安全與合規-operations)
6. [監控與觀測](#6--監控與觀測)
7. [疑難排解](#7--疑難排解)
8. [附錄 — 頁面路由總覽](#附錄--頁面路由總覽)

---

## 1. 🚀 快速開始

### 環境需求

| 元件 | 版本 |
|---|---|
| Docker Desktop | 最新穩定版 |
| .NET SDK | 8.0+ |
| Node.js (E2E 測試) | 18+ |
| Git | 最新版 |

### 啟動步驟

```powershell
# 1. 啟動基礎設施 (APISIX, etcd, Prometheus, Grafana, Jaeger, ELK)
docker-compose up -d

# 2. 啟動後端 API
cd backend/MilkApiManager
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5001"

# 3. 啟動 Blazor 管理介面
cd ../MilkAdminBlazor
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5002"
```

啟動後開啟瀏覽器訪問 **`http://localhost:5002`**，即可看到 Dark Trading Terminal 風格的管理儀表板。

### 服務端口一覽

| 服務 | 端口 | 用途 |
|---|---|---|
| **Blazor Admin UI** | `5002` | 主控管理介面 (本系統) |
| **MilkApiManager API** | `5001` | 後端 REST API |
| **APISIX Gateway** | `9080` | Data Plane (流量入口) |
| **APISIX Admin API** | `9180` | Control Plane (Admin API) |
| **APISIX Dashboard** | `9000` | 原生 Dashboard (備用) |
| **Prometheus** | `9090` | 指標收集 |
| **Grafana** | `3000` | 可視化儀表板 |
| **Jaeger** | `16686` | 分散式鏈路追蹤 |
| **Kibana** | `5601` | 日誌查詢 |
| **Elasticsearch** | `9200` | 搜尋引擎 |

---

## 2. ⚡ 系統總覽 — Gateway Dashboard

**路徑:** `/gateway`

Gateway Dashboard 是系統的入口頁面，提供 APISIX 控制平面的即時概覽。

### 功能區塊

| 區塊 | 說明 |
|---|---|
| **Stats Cards** | 六張統計卡片：Routes / Services / Upstreams / Consumers / SSL Certs / Global Rules 即時數量 |
| **APISIX Server Info** | 顯示網關版本、Hostname、啟動時間等系統資訊 |
| **Quick Navigation** | 一鍵跳轉至各管理頁面的快速入口 |

### 操作

1. 進入 `/gateway` 頁面，系統自動載入統計數據
2. 點擊 **Refresh** 按鈕手動重新抓取
3. 點擊 Quick Navigation 的卡片直接跳轉至對應管理頁面

---

## 3. ⚙️ 網關核心管理 (GATEWAY)

### 3.1 Routes Management — 路由管理

**路徑:** `/routes-management`

管理所有 APISIX 路由的 CRUD 操作，包含 URI、HTTP Methods、Upstream 綁定與 Plugin 配置。

**操作流程:**

1. 點擊 **"Create Route"** 新增路由
2. 填寫 Name、URI (如 `/api/v1/orders/*`)、選擇 HTTP Methods (GET, POST, PUT, DELETE)
3. 配置 Upstream 目標或綁定 Service ID
4. (選用) 以 JSON 格式加入 Plugin 配置
5. 點擊 **Save** 即時下發至 APISIX

**功能特色:**
- 搜尋過濾 (Filter) — 即時篩選路由
- Method 色彩標籤 — GET=綠, POST=藍, PUT=黃, DELETE=紅
- 點擊 JSON 按鈕查看完整路由配置
- 支援 Inline Upstream 或參照 Service/Upstream ID

### 3.2 Services Management — 服務管理

**路徑:** `/services-management`

將多條路由歸納至共用的 Service 定義，統一管理 Upstream 與 Plugin。

**操作流程:**

1. 點擊 **"Create Service"** 新增服務
2. 填入 Service Name、Description
3. 配置 Upstream Nodes (如 `host:port`) 與 Weight (權重)
4. 選擇負載均衡演算法 (roundrobin / chash / ewma / least_conn)
5. 儲存後，在 Route 中可透過 Service ID 引用

### 3.3 Upstreams Management — 上游管理

**路徑:** `/upstreams-management`

獨立管理後端節點群組，支援多種負載均衡策略與健康檢查。

**操作流程:**

1. 點擊 **"Create Upstream"** 新增
2. 填入 Name、選擇 Type (roundrobin / chash / ewma / least_conn)
3. 選擇 Scheme (http / https / grpc)
4. 新增一或多個 Node (`host:port` + `weight`)
5. (選用) 設定 Retries 數量與 Timeout

### 3.4 SSL Certificate Management — 憑證管理

**路徑:** `/ssl-management`

上傳與管理 TLS/SSL 憑證，實現 HTTPS SNI 路由。

**操作流程:**

1. 點擊 **"Upload Certificate"**
2. 填入 SNI 域名 (Domain)，以逗號分隔多個域名
3. 貼上 PEM 格式的 Certificate 與 Private Key
4. 儲存後 APISIX 即時啟用 SNI 匹配

**狀態顯示:**
- 🟢 **Enabled** — 憑證生效中
- 🔴 **Disabled** — 憑證已停用

### 3.5 Global Plugin Rules — 全域插件

**路徑:** `/global-plugins`

定義套用到所有路由的全域插件規則（如 prometheus、logging、cors）。

**操作流程:**

1. 點擊 **"Create Global Rule"**
2. 輸入 Rule ID
3. 以 JSON 格式配置 Plugins (如 `{"prometheus": {}, "request-id": {}}`)
4. 儲存後，所有經過 APISIX 的請求皆會套用此規則

**常用插件參考:**

| Plugin | 用途 |
|---|---|
| `prometheus` | 匯出 Prometheus 指標 |
| `request-id` | 為每個請求注入唯一 ID |
| `cors` | 跨域資源共享 |
| `ip-restriction` | IP 存取控制 |
| `limit-req` | 請求速率限制 |
| `limit-count` | 固定時間窗口限流 |

---

## 4. 🔌 API 治理與開發者體驗 (API / DEVELOPER)

### 4.1 API Inventory — API 資產清冊

**路徑:** `/api-inventory`

綜觀所有已註冊的 API，包含風險等級 (L1–L3)、狀態與 Owner 資訊。

### 4.2 Sync Status — 同步狀態

**路徑:** `/sync-status`

監控 OpenAPI 規格至 APISIX 的自動同步情況，查看最後同步時間與結果。

### 4.3 Developer Hub — 開發者入口

**路徑:** `/dev-portal`

開發者的一站式自助平台，包含三個標籤頁：

| Tab | 功能 |
|---|---|
| **API Explorer** | 左側列表瀏覽所有服務，右側嵌入 Swagger UI 查看文件 |
| **Run Tests** | 針對選定服務執行自動化驗證場景，即時回報延遲與狀態 |
| **Request Access** | 填寫專案名稱、信箱、選擇 Tier (Gold/Silver/Free) 提交存取申請 |

### 4.4 Mock Lab — API 模擬實驗室

**路徑:** `/mock-lab`

無需撰寫後端程式碼即可模擬 API 回應，適合前端開發與整合測試。

**操作流程:**

1. 點擊 **"Create Mock Response"**
2. 輸入 Route ID、HTTP Status Code、Content-Type
3. 填入 Response Body (JSON)
4. 使用 **Toggle Switch** 啟用/停用 Mock

### 4.5 API List — 路由清單

**路徑:** `/api-list`

以精簡表格呈現所有路由的 Path、Methods、風險等級 (L1–L3)。

---

## 5. 🛡️ 安全與合規 (OPERATIONS)

### 5.1 PII Protection — 個資隱私防護

**路徑:** `/pii-management`

管理 API 回應中的個資遮蔽規則（PII Masking），符合 GDPR/個資法要求。

**操作流程:**

1. 點擊 **"Add New Rule"**
2. 輸入 Route ID、欄位路徑 (如 `email`, `phone`)
3. 設定 Regex 規則 (預設 `.*` 全欄位遮蔽)
4. 儲存後即時生效，回應中的敏感欄位顯示為 `***`

**狀態標籤:**
- `active` — 規則生效中
- `disabled` — 規則已停用

### 5.2 IP Blacklist — IP 封鎖清單

**路徑:** `/blacklist`

管理被封鎖的 IP，支援自動偵測異常流量與手動新增。

**操作流程:**

1. 在輸入框填入 IP 位址 (如 `192.168.1.100`)
2. 點擊 **"Add to Blacklist"**
3. 該 IP 的後續請求將被回傳 **403 Forbidden**
4. 確認為誤判時，可直接刪除移除封鎖

### 5.3 Consumers — API 消費者管理

**路徑:** `/consumers`

管理 API 消費者（使用者）、角色分配與權限範圍 (Scopes)。

**操作流程:**

1. 點擊 **"新增消費者"**
2. 輸入作名稱、描述
3. 選擇角色 (admin / developer / viewer) 與 Scopes (read / write / delete)
4. 可添加自定義 Labels（以逗號分隔）
5. 儲存後消費者即可透過 API Key 存取

### 5.4 Consumer Groups — 消費者群組

**路徑:** `/consumer-groups`

將多個消費者歸納至群組，統一管理共用插件配置。

### 5.5 Audit Logs — 審計日誌

**路徑:** `/audit-logs`

記錄所有管理操作，支援篩選查詢與 CSV 匯出。

**功能:**
- 查看誰在什麼時間做了什麼操作
- 按時間範圍、操作類型篩選
- 點擊 **"Export CSV Report"** 下載合規報表

### 5.6 Alert Rules — 告警設定

**路徑:** `/alert-rules`

配置 Prometheus 指標告警規則，自動偵測異常並推播通知。

**操作流程:**

1. 填入 Rule Name、選擇 Metric (5xx Error Spike / High Frequency IP)
2. 設定 Threshold 與 Duration (如 `1m`)
3. 勾選通知管道 (Mattermost / Email)
4. 點擊 **"Add Rule"**

### 5.7 Reports — 合規報表

**路徑:** `/reports`

產出系統合規性報告，涵蓋安全、效能、SLA 等面向。

---

## 6. 📈 監控與觀測

### 6.1 Traffic Intelligence Center — 流量智慧中心

**路徑:** `/consumer-analytics`

即時流量分析、延遲監控與 SLA 追蹤。

**功能:**
- **Consumer Filter** — 按使用者篩選流量
- **Route Filter** — 按路由篩選
- **Time Horizon** — 日期範圍選擇
- **Auto-Refresh (15s)** — 自動更新數據
- **SLA 指標** — 即時顯示可用性百分比
- **Performance Bottlenecks** — 自動標記延遲異常的 API

### 6.2 Stress Test Center — 壓力測試

**路徑:** `/load-testing`

使用 k6 引擎對 API 執行即時效能測試。

**操作流程:**

1. 填入 Target URL (如 `http://apisix:9080/api/v1/health`)
2. 拖曳設定 Virtual Users (1–100) 與 Duration (10–300s)
3. 點擊 **"Start Stress Test"**
4. 在右側的 **Live Execution Console** 即時查看結果

### 6.3 外部監控服務

| 服務 | 位址 | 用途 |
|---|---|---|
| **Grafana** | `http://localhost:3000` | 流量與阻斷數據可視化 |
| **Prometheus** | `http://localhost:9090` | 指標收集與查詢 |
| **Jaeger** | `http://localhost:16686` | 分散式鏈路追蹤 |
| **Kibana** | `http://localhost:5601` | 日誌深度查詢 |
| **APISIX Dashboard** | `http://localhost:9000` | 原生 Dashboard (備用) |

---

## 7. 🛠️ 疑難排解

| 症狀 | 檢查方向 |
|---|---|
| **Blazor UI 無法載入** | 確認 `dotnet run` 正常啟動，檢查 5002 Port 是否被佔用 |
| **API 回傳 404** | 檢查 Route Sync Service 日誌，確認路由已下發至 APISIX |
| **Gateway Dashboard 資料為 0** | 確認 APISIX Admin API (`9180`) 可連線，檢查 Admin Key 配置 |
| **Routes/Services 建立失敗** | 檢查 etcd 容器狀態 (`docker-compose logs etcd`)，確認可連線 |
| **SSL 上傳失敗** | 確認 PEM 格式正確，Certificate 和 Key 必須配對 |
| **PII 規則不生效** | 確認 `pii-masker.lua` 插件已載入 APISIX，檢查 Plugin 配置 |
| **Kibana 無數據** | 確認 Logstash 容器正常運行 (Port 8080/8081) |
| **Swagger 無法顯示** | 設定 `ASPNETCORE_ENVIRONMENT=Development` 啟用 Swagger |
| **通知收不到** | 在 `appsettings.json` 或資料庫中確認 Webhook URL 配置 |
| **壓測無輸出** | 確認 k6 已安裝，Target URL 可存取 |

---

## 附錄 — 頁面路由總覽

| # | 頁面 | 路由 | 分類 |
|---|---|---|---|
| 1 | Gateway Dashboard | `/gateway` | GATEWAY |
| 2 | Routes Management | `/routes-management` | GATEWAY |
| 3 | Services Management | `/services-management` | GATEWAY |
| 4 | Upstreams Management | `/upstreams-management` | GATEWAY |
| 5 | SSL Certificates | `/ssl-management` | GATEWAY |
| 6 | Global Plugins | `/global-plugins` | GATEWAY |
| 7 | API Inventory | `/api-inventory` | API |
| 8 | API List | `/api-list` | API |
| 9 | Sync Status | `/sync-status` | API |
| 10 | PII Protection | `/pii-management` | API |
| 11 | Developer Hub | `/dev-portal` | DEVELOPER |
| 12 | Mock Lab | `/mock-lab` | DEVELOPER |
| 13 | Consumers | `/consumers` | OPERATIONS |
| 14 | Consumer Groups | `/consumer-groups` | OPERATIONS |
| 15 | Consumer Analytics | `/consumer-analytics` | OPERATIONS |
| 16 | IP Blacklist | `/blacklist` | OPERATIONS |
| 17 | Alert Rules | `/alert-rules` | OPERATIONS |
| 18 | Audit Logs | `/audit-logs` | OPERATIONS |
| 19 | Reports | `/reports` | OPERATIONS |
| 20 | Load Testing | `/load-testing` | OPERATIONS |

---

*欲了解更多技術細節，請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)。*  
*模擬操作情境，請參考 [SCENARIOS.md](./SCENARIOS.md)。*
