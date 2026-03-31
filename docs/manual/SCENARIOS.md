# 🎬 Milk API Manager — 使用情境操作手冊 v3.0

> 本文件以情境式 (Scenario-Based) 教學，模擬企業實際日常維運、開發者協作、安全事件處理與進階 DevOps 流程。  
> 每個情境包含：**背景說明 → 角色 → 操作步驟 → 預期結果 → 驗證方式**。  
> 最後更新：2026-03-31

---

## 目錄

### 基礎營運情境
1. [情境一：新 API 服務上架 — 全流程配置](#情境一新-api-服務上架--全流程配置)
2. [情境二：API 過量限流 — 緊急保護後端](#情境二api-過量限流--緊急保護後端)
3. [情境三：惡意 IP 攻擊與手動封鎖](#情境三惡意-ip-攻擊與手動封鎖)
4. [情境四：SSL 憑證到期更換](#情境四ssl-憑證到期更換)
5. [情境五：開發者使用 Mock Lab 進行前後端聯調](#情境五開發者使用-mock-lab-進行前後端聯調)

### 合規與安全情境
6. [情境六：API 回應個資洩漏 — PII 遮蔽緊急處置](#情境六api-回應個資洩漏--pii-遮蔽緊急處置)
7. [情境七：合規稽核 — 完整審計報告產出](#情境七合規稽核--完整審計報告產出)

### 進階營運情境
8. [情境八：新微服務上線 — 全流程 Gateway 配置](#情境八新微服務上線--全流程-gateway-配置)
9. [情境九：效能瓶頸定位與壓力測試](#情境九效能瓶頸定位與壓力測試)
10. [情境十：消費者權限分級與 Tier 管理](#情境十消費者權限分級與-tier-管理)
11. [情境十一：全域限流插件上線](#情境十一全域限流插件上線)

### 進階 DevOps 情境
12. [情境十二：灰度發布 (Canary Release) — 安全升版上線](#情境十二灰度發布-canary-release--安全升版上線)
13. [情境十三：熔斷器配置 — 保護下游服務](#情境十三熔斷器配置--保護下游服務)
14. [情境十四：開發者自助申請 API 存取權限](#情境十四開發者自助申請-api-存取權限)
15. [情境十五：API 生命週期管理 — 從上線到退役](#情境十五api-生命週期管理--從上線到退役)
16. [情境十六：黑名單 Drift 偵測與修復](#情境十六黑名單-drift-偵測與修復)
17. [情境十七：SDK 整合 — 自動化維運腳本](#情境十七sdk-整合--自動化維運腳本)
18. [情境十八：日誌分析 — 使用 ELK 定位問題](#情境十八日誌分析--使用-elk-定位問題)

### 附錄
- [情境速查表](#情境速查表)
- [角色權限速查](#角色權限速查)

---

## 情境一：新 API 服務上架 — 全流程配置

### 背景

產品團隊開發了一個新的 Order Service (`/api/v2/orders`)，後端已部署完成，現需要透過 API Gateway 對外開放並納入管理。

### 角色

- **API 管理員** (Admin)
- **後端開發者** (Developer)

### 操作步驟

#### Step 1：建立 Upstream (上游節點)

1. 開啟 `/upstreams-management`
2. 點擊 **"Create Upstream"**
3. 填入：
   - **Name:** `order-service-v2`
   - **Type:** `roundrobin`
   - **Scheme:** `http`
   - **Nodes:** `order-svc:8080` (weight: 100)
4. 點擊 **Save**

#### Step 2：建立 Service (服務定義)

1. 開啟 `/services-management`
2. 點擊 **"Create Service"**
3. 填入：
   - **Name:** `Order Service v2`
   - **Description:** `訂單服務第二版，支援批量下單`
   - **Upstream:** 選擇剛才建立的 `order-service-v2`
4. 記下產生的 Service ID (如 `00000000000000000456`)

#### Step 3：建立 Route (路由)

1. 開啟 `/routes-management`
2. 點擊 **"Create Route"**
3. 填入：
   - **Name:** `order-v2-all`
   - **URI:** `/api/v2/orders/*`
   - **Methods:** `GET`, `POST`, `PUT`, `DELETE`
   - **Service ID:** 貼上 Step 2 的 Service ID
   - **Plugins (JSON):**
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
4. 點擊 **Save**

#### Step 4：驗證上架成功

1. 開啟 `/gateway`，確認 Routes 數量 +1
2. 開啟 `/api-list`，確認新路由出現在清單中
3. 使用 curl 驗證：
   ```bash
   curl -i http://localhost:9080/api/v2/orders
   # 預期回傳 200 OK (或後端定義的回應)
   ```
4. 在 `/audit-logs` 中確認建立記錄已寫入

### 預期結果

- Gateway Dashboard 的 Routes / Services / Upstreams 計數各 +1
- 新 API 可透過 Gateway 正常存取
- Prometheus 開始收集該路由的指標

---

## 情境二：API 過量限流 — 緊急保護後端

### 背景

某合作夥伴的自動化程式出現 Bug，1 分鐘內對 `/api/v1/products` 發送了 5,000 次請求，遠超正常的 500 次/分鐘上限。需要即時限流保護後端。

### 角色

- **API 管理員** (Admin)

### 操作步驟

#### Step 1：確認流量異常

1. 開啟 `/consumer-analytics` (Traffic Intelligence Center)
2. 在 **Route Filter** 輸入 `/api/v1/products`
3. 觀察到請求量異常飆升
4. 在 **Performance Bottlenecks** 區塊確認該 Route 被標記

#### Step 2：緊急加掛限流插件

1. 開啟 `/routes-management`
2. 找到 URI 為 `/api/v1/products` 的路由，點擊 **Edit**
3. 在 Plugins JSON 中加入：
   ```json
   {
     "limit-count": {
       "count": 500,
       "time_window": 60,
       "rejected_code": 429,
       "key_type": "var",
       "key": "remote_addr"
     },
     "limit-req": {
       "rate": 10,
       "burst": 20,
       "rejected_code": 503,
       "key_type": "var",
       "key": "remote_addr"
     }
   }
   ```
4. 點擊 **Save**，APISIX 即時生效

#### Step 3：設定告警規則

1. 開啟 `/alert-rules`
2. 新增規則：
   - **Rule Name:** `products-rate-limit-alert`
   - **Metric:** `High Frequency IP`
   - **Threshold:** `500`
   - **Duration:** `1m`
   - 勾選 **Mattermost** 通知
3. 點擊 **"Add Rule"**

#### Step 4：驗證限流生效

```bash
# 快速送出多次請求
for i in $(seq 1 600); do
  curl -s -o /dev/null -w "%{http_code}\n" http://localhost:9080/api/v1/products
done

# 前 500 次應回傳 200，之後回傳 429 Too Many Requests
```

### 預期結果

- 超過 500 次/分鐘的請求被回傳 `429 Too Many Requests`
- 相同 IP 的瞬時突發超過 20 次/秒時回傳 `503`
- 告警規則觸發，Mattermost 收到通知
- 後端服務不再因大量請求而過載

---

## 情境三：惡意 IP 攻擊與手動封鎖

### 背景

安全系統偵測到 IP `45.33.32.156` 在短時間內對多個 API Endpoint 進行掃描式探測（嘗試 SQL Injection 和 Path Traversal），需要立即封鎖。

### 角色

- **安全維運工程師** (SecOps)

### 操作步驟

#### Step 1：確認攻擊行為

1. 開啟 **Kibana** (`http://localhost:5601`)
2. 搜尋 `source.ip: "45.33.32.156"` 查看請求記錄
3. 確認存在 `/../etc/passwd`、`' OR 1=1 --` 等惡意 Payload

#### Step 2：手動加入 IP 黑名單

1. 開啟 `/blacklist`
2. 在輸入框輸入 `45.33.32.156`
3. 點擊 **"Add to Blacklist"**
4. 列表即時更新，顯示被封鎖的 IP

#### Step 3：驗證封鎖

```bash
# 模擬該 IP 的請求 (若在本地測試需使用 X-Forwarded-For)
curl -H "X-Forwarded-For: 45.33.32.156" http://localhost:9080/api/v1/health
# 預期回傳 403 Forbidden
```

#### Step 4：記錄與通報

1. 開啟 `/audit-logs` 確認封鎖操作已記錄
2. 匯出 CSV 存檔供資安團隊分析

#### 後續：解除封鎖（誤判情況）

1. 開啟 `/blacklist`
2. 找到 `45.33.32.156`，點擊 **Delete** 按鈕
3. 該 IP 立即恢復存取

---

## 情境四：SSL 憑證到期更換

### 背景

域名 `api.milk-platform.com` 的 SSL 憑證將在 7 天後到期，需要更新憑證以避免 HTTPS 中斷。

### 角色

- **基礎設施管理員** (Infra)

### 操作步驟

#### Step 1：準備新憑證

從 CA (Certificate Authority) 取得新的憑證文件：
- `fullchain.pem` — 完整憑證鏈
- `privkey.pem` — 私鑰

#### Step 2：上傳新憑證

1. 開啟 `/ssl-management`
2. 找到現有的 `api.milk-platform.com` 憑證
3. 點擊 **Edit** (或刪除舊的，再 **"Upload Certificate"**)
4. 填入：
   - **SNIs:** `api.milk-platform.com, *.milk-platform.com`
   - **Certificate:** 貼上 `fullchain.pem` 內容
   - **Private Key:** 貼上 `privkey.pem` 內容
5. 點擊 **Save**

#### Step 3：驗證

```bash
# 測試 HTTPS 連線
curl -v https://api.milk-platform.com/api/v1/health 2>&1 | grep "expire date"
# 應顯示新的到期日期

# 或使用 openssl
openssl s_client -connect api.milk-platform.com:443 -servername api.milk-platform.com 2>/dev/null | openssl x509 -noout -dates
```

#### Step 4：設定到期提醒

1. 開啟 `/alert-rules`
2. 新增告警規則監控 SSL 到期天數（自定義 Metric）

### 預期結果

- HTTPS 連線正常，使用新憑證
- 舊憑證到期不影響服務
- SNI 路由正確匹配域名

---

## 情境五：開發者使用 Mock Lab 進行前後端聯調

### 背景

前端團隊需要開發「用戶資料」頁面，但後端 User Service 尚未完成。需要先用 Mock 回應進行開發。

### 角色

- **前端開發者** (Frontend Dev)

### 操作步驟

#### Step 1：建立 Mock Route

1. 開啟 `/routes-management`
2. 建立一條新路由：
   - **Name:** `mock-user-profile`
   - **URI:** `/api/v1/users/profile`
   - **Methods:** `GET`
3. 不填 Upstream（稍後由 Mock 處理）

#### Step 2：配置 Mock Response

1. 開啟 `/mock-lab`
2. 點擊 **"Create Mock Response"**
3. 填入：
   - **Route ID:** 剛才建立的路由 ID
   - **HTTP Status Code:** `200`
   - **Content-Type:** `application/json`
   - **Response Body:**
     ```json
     {
       "id": "usr_123456",
       "name": "Ted Wang",
       "email": "ted@milk-platform.com",
       "role": "admin",
       "tier": "gold",
       "created_at": "2025-01-15T08:30:00Z",
       "quota": {
         "daily_limit": 10000,
         "used": 3456
       }
     }
     ```
4. 啟用 **Toggle Switch**

#### Step 3：前端開發驗證

```bash
curl http://localhost:9080/api/v1/users/profile
# 回傳 Mock 的 JSON 內容
```

#### Step 4：後端完成後切換

1. 修改路由，加入真實的 Upstream/Service ID
2. 回到 `/mock-lab`，關閉該 Mock 的 Toggle Switch
3. 驗證 API 回傳真實數據

### 預期結果

- 前端可在後端未完成時獨立開發
- Mock 回應格式與真實 API 規格一致
- 切換無縫，不需要前端修改程式碼

---

## 情境六：API 回應個資洩漏 — PII 遮蔽緊急處置

### 背景

合規部門發現 `/api/v1/customers` 的回應中直接暴露了用戶的 Email 和手機號碼，違反個資保護政策。需要在 **不修改後端程式碼** 的情況下緊急遮蔽。

### 角色

- **合規管理員** (Compliance Officer)
- **API 管理員** (Admin)

### 操作步驟

#### Step 1：確認問題

```bash
curl http://localhost:9080/api/v1/customers/123
# 回傳:
# {
#   "id": 123,
#   "name": "王小明",
#   "email": "ming@example.com",     ← 個資暴露
#   "phone": "0912-345-678",          ← 個資暴露
#   "address": "台北市信義區..."       ← 個資暴露
# }
```

#### Step 2：新增 PII 遮蔽規則

1. 開啟 `/pii-management`
2. 點擊 **"Add New Rule"**
3. 新增三條規則：

| Route ID | Field Path | Regex | 說明 |
|---|---|---|---|
| (customers 路由 ID) | `email` | `(.+)@(.+)` | 遮蔽 Email @ 前的部分 |
| (customers 路由 ID) | `phone` | `.*` | 完全遮蔽手機號 |
| (customers 路由 ID) | `address` | `.*` | 完全遮蔽地址 |

4. 確認所有規則狀態為 **active**

#### Step 3：驗證遮蔽效果

```bash
curl http://localhost:9080/api/v1/customers/123
# 預期回傳:
# {
#   "id": 123,
#   "name": "王小明",
#   "email": "***@***.com",
#   "phone": "***",
#   "address": "***"
# }
```

#### Step 4：審計記錄

1. 開啟 `/audit-logs` 確認規則建立記錄
2. 匯出報表提供合規部門存查

### 預期結果

- 敏感欄位即時遮蔽，無需後端 Hotfix
- 非敏感欄位 (id, name) 不受影響
- 合規部門確認符合個資保護政策

---

## 情境七：合規稽核 — 完整審計報告產出

### 背景

公司年度資安稽核即將進行，稽核員要求提供完整的 API 管理操作紀錄、存取權限清單與合規性報表。要求在 2 小時內整理好所有資料。

### 角色

- **合規管理員** (Compliance Officer)
- **資安稽核師** (Auditor)

### 操作步驟

#### Step 1：匯出審計日誌

1. 開啟 `/audit-logs`
2. 確認 KPI 卡片顯示正確的事件總數
3. 使用日期篩選器選擇稽核週期 (如 `2026-01-01` ~ `2026-03-31`)
4. 點擊 **"Export CSV Report"** → 下載 `audit-logs.csv`
5. 確認 CSV 包含：操作時間、操作者、操作類型、目標資源、詳細內容

#### Step 2：檢視 API 資產清冊

1. 開啟 `/api-inventory`
2. 確認所有 API 都已標記風險等級 (L1/L2/L3)
3. 確認稽核日期記錄完整
4. 點擊 **Export** 匯出治理報告

#### Step 3：驗證消費者權限

1. 開啟 `/consumers`
2. 列出所有消費者與其角色 (admin/developer/viewer)
3. 確認每個消費者的 Scopes 符合最小權限原則
4. 開啟 `/consumer-groups` 確認 Tier 配額設定合理

#### Step 4：產出統計報表

1. 開啟 `/reports`
2. 查看各消費者的 24h 使用量與錯誤率
3. 點擊 **Export CSV** 匯出統計資料

#### Step 5：PII 防護驗證

1. 開啟 `/pii-management`
2. 確認所有含敏感資訊的路由都已配置遮蔽規則
3. 驗證 Email/Phone/SSN 等欄位規則狀態為 **active**

#### Step 6：整理稽核報告

將下列檔案提供給稽核師：
| 報告 | 來源 | 說明 |
|------|------|------|
| `audit-logs.csv` | Audit Logs | 操作紀錄全量匯出 |
| `api-inventory-report` | API Inventory | 資產清冊 + 風險分級 |
| `consumer-report.csv` | Reports | 消費者使用統計 |
| PII 規則截圖 | PII Management | 脫敏規則配置證明 |

### 預期結果

- 稽核師可追溯任何操作的 Who/When/What 完整紀錄
- API 風險分級符合公司治理標準
- 消費者權限遵循最小特權原則
- PII 脫敏規則完整覆蓋所有敏感欄位

---

## 情境八：新微服務上線 — 全流程 Gateway 配置

### 背景

團隊要上線一個全新的 Payment Service，包含三個 API Endpoint，需要完整的 Gateway 配置（路由、限流、監控、權限）。

### 角色

- **API 管理員** (Admin)
- **後端架構師** (Architect)

### 操作步驟

#### Step 1：建立 Upstream

1. `/upstreams-management` → **Create Upstream**
   - **Name:** `payment-service-prod`
   - **Type:** `roundrobin`
   - **Scheme:** `https`
   - **Nodes:**
     - `payment-node-1:8443` (weight: 50)
     - `payment-node-2:8443` (weight: 50)
   - **Retries:** `3`

#### Step 2：建立 Service

1. `/services-management` → **Create Service**
   - **Name:** `Payment Service`
   - **Description:** `信用卡/電子支付處理服務`
   - **Upstream:** 選擇 `payment-service-prod`

#### Step 3：建立三條 Route

| Route Name | URI | Methods | 特殊 Plugin |
|---|---|---|---|
| payment-charge | `/api/v1/payments/charge` | POST | `limit-count: 100/min`, `key-auth` |
| payment-refund | `/api/v1/payments/refund` | POST | `limit-count: 50/min`, `key-auth` |
| payment-status | `/api/v1/payments/:id` | GET | `limit-count: 1000/min` |

#### Step 4：建立消費者與 API Key

1. `/consumers` → **新增消費者**
   - **Username:** `payment-partner-alpha`
   - **Roles:** `developer`
   - **Scopes:** `read`, `write`

#### Step 5：加入 PII 遮蔽 (支付卡號)

1. `/pii-management` → **Add New Rule**
   - **Field:** `card_number`
   - **Regex:** `\d{12}` (只保留末四碼)

#### Step 6：設定告警

1. `/alert-rules` → 新增：
   - `payment-5xx-alert` — 5xx Error Spike, threshold=5, 1m
   - `payment-rate-alert` — High Frequency IP, threshold=200, 1m

#### Step 7：驗證

1. `/gateway` — 確認所有計數更新
2. `/consumer-analytics` — 啟用 Auto-Refresh 監控流量
3. `/audit-logs` — 確認所有操作記錄完整

### 預期結果

- Payment Service 三個端點均可透過 Gateway 存取
- 雙節點負載均衡，故障自動重試
- 支付端點有嚴格限流保護
- 卡號資訊已遮蔽
- 5xx 告警已啟用

---

## 情境九：效能瓶頸定位與壓力測試

### 背景

用戶反映 `/api/v1/search` 回應緩慢（P95 延遲 > 3 秒），需要定位瓶頸並確認優化效果。

### 角色

- **SRE / DevOps 工程師**

### 操作步驟

#### Step 1：確認延遲數據

1. 開啟 `/consumer-analytics`
2. 在 **Route Filter** 輸入 `search`
3. 觀察 P95 延遲趨勢圖
4. 確認 **Performance Bottlenecks** 區塊是否標記該 API

#### Step 2：Jaeger 鏈路追蹤

1. 開啟 Jaeger (`http://localhost:16686`)
2. 選擇 Service `apisix`，Operation `search`
3. 查看 Trace 瀑布圖，定位慢查詢 Span

#### Step 3：執行壓力測試 (基準線)

1. 開啟 `/load-testing`
2. 配置：
   - **Target URL:** `http://apisix:9080/api/v1/search?q=test`
   - **Virtual Users:** `50`
   - **Duration:** `60s`
3. 點擊 **"Start Stress Test"**
4. 記錄基準結果：
   - 平均延遲
   - P95 延遲
   - 每秒請求數 (RPS)
   - 錯誤率

#### Step 4：後端優化後重測

> 後端團隊優化（如加 Redis 快取、索引優化）後

1. 再次執行相同壓測配置
2. 比較前後數據

#### Step 5：檢查 Grafana Dashboard

1. 開啟 Grafana (`http://localhost:3000`)
2. 查看 APISIX Dashboard 的 RPS 與延遲面板
3. 確認優化後指標改善

### 預期結果

- 定位出具體的延遲瓶頸（後端 DB? 第三方 API? 網路?）
- 壓測提供量化的效能基準
- 優化前後數據可比較，證明改善幅度

---

## 情境十：消費者權限分級與 Tier 管理

### 背景

公司的 API 對外提供三個級別的服務 (Gold / Silver / Free)，不同級別有不同呼叫配額和 SLA 保障。

### 角色

- **API 產品經理** (PM)
- **API 管理員** (Admin)

### 操作步驟

#### Step 1：建立消費者群組

1. 開啟 `/consumer-groups`
2. 建立三個群組：

| Group Name | Plugins |
|---|---|
| `tier-gold` | `limit-count: 100000/day`, `limit-req: rate=500` |
| `tier-silver` | `limit-count: 10000/day`, `limit-req: rate=100` |
| `tier-free` | `limit-count: 1000/day`, `limit-req: rate=10` |

#### Step 2：建立消費者並分配 Tier

1. 開啟 `/consumers`
2. 新增消費者：

| Username | Group | Labels |
|---|---|---|
| `partner-alpha` | `tier-gold` | `role:admin`, `scope:full` |
| `partner-beta` | `tier-silver` | `role:developer`, `scope:read` |
| `demo-user` | `tier-free` | `role:viewer`, `scope:read` |

#### Step 3：開發者申請升級

1. 開發者在 `/dev-portal` → **Request Access** 提交升級申請
2. 選擇目標 **Performance Tier: Gold**
3. 管理員在後台審核，允許後調整 Consumer Group 綁定

#### Step 4：驗證配額

```bash
# Free tier 使用者：1001 次時觸發限流
curl -H "apikey: DEMO_USER_KEY" http://localhost:9080/api/v1/data
# 第 1001 次回傳 429

# Gold tier 使用者：100000 次才觸發
curl -H "apikey: PARTNER_ALPHA_KEY" http://localhost:9080/api/v1/data
# 可正常使用
```

#### Step 5：監控各 Tier 使用量

1. 開啟 `/consumer-analytics`
2. 按 **Consumer Filter** 依序觀察各消費者的使用量
3. 確認各 Tier 的限流配額正確套用

### 預期結果

- 三級消費者各有獨立配額
- 超額時精確回傳 429
- 升級流程透明，有 Audit Log 記錄

---

## 情境十一：全域限流插件上線

### 背景

公司資安政策要求所有 API 端點必須啟用速率限制，避免 DDoS 攻擊。需要以全域插件方式一次性套用，而非逐條路由修改。

### 角色

- **安全架構師** (Security Architect)
- **API 管理員** (Admin)

### 操作步驟

#### Step 1：規劃全域規則

確定以下預設限流策略：
- 每個 IP 每分鐘最多 600 次請求
- 超出後回傳 429，並在 Header 中告知剩餘額度

#### Step 2：建立 Global Plugin Rule

1. 開啟 `/global-plugins`
2. 點擊 **"Create Global Rule"**
3. 輸入 Rule ID (如 `1`)
4. Plugins JSON：
   ```json
   {
     "limit-count": {
       "count": 600,
       "time_window": 60,
       "rejected_code": 429,
       "key_type": "var",
       "key": "remote_addr",
       "policy": "local",
       "show_limit_quota_header": true
     },
     "prometheus": {},
     "request-id": {
       "header_name": "X-Request-Id",
       "include_in_response": true
     }
   }
   ```
5. 點擊 **Save**

#### Step 3：驗證全域生效

```bash
# 任意端點測試
curl -i http://localhost:9080/api/v1/health
# 回應 Header 應包含:
# X-RateLimit-Limit: 600
# X-RateLimit-Remaining: 599
# X-Request-Id: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

# 確認所有路由都被套用
curl -i http://localhost:9080/api/v2/orders
# 同樣有 RateLimit Header
```

#### Step 4：確認不影響現有路由級限流

- 路由級的 `limit-count` 設定仍然優先
- 全域規則作為 baseline 保護
- 在 `/consumer-analytics` 確認流量未受非預期影響

### 預期結果

- 所有經過 APISIX 的請求都有基本的速率保護
- Response Header 透明顯示限流資訊
- 每個請求都有唯一 Request ID
- Prometheus 指標全面啟用

---

## 情境十二：灰度發布 (Canary Release) — 安全升版上線

### 背景

Order Service 準備從 v1 升級至 v2，但新版有重大邏輯變更。為降低風險，決定採用灰度發布策略：先導入 10% 流量到 v2，確認穩定後逐步增加至 100%。

### 角色

- **DevOps 工程師**
- **後端架構師** (Architect)

### 操作步驟

#### Step 1：確認新版 Upstream 已就緒

確保 v2 的 Upstream 已在 `/upstreams-management` 建立完成：
- `order-service-v1` (現有 Stable)
- `order-service-v2` (新版 Canary)

#### Step 2：建立灰度發布

```bash
# 使用 REST API 建立灰度：90% Stable / 10% Canary
curl -X POST http://localhost:5001/api/canaryrelease \
  -H "X-API-KEY: milk-admin-secret-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "order-api-main",
    "stableUpstreamId": "order-service-v1",
    "canaryUpstreamId": "order-service-v2",
    "stableWeight": 90,
    "canaryWeight": 10
  }'
```

#### Step 3：監控灰度流量

1. 開啟 `/consumer-analytics`
2. 觀察 v2 的回應時間與錯誤率
3. 如錯誤率 < 0.1%，調整流量比例：

```bash
# 調升至 50/50
curl -X PUT http://localhost:5001/api/canaryrelease/1 \
  -H "X-API-KEY: milk-admin-secret-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{"stableWeight": 50, "canaryWeight": 50}'
```

#### Step 4：全量推進或回滾

```bash
# 確認穩定 → 全量推進 (0/100)
curl -X POST http://localhost:5001/api/canaryrelease/1/promote \
  -H "X-API-KEY: milk-admin-secret-key-change-me"

# 如發現問題 → 立即回滾 (100/0)
curl -X POST http://localhost:5001/api/canaryrelease/1/rollback \
  -H "X-API-KEY: milk-admin-secret-key-change-me"
```

#### Step 5：驗證

1. 開啟 `/audit-logs` 確認灰度操作紀錄完整
2. 在 Jaeger (`http://localhost:16686`) 比較 v1 vs v2 的 Trace

### 預期結果

- 灰度期間 v1 使用者完全不受影響
- v2 錯誤可在 10% 流量範圍內被發現
- 回滾操作在秒級內完成
- 全量推進後 v1 Upstream 可安全下線

---

## 情境十三：熔斷器配置 — 保護下游服務

### 背景

Payment Service 的下游銀行 API 偶爾會出現高延遲（> 10 秒），導致連鎖反應影響所有經過 Payment 的請求。需要配置熔斷器在錯誤率超過閾值時自動切斷請求，保護系統穩定。

### 角色

- **SRE 工程師**

### 操作步驟

#### Step 1：識別問題路由

1. 開啟 `/consumer-analytics`
2. 在 **Top 5 Bottlenecks** 確認 Payment 相關路由的 P95 延遲異常
3. 記下 Route ID

#### Step 2：配置熔斷器

```bash
curl -X POST http://localhost:5001/api/circuitbreaker \
  -H "X-API-KEY: milk-admin-secret-key-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "payment-charge",
    "errorThresholdPercent": 50,
    "breakDurationSeconds": 30,
    "halfOpenRequests": 3,
    "windowSizeSeconds": 60
  }'
```

**參數說明：**
| 參數 | 值 | 說明 |
|------|-----|------|
| `errorThresholdPercent` | 50 | 60 秒內錯誤率超過 50% 觸發熔斷 |
| `breakDurationSeconds` | 30 | 熔斷後斷路 30 秒 |
| `halfOpenRequests` | 3 | 半開狀態放行 3 個探測請求 |

#### Step 3：設定告警

1. 開啟 `/alert-rules`
2. 新增 `payment-circuit-break` 規則
3. Metric: `5xx Error Spike`, Threshold: 10, Duration: 1m
4. 勾選 Mattermost 通知

#### Step 4：驗證熔斷行為

當下游 API 異常時：
- 前 50% 錯誤 → 熔斷器觸發 → 後續請求回傳 `503 Service Unavailable`
- 30 秒後 → 半開狀態 → 放行 3 個探測請求
- 探測成功 → 恢復正常流量
- 探測失敗 → 繼續熔斷

### 預期結果

- 下游故障不會雪崩式影響全系統
- 熔斷期間用戶收到明確的 503 回應（而非長時間等待）
- 恢復時自動探測並重新放行

---

## 情境十四：開發者自助申請 API 存取權限

### 背景

新加入的合作夥伴 "PartnerX" 需要存取 API 來整合訂單系統，但尚未取得 API Key。開發者需要透過自助門戶提交申請，管理員審核後自動撥備。

### 角色

- **外部開發者** (PartnerX)
- **API 管理員** (Admin)

### 操作步驟

#### Step 1：開發者提交申請

1. 開發者訪問 `/dev-portal`
2. 點擊 **Request Access** Tab
3. 填寫：
   - **Project Name:** `PartnerX Order Integration`
   - **Contact Email:** `dev@partnerx.com`
   - **Performance Tier:** `Silver` (10,000 req/day)
   - **Reason:** `整合訂單查詢 API，預計每日 5,000 呼叫`
4. 點擊 **Submit Request**
5. 系統即時通知管理員 (Webhook)

#### Step 2：管理員審核

1. 管理員登入系統 (`admin` / `admin`)
2. 使用 API 查詢待審核申請：
   ```bash
   curl http://localhost:5001/api/accessrequest \
     -H "X-API-KEY: milk-admin-secret-key-change-me"
   ```
3. 審核通過：
   ```bash
   curl -X POST http://localhost:5001/api/accessrequest/{id}/approve \
     -H "X-API-KEY: milk-admin-secret-key-change-me"
   ```

#### Step 3：系統自動撥備

審核通過後系統自動：
1. 在 APISIX 建立 Consumer `PartnerX`
2. 生成 API Key 並綁定 Silver Tier 配額
3. 通知開發者 API Key 已就緒

#### Step 4：開發者驗證

```bash
# 使用新的 API Key 取得訂單資料
curl -H "apikey: GENERATED_KEY" http://localhost:9080/api/v1/orders
# 預期回傳 200 OK
```

### 預期結果

- 開發者無需直接聯繫管理員，自助申請
- 管理員有完整的審核記錄
- API Key 自動撥備，不需手動設定 APISIX Consumer
- Audit Log 記錄完整的申請→審核→撥備流程

---

## 情境十五：API 生命週期管理 — 從上線到退役

### 背景

`/api/v1/products` 已運行三年，將被 `/api/v2/products` 取代。需要按照公司 API 治理規範，將 v1 標記為棄用，設定過渡期，最終退役。

### 角色

- **API 產品經理** (PM)
- **API 管理員** (Admin)

### 操作步驟

#### Step 1：記錄生命週期狀態

```bash
# 將 v1 標記為 "deprecated"
curl -X POST http://localhost:5001/api/apilifecycle/{id}/deprecate \
  -H "X-API-KEY: milk-admin-secret-key-change-me"
```

#### Step 2：通知消費者

1. 在 `/consumers` 找到所有使用 v1 的消費者
2. 透過 Webhook 通知遷移時間表：
   - 即日起：v1 標記為 Deprecated，回應 Header 加入 `Sunset: 2026-06-30`
   - 3 個月後：v1 路由轉為唯讀（僅允許 GET）
   - 6 個月後：v1 完全退役

#### Step 3：路由標記棄用 Header

開啟 `/routes-management`，為 v1 路由加入 Plugin：
```json
{
  "response-rewrite": {
    "headers": {
      "set": {
        "Sunset": "Sat, 30 Jun 2026 00:00:00 GMT",
        "Deprecation": "true",
        "Link": "</api/v2/products>; rel=\"successor-version\""
      }
    }
  }
}
```

#### Step 4：監控遷移進度

1. 開啟 `/consumer-analytics`
2. 比較 v1 vs v2 的流量比例
3. 當 v1 流量降至 < 1% 時，可進行最終退役

#### Step 5：完全退役

1. 刪除 v1 路由：`DELETE /api/route/{v1-route-id}`
2. 更新生命週期狀態為 `retired`
3. 在 `/audit-logs` 確認退役操作記錄

### 預期結果

- 消費者有充足的遷移緩衝期
- HTTP Header 提供機器可讀的棄用資訊
- 流量數據支持退役決策
- 完整的生命週期 Audit Trail

---

## 情境十六：黑名單 Drift 偵測與修復

### 背景

資安團隊發現某些已在管理後台移除的 IP 仍然被 APISIX Gateway 封鎖，懷疑 PostgreSQL 資料庫與 APISIX 的黑名單不一致 (Drift)。

### 角色

- **系統管理員** (Admin)

### 操作步驟

#### Step 1：偵測 Drift

1. 開啟 `/sync-status`
2. 查看同步狀態
3. 使用 API 查詢詳細 Drift 報告：
   ```bash
   curl http://localhost:5001/api/syncstatus/blacklist-drift \
     -H "X-API-KEY: milk-admin-secret-key-change-me"
   ```
4. 回應會列出：
   - **DB 有但 APISIX 沒有** 的 IP (遺漏封鎖)
   - **APISIX 有但 DB 沒有** 的 IP (殘留封鎖)

#### Step 2：執行 Reconcile

```bash
# 以 DB 為 Source of Truth，同步至 APISIX
curl -X POST http://localhost:5001/api/syncstatus/reconcile-blacklist \
  -H "X-API-KEY: milk-admin-secret-key-change-me"
```

#### Step 3：驗證

1. 重新查詢 Drift 報告，應回傳空差異
2. 在 `/blacklist` 確認列表與實際封鎖一致
3. 測試之前被殘留封鎖的 IP 可正常存取

#### Step 4：設定自動 Reconcile

MilkWorker 背景服務支援自動定期 Reconcile：
```yaml
# docker-compose.yml 中的 milk-worker 環境變數
Sync__Blacklist__EnableReconcile: true
Sync__Blacklist__ReconcileIntervalSeconds: 60
```

### 預期結果

- Drift 被完整偵測並修復
- DB 成為唯一的 Source of Truth
- 自動 Reconcile 防止未來 Drift 累積

---

## 情境十七：SDK 整合 — 自動化維運腳本

### 背景

DevOps 團隊需要撰寫自動化腳本，定期檢查所有路由的健康狀態、匯出審計日誌、並在特定條件下自動封鎖 IP。

### 角色

- **DevOps 工程師**

### 操作步驟

#### Step 1：使用 C# SDK

```csharp
using MilkApi.Client;

var client = new MilkApiClient("http://localhost:5001", "milk-admin-secret-key-change-me");

// 1. 取得所有路由
var routes = await client.GetRoutesAsync();
Console.WriteLine($"總路由數: {routes.Count}");

// 2. 匯出審計日誌
var auditCsv = await client.ExportAuditLogsAsync();
File.WriteAllText("audit-report.csv", auditCsv);

// 3. 根據條件封鎖 IP
var analytics = await client.GetAnalyticsAsync();
if (analytics.ErrorRate > 10)
{
    await client.AddToBlacklistAsync("suspicious-ip");
    Console.WriteLine("高錯誤率 IP 已自動封鎖");
}
```

#### Step 2：使用 Python SDK

```python
from milk_api import MilkApiClient

client = MilkApiClient("http://localhost:5001", api_key="milk-admin-secret-key-change-me")

# 1. 列出所有消費者
consumers = client.get_consumers()
for c in consumers:
    print(f"{c['username']} - Quota: {c.get('quota', 'N/A')}")

# 2. 批量管理黑名單
suspicious_ips = ["10.0.0.1", "10.0.0.2", "10.0.0.3"]
for ip in suspicious_ips:
    client.add_to_blacklist(ip, reason="Automated scan detection")
    print(f"已封鎖: {ip}")

# 3. 取得 SLA 指標
sla = client.get_sla()
if sla['availability'] < 99.9:
    print(f"⚠️ SLA 警告: {sla['availability']}%")
```

#### Step 3：自動生成最新 SDK

```powershell
# C# SDK (基於 Swagger 自動生成)
.\scripts\generate-sdk.ps1

# Python SDK
.\scripts\generate-python-sdk.ps1
```

### 預期結果

- DevOps 團隊可用程式化方式管理 API Gateway
- 定期排程自動匯出合規報表
- 異常偵測可與封鎖動作串接自動化

---

## 情境十八：日誌分析 — 使用 ELK 定位問題

### 背景

用戶回報「訂單 API 偶爾回傳 500 錯誤」，但後端開發者檢查應用日誌找不到異常。需要透過 ELK Stack 分析完整的請求鏈路。

### 角色

- **SRE 工程師**
- **後端開發者**

### 操作步驟

#### Step 1：查詢 Gateway 訪問日誌

1. 開啟 Kibana (`http://localhost:5601`)
2. 進入 **Discover** 頁面
3. 搜尋：
   ```
   request.uri: "/api/v1/orders*" AND response.status: 500
   ```
4. 時間範圍選擇最近 24 小時

#### Step 2：分析錯誤模式

在 Kibana 中觀察：
- 500 錯誤是否集中在特定時間段？
- 是否來自特定 Consumer / IP？
- 上游回應時間是否異常？

#### Step 3：交叉比對審計日誌

1. 開啟 `/audit-logs`
2. 搜尋同時段是否有路由配置變更
3. 確認是否因配置異動導致 500

#### Step 4：Jaeger 鏈路追蹤

1. 開啟 Jaeger (`http://localhost:16686`)
2. Service 選 `apisix`，搜尋 500 狀態的 Trace
3. 展開 Span 瀑布圖，定位是哪一段返回 500

#### Step 5：定位根因與修復

根據日誌分析結果：
| 根因 | 修復方式 | 操作頁面 |
|------|----------|----------|
| 上游節點不健康 | 移除故障節點 | `/upstreams-management` |
| Plugin 配置錯誤 | 修正 JSON 配置 | `/routes-management` |
| 限流誤觸發 | 調高 limit-count | `/routes-management` 或 `/global-plugins` |
| 後端 Bug | 通知後端團隊修復 | — |

### 預期結果

- 透過 ELK 完整還原問題請求的生命週期
- Jaeger Trace 定位精確到毫秒級故障點
- 結合 Audit Log 排除配置變更因素
- 修復後可在 `/consumer-analytics` 確認錯誤率歸零

---

## 情境速查表

| # | 情境 | 主要頁面 | 關鍵操作 | 適用角色 |
|---|------|----------|----------|----------|
| 1 | 新 API 服務上架 | Upstreams → Services → Routes | 建立完整的三層路由 | Admin |
| 2 | API 過量限流 | Routes + Alert Rules | 加掛 limit-count / limit-req 插件 | Admin |
| 3 | 惡意 IP 封鎖 | IP Blacklist | 手動/自動加入黑名單 | SecOps |
| 4 | SSL 憑證更換 | SSL Management | 上傳新 PEM 憑證 | Infra |
| 5 | Mock Lab 聯調 | Routes + Mock Lab | 建立 Mock Response | Developer |
| 6 | PII 遮蔽緊急處置 | PII Protection | 新增欄位遮蔽規則 | Compliance |
| 7 | 合規稽核 | Audit Logs + Reports + API Inventory | CSV 匯出 + 資產盤點 | Compliance |
| 8 | 微服務全流程上線 | 多頁面協作 | Upstream→Service→Route→Consumer→Alert | Admin + Architect |
| 9 | 效能壓測 | Load Testing + Analytics | k6 壓測 + Jaeger 追蹤 | SRE |
| 10 | 消費者 Tier 管理 | Consumers + Consumer Groups | 分級配額 + DevPortal 申請 | PM + Admin |
| 11 | 全域限流上線 | Global Plugins | 一次性套用所有路由 | Security Architect |
| 12 | 灰度發布 | REST API (canaryrelease) | 按比例切換流量 | DevOps |
| 13 | 熔斷器配置 | REST API (circuitbreaker) | 設定錯誤閾值自動斷路 | SRE |
| 14 | 開發者自助申請 | Dev Portal + AccessRequest | 提交申請 → 管理員審核 | Developer + Admin |
| 15 | API 生命週期管理 | REST API (apilifecycle) | Planning → Active → Deprecated → Retired | PM + Admin |
| 16 | 黑名單 Drift 修復 | Sync Status | 偵測 DB/Gateway 差異並修復 | Admin |
| 17 | SDK 自動化維運 | C#/Python SDK | 批量操作路由與黑名單 | DevOps |
| 18 | ELK 日誌分析 | Kibana + Audit Logs | 結構化日誌查詢 | SRE |

---

## 角色權限速查

| 操作 | Admin | Operator | Viewer |
|------|:-----:|:--------:|:------:|
| 查看所有頁面 | ✅ | ✅ | ✅ |
| 建立/修改路由、服務、Upstream | ✅ | ✅ | ❌ |
| 管理 SSL 憑證 | ✅ | ✅ | ❌ |
| 管理全域插件 | ✅ | ✅ | ❌ |
| 管理消費者與群組 | ✅ | ✅ | ❌ |
| 管理 PII 規則 | ✅ | ✅ | ❌ |
| 管理 Mock 規則 | ✅ | ✅ | ❌ |
| 管理白名單 | ✅ | ✅ | ❌ |
| 管理黑名單 | ✅ | ❌ | ❌ |
| 管理 API Key | ✅ | ❌ | ❌ |
| 審核存取申請 | ✅ | ❌ | ❌ |
| Reconcile 同步 | ✅ | ❌ | ❌ |
| 刪除進階配置 (Cache/Circuit/HealthCheck) | ✅ | ❌ | ❌ |
| 執行壓力測試 | ✅ | ✅ | ❌ |
| 匯出審計日誌 | ✅ | ✅ | ❌ |
| 查看審計日誌 | ✅ | ✅ | ❌ |

---

*功能詳細說明請參考 [USER_GUIDE.md](./USER_GUIDE.md)。*  
*架構細節請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)。*  
*開發者導覽請參考 [ONBOARDING.md](../ONBOARDING.md)。*

*更多操作細節請參考 [USER_GUIDE.md](./USER_GUIDE.md) | 技術架構請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)*
