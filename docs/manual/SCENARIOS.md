# 🎬 Milk API Manager — 模擬操作情境手冊

> 本文件以情境式 (Scenario) 教學，模擬日常維運、開發者協作、與緊急事件處理流程。  
> 每個情境包含：**背景說明 → 操作步驟 → 預期結果 → 驗證方式**。

---

## 目錄

1. [情境一：API 申請上架](#情境一api-申請上架)
2. [情境二：API 過量限流](#情境二api-過量限流)
3. [情境三：惡意 IP 攻擊與自動封鎖](#情境三惡意-ip-攻擊與自動封鎖)
4. [情境四：SSL 憑證即將到期更換](#情境四ssl-憑證即將到期更換)
5. [情境五：開發者使用 Mock Lab 進行前後端聯調](#情境五開發者使用-mock-lab-進行前後端聯調)
6. [情境六：API 回應個資洩漏 — PII 遮蔽緊急處置](#情境六api-回應個資洩漏--pii-遮蔽緊急處置)
7. [情境七：新微服務上線 — 全流程 Gateway 配置](#情境七新微服務上線--全流程-gateway-配置)
8. [情境八：效能瓶頸定位與壓力測試](#情境八效能瓶頸定位與壓力測試)
9. [情境九：消費者權限分級與 Tier 管理](#情境九消費者權限分級與-tier-管理)
10. [情境十：全域限流插件上線](#情境十全域限流插件上線)

---

## 情境一：API 申請上架

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

## 情境二：API 過量限流

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

## 情境三：惡意 IP 攻擊與自動封鎖

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

## 情境四：SSL 憑證即將到期更換

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

## 情境七：新微服務上線 — 全流程 Gateway 配置

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

## 情境八：效能瓶頸定位與壓力測試

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

## 情境九：消費者權限分級與 Tier 管理

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

## 情境十：全域限流插件上線

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

## 情境速查表

| # | 情境 | 主要頁面 | 關鍵操作 |
|---|---|---|---|
| 1 | API 申請上架 | Upstreams → Services → Routes | 建立完整的三層路由 |
| 2 | API 過量限流 | Routes + Alert Rules | 加掛 limit-count / limit-req 插件 |
| 3 | 惡意 IP 封鎖 | IP Blacklist | 手動/自動加入黑名單 |
| 4 | SSL 憑證更換 | SSL Management | 上傳新 PEM 憑證 |
| 5 | Mock Lab 聯調 | Routes + Mock Lab | 建立 Mock Response |
| 6 | PII 遮蔽緊急處置 | PII Protection | 新增欄位遮蔽規則 |
| 7 | 微服務全流程上線 | 多頁面協作 | Upstream→Service→Route→Consumer→Alert |
| 8 | 效能壓測 | Load Testing + Analytics | k6 壓測 + Jaeger 追蹤 |
| 9 | 消費者 Tier 管理 | Consumers + Consumer Groups | 分級配額 + DevPortal 申請 |
| 10 | 全域限流上線 | Global Plugins | 一次性套用所有路由 |

---

*更多操作細節請參考 [USER_GUIDE.md](./USER_GUIDE.md) | 技術架構請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)*
