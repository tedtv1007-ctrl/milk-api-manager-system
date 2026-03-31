# MilkDemo 情境操作手冊

> **版本**: 1.0  
> **建立日期**: 2025 年  
> **適用對象**: 系統操作人員、驗收測試人員、展示人員  
> **前置條件**: 系統已啟動，可存取 http://localhost:5002 (前端) 和 http://localhost:5003 (API)

---

## 目錄

1. [情境一：首次登入與系統巡覽](#情境一首次登入與系統巡覽)
2. [情境二：完整商品生命週期管理](#情境二完整商品生命週期管理)
3. [情境三：訂單建立與庫存聯動驗證](#情境三訂單建立與庫存聯動驗證)
4. [情境四：透過 API Gateway 存取業務 API](#情境四透過-api-gateway-存取業務-api)
5. [情境五：安全防護功能驗證](#情境五安全防護功能驗證)
6. [情境六：稽核日誌追蹤操作記錄](#情境六稽核日誌追蹤操作記錄)
7. [情境七：使用 API 測試工具進行端對端驗證](#情境七使用-api-測試工具進行端對端驗證)
8. [情境八：角色權限差異驗證](#情境八角色權限差異驗證)
9. [情境九：Docker 容器化部署與驗證](#情境九docker-容器化部署與驗證)
10. [情境十：E2E 自動化測試執行](#情境十e2e-自動化測試執行)

---

## 情境一：首次登入與系統巡覽

### 目標
驗證使用者可以成功登入系統，並確認所有頁面均可正常存取。

### 步驟

#### Step 1：開啟系統

1. 開啟瀏覽器，輸入 `http://localhost:5002`
2. 系統自動導向登入頁面

<!-- 📸 截圖位置：login-page.png — 登入頁面全貌 -->

#### Step 2：使用 Admin 帳號登入

1. 在 Username 欄位輸入 `admin`
2. 在 Password 欄位輸入 `admin`
3. 點擊 **Login** 按鈕

**預期結果**：
- 自動跳轉至 Dashboard 頁面
- 頂部導覽列顯示目前登入使用者
- 左側導覽列顯示所有功能選單

<!-- 📸 截圖位置：dashboard-after-login.png — 登入後的 Dashboard -->

#### Step 3：巡覽所有頁面

按照以下順序點擊左側導覽列，確認每個頁面均可載入：

| 順序 | 頁面 | 路由 | 預期內容 |
|------|------|------|----------|
| 1 | Dashboard | `/` | 顯示 5 個指標卡片 + 兩個表格 |
| 2 | Products | `/products` | 顯示 10 筆種子資料商品 |
| 3 | Orders | `/orders` | 顯示 2 筆種子資料訂單 |
| 4 | Gateway | `/gateway` | 顯示閘道連線狀態 |
| 5 | Routes | `/routes` | 顯示路由配置資訊 |
| 6 | Security | `/security` | 顯示安全功能說明 |
| 7 | Audit | `/audit` | 顯示稽核日誌查詢 |
| 8 | API Test | `/api-test` | 顯示 HTTP 請求建構器 |
| 9 | About | `/about` | 顯示專案說明 |

#### Step 4：登出

1. 點擊頂部導覽列的 **Logout** 按鈕
2. **預期結果**：返回登入頁面

### 驗收標準
- ✅ 登入成功跳轉至 Dashboard
- ✅ 所有 9 個頁面可正常載入
- ✅ 登出後返回登入頁

---

## 情境二：完整商品生命週期管理

### 目標
驗證商品從建立、查詢、修改到刪除的完整 CRUD 生命週期。

### 步驟

#### Step 1：查看商品列表

1. 登入後前往 **Products** 頁面
2. 確認列表顯示 10 筆種子資料
3. 確認分頁顯示 `Page 1 of 1 (10 total)`

<!-- 📸 截圖位置：products-list.png — 商品列表 -->

#### Step 2：使用分類篩選

1. 點擊分類下拉選單
2. 選擇 **Dairy**
3. **預期結果**：列表僅顯示乳製品（Premium Milk, Low-Fat Yogurt, Cheddar Cheese, Oat Milk）

<!-- 📸 截圖位置：products-filter-dairy.png — Dairy 分類篩選結果 -->

4. 選擇 **All Categories** 恢復全部顯示

#### Step 3：新增商品

1. 點擊 **+ New Product** 按鈕
2. 填入以下資料：
   - Name: `Demo Organic Juice`
   - Description: `Fresh organic orange juice 500ml`
   - Price: `129.00`
   - Stock: `200`
   - Category: `Beverages`
3. 點擊 **Create**

**預期結果**：
- 對話框關閉
- 列表更新，出現新商品
- 總數變為 11

<!-- 📸 截圖位置：products-create-dialog.png — 新增商品對話框 -->
<!-- 📸 截圖位置：products-after-create.png — 新增後的列表 -->

#### Step 4：編輯商品

1. 找到剛建立的 `Demo Organic Juice`
2. 點擊 **Edit** 按鈕
3. 修改 Price 為 `149.00`，Stock 為 `300`
4. 點擊 **Update**

**預期結果**：
- 對話框關閉
- 商品價格更新為 $149.00
- 庫存更新為 300

<!-- 📸 截圖位置：products-edit-dialog.png — 編輯商品對話框 -->

#### Step 5：刪除商品

1. 找到 `Demo Organic Juice`
2. 點擊 **Delete** 按鈕

**預期結果**：
- 商品從列表消失
- 總數恢復為 10

### 驗收標準
- ✅ 商品列表正確顯示種子資料
- ✅ 分類篩選功能正常
- ✅ 可成功新增商品
- ✅ 可成功編輯商品
- ✅ 可成功刪除商品

---

## 情境三：訂單建立與庫存聯動驗證

### 目標
驗證訂單建立流程及庫存自動扣減/回補機制。

### 步驟

#### Step 1：記錄初始庫存

1. 前往 **Products** 頁面
2. 記下 `Premium Milk` 的庫存數量（初始值：500）

#### Step 2：建立訂單

1. 前往 **Orders** 頁面
2. 點擊 **+ New Order** 按鈕
3. 填入客戶資訊：
   - Customer Name: `Test Customer`
   - Email: `test@example.com`
   - Phone: `0987654321`
4. 選擇商品：
   - 從下拉選單選擇 `Premium Milk`
   - 數量輸入 `5`
   - 點擊 **Add Item**
5. 確認小計：`$445.00`（89.00 × 5）
6. 點擊 **Submit Order**

**預期結果**：
- 訂單建立成功
- 訂單狀態為 `Pending`
- 訂單金額為 $445.00

<!-- 📸 截圖位置：orders-create-dialog.png — 建立訂單對話框 -->
<!-- 📸 截圖位置：orders-after-create.png — 新增後的訂單列表 -->

#### Step 3：驗證庫存扣減

1. 返回 **Products** 頁面
2. 查看 `Premium Milk` 庫存

**預期結果**：庫存從 500 減少為 495

<!-- 📸 截圖位置：products-stock-decreased.png — 庫存扣減後 -->

#### Step 4：取消訂單

1. 返回 **Orders** 頁面
2. 找到剛建立的訂單
3. 點擊 **Cancel** 按鈕

**預期結果**：
- 訂單狀態變更為 `Cancelled`（紅色標記）
- Cancel 按鈕不再顯示

<!-- 📸 截圖位置：orders-cancelled.png — 取消後的訂單 -->

#### Step 5：驗證庫存回補

1. 返回 **Products** 頁面
2. 查看 `Premium Milk` 庫存

**預期結果**：庫存恢復為 500

#### Step 6：查看訂單詳情

1. 返回 **Orders** 頁面
2. 點擊訂單的 **Detail** 按鈕
3. 確認對話框顯示：
   - 客戶資訊
   - 訂單項目明細（商品名、數量、單價、小計）
   - 總金額

<!-- 📸 截圖位置：orders-detail-dialog.png — 訂單詳情對話框 -->

### 驗收標準
- ✅ 訂單建立成功，自動計算金額
- ✅ 建立訂單後庫存自動扣減
- ✅ 取消訂單後庫存自動回補
- ✅ 訂單詳情顯示完整資訊

---

## 情境四：透過 API Gateway 存取業務 API

### 目標
驗證 Demo 業務 API 可以透過 APISIX 閘道存取，展示 API Manager 的流量管理能力。

### 前置條件
- Docker 環境已啟動（全部服務）
- 已執行 `scripts/setup-demo-routes.sh`

### 步驟

#### Step 1：確認閘道狀態

1. 前往 **Gateway** 頁面
2. 確認 APISIX Gateway 顯示 **Online**
3. 確認 API Manager 顯示 **Online**

<!-- 📸 截圖位置：gateway-status-online.png — 閘道狀態在線 -->

#### Step 2：直接存取 API（不經過閘道）

使用 API Test 頁面或 curl：

```bash
# 直接呼叫 Demo API
curl http://localhost:5003/api/products
```

**預期結果**：回傳商品列表 JSON

#### Step 3：透過閘道存取同一 API

```bash
# 透過 APISIX 閘道呼叫（路由前綴 /demo）
curl http://localhost:9080/demo/api/products
```

**預期結果**：
- 回傳與 Step 2 相同的商品列表
- Response Header 包含 APISIX 相關標頭

#### Step 4：驗證速率限制

```bash
# 快速連續呼叫超過限制（100 次/60 秒）
for i in $(seq 1 110); do
  curl -s -o /dev/null -w "%{http_code}\n" http://localhost:9080/demo/api/products
done
```

**預期結果**：前 100 次回傳 `200`，之後回傳 `429 Too Many Requests`

#### Step 5：在 API Test 頁面驗證

1. 前往 **API Test** 頁面
2. 點擊 **Gateway (APISIX)** 預設按鈕
3. 點擊 **Send Request**
4. 確認回傳狀態碼 `200`

<!-- 📸 截圖位置：api-test-gateway-result.png — 閘道請求結果 -->

### 驗收標準
- ✅ 閘道狀態顯示 Online
- ✅ 可透過閘道存取 Demo API
- ✅ 閘道路由重寫正確（/demo/api/* → /api/*）
- ✅ 速率限制功能生效

---

## 情境五：安全防護功能驗證

### 目標
驗證 API Manager 的安全防護功能，包含 IP 黑名單與 PII 遮罩。

### 步驟

#### Step 1：查看安全功能頁面

1. 前往 **Security** 頁面
2. 確認頁面顯示安全功能一覽表

<!-- 📸 截圖位置：security-overview.png — 安全功能頁面 -->

#### Step 2：查看 IP 黑名單

1. 在 Security 頁面查看 IP 黑名單區塊
2. 確認從 API Manager 取得的黑名單清單

**說明**：IP 黑名單由 API Manager 管理，透過 `traffic-blocker.lua` Plugin 在 APISIX 閘道層面生效。

#### Step 3：驗證 PII 遮罩效果

1. 在 Security 頁面查看 PII 遮罩示範區塊
2. 確認以下遮罩規則：

| 資料類型 | 原始 | 遮罩後 |
|----------|------|--------|
| Email | `alice@example.com` | `a***@example.com` |
| 手機號碼 | `0912345678` | `091****678` |
| 身份證字號 | `A123456789` | `A1234****9` |

<!-- 📸 截圖位置：security-pii-masking.png — PII 遮罩示範 -->

#### Step 4：驗證 JWT 認證保護

使用 API Test 頁面：

1. 選擇 **GET** 方法
2. 輸入 URL: `http://localhost:5003/api/orders`
3. 點擊 **Send Request**（不攜帶 Token）
4. **預期結果**：回傳 `401 Unauthorized`

5. 登入取得 Token 後重試
6. **預期結果**：回傳 `200 OK` 與訂單列表

### 驗收標準
- ✅ 安全功能頁面正常展示
- ✅ IP 黑名單可從 API Manager 取得
- ✅ PII 遮罩規則正確展示
- ✅ JWT 認證保護生效

---

## 情境六：稽核日誌追蹤操作記錄

### 目標
驗證 API Manager 的稽核日誌功能，追蹤所有管理操作。

### 前置條件
- API Manager 已啟動並有操作紀錄

### 步驟

#### Step 1：查看稽核日誌

1. 前往 **Audit** 頁面
2. 設定查詢筆數為 **25**
3. 確認頁面載入稽核日誌列表

<!-- 📸 截圖位置：audit-logs-list.png — 稽核日誌列表 -->

#### Step 2：確認日誌欄位

每筆稽核日誌應包含：

| 欄位 | 範例 |
|------|------|
| Timestamp | 2025-01-15T10:30:00Z |
| Actor | admin |
| Action | CREATE_ROUTE |
| Target | /demo/api/products |
| Result | Success |
| Source IP | 192.168.1.100 |

#### Step 3：產生新的稽核事件

1. 前往 Milk Admin UI (`http://localhost:5000`)
2. 執行一個管理操作（如建立新路由）
3. 返回 **Audit** 頁面
4. 點擊重新載入
5. **預期結果**：最新的操作記錄出現在列表頂部

#### Step 4：驗證稽核功能說明

確認頁面的稽核功能說明表包含：
- 自動記錄
- 不可竄改
- ELK 整合
- Grafana 儀表板
- Outbox Pattern

### 驗收標準
- ✅ 稽核日誌列表正常載入
- ✅ 日誌包含完整欄位資訊
- ✅ 新操作即時產生日誌記錄
- ✅ 支援不同筆數查詢

---

## 情境七：使用 API 測試工具進行端對端驗證

### 目標
使用內建的 API 測試工具，完成一個完整的業務流程（登入 → 查詢商品 → 建立訂單）。

### 步驟

#### Step 1：測試 Health Check

1. 前往 **API Test** 頁面
2. 點擊 **Health Check** 預設按鈕
3. 點擊 **Send Request**
4. **預期結果**：`200 OK`，回傳 `{"status":"Healthy","timestamp":"..."}`

<!-- 📸 截圖位置：api-test-health.png — Health Check 結果 -->

#### Step 2：取得商品列表

1. 點擊 **Products** 預設按鈕
2. 點擊 **Send Request**
3. **預期結果**：`200 OK`，回傳商品 JSON 列表

<!-- 📸 截圖位置：api-test-products.png — 商品列表結果 -->

#### Step 3：登入取得 Token

1. 選擇 **POST** 方法
2. 輸入 URL: `http://localhost:5003/api/auth/login`
3. 填入 Body:
   ```json
   {"username": "admin", "password": "admin"}
   ```
4. 點擊 **Send Request**
5. 記下回傳的 `token` 值

<!-- 📸 截圖位置：api-test-login.png — 登入回傳 Token -->

#### Step 4：建立新商品

1. 選擇 **POST** 方法
2. 輸入 URL: `http://localhost:5003/api/products`
3. 填入 Body:
   ```json
   {
     "name": "API Test Coffee",
     "description": "Created via API Test tool",
     "price": 199.00,
     "stockQuantity": 50,
     "category": "Beverages"
   }
   ```
4. 點擊 **Send Request**

**注意**：如果回傳 `401`，表示當前登入 Session 的 Token 已透過 API Service 自動附帶。可透過瀏覽器登入後再測試。

<!-- 📸 截圖位置：api-test-create-product.png — 建立商品結果 -->

#### Step 5：確認商品已建立

1. 切換到 **Products** 頁面
2. 確認 `API Test Coffee` 出現在列表中

### 驗收標準
- ✅ Health Check 回傳 200
- ✅ 可查詢商品列表
- ✅ 可發送 POST 請求
- ✅ 回應資訊完整（狀態碼、時間、Body）

---

## 情境八：角色權限差異驗證

### 目標
驗證不同角色帳號的權限差異。

### 步驟

#### Step 1：使用 Admin 帳號測試

1. 以 `admin / admin` 登入
2. 前往 Products 頁面
3. 確認可以看到 **+ New Product**、**Edit**、**Delete** 按鈕
4. 前往 Orders 頁面
5. 確認可以看到 **+ New Order**、**Cancel** 功能

#### Step 2：使用 Viewer 帳號測試

1. 登出
2. 以 `viewer / viewer` 登入
3. 前往 Products 頁面
4. 確認商品列表可以正常讀取

**說明**：前端所有操作均依賴後端 JWT 認證。需 `[Authorize]` 標記的端點（如 POST/PUT/DELETE Products、所有 Orders 端點）會根據 Token 中的角色進行授權檢查。

#### Step 3：API 層級權限測試

使用 curl 測試不同角色的存取：

```bash
# 取得 viewer 的 Token
TOKEN=$(curl -s http://localhost:5003/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"viewer","password":"viewer"}' | jq -r '.token')

# 嘗試建立商品（viewer 角色）
curl -X POST http://localhost:5003/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","price":10,"stockQuantity":1,"category":"Test"}'
```

### 驗收標準
- ✅ Admin 帳號擁有完整 CRUD 權限
- ✅ 不同角色的 Token 可正確簽發
- ✅ 需認證的端點正確拒絕無 Token 請求

---

## 情境九：Docker 容器化部署與驗證

### 目標
驗證整個系統可以透過 Docker Compose 一鍵部署並正確運作。

### 步驟

#### Step 1：啟動所有服務

```bash
cd /path/to/milk-api-manager-system
docker compose up -d --build
```

等待所有服務啟動（約 2-3 分鐘）。

#### Step 2：確認服務健康狀態

```bash
docker compose ps
```

**預期結果**：所有服務狀態為 `running` 或 `healthy`

<!-- 📸 截圖位置：docker-ps-healthy.png — Docker 服務狀態 -->

#### Step 3：設置 APISIX 路由

```bash
bash scripts/setup-demo-routes.sh
```

**預期結果**：
```
[1/3] Creating upstream: milk-demo-api... 201
[2/3] Creating route: /demo/products... 201
[3/3] Creating route: /demo/orders... 201
```

#### Step 4：驗證各服務端點

| 服務 | URL | 預期回應 |
|------|-----|----------|
| Demo WebApp | http://localhost:5002 | 登入頁面 |
| Demo API | http://localhost:5003/health | `{"status":"Healthy"}` |
| Demo API Swagger | http://localhost:5003/swagger | Swagger UI |
| API Manager | http://localhost:5001/health/ready | `Healthy` |
| APISIX Gateway | http://localhost:9080/demo/api/products | 商品列表 |
| Grafana | http://localhost:3000 | 登入頁面 |
| Kibana | http://localhost:5601 | Kibana UI |

#### Step 5：完整業務流程測試

1. 開啟 `http://localhost:5002`
2. 登入
3. 查看 Dashboard 指標
4. 新增商品
5. 建立訂單
6. 前往 Gateway 頁面確認狀態

#### Step 6：關閉服務

```bash
docker compose down
# 如需同時清除資料卷
docker compose down -v
```

### 驗收標準
- ✅ Docker Compose 一鍵啟動成功
- ✅ 所有服務健康狀態正常
- ✅ APISIX 路由設置成功
- ✅ 完整業務流程可正常執行

---

## 情境十：E2E 自動化測試執行

### 目標
執行自動化 E2E 測試，驗證所有功能的正確性。

### 前置條件
- Node.js 18+ 已安裝
- Playwright 已安裝
- Demo 系統已啟動

### 步驟

#### Step 1：安裝測試依賴

```bash
cd e2e
npm install
npx playwright install
```

#### Step 2：執行 API E2E 測試

```bash
npx playwright test tests/demo-api.spec.js
```

**預期結果**：

```
✅ Demo Admin 登入成功
✅ Demo User 登入成功
✅ 錯誤密碼正確被拒
✅ 健康檢查通過
✅ 取得 10 筆商品
✅ 按分類篩選：取得 4 筆 Dairy 商品
✅ 分頁功能正常
✅ 新增商品成功
✅ 未認證建立商品被正確拒絕
✅ 更新商品成功
✅ 刪除商品成功
✅ 取得 4 個商品分類
✅ 取得 2 筆訂單
✅ 建立訂單成功
✅ 取得訂單詳情
✅ 取消訂單成功
✅ 閘道狀態查詢成功
```

<!-- 📸 截圖位置：e2e-api-results.png — API E2E 測試結果 -->

#### Step 3：執行 UI E2E 測試

```bash
npx playwright test tests/demo-ui.spec.js
```

**預期結果**：
- 登入頁面截圖產生
- 所有頁面載入測試通過
- 商品列表顯示種子資料

#### Step 4：查看測試報告

```bash
npx playwright show-report
```

<!-- 📸 截圖位置：e2e-report.png — Playwright 測試報告 -->

#### Step 5：執行完整測試套件（含原有 API Manager 測試）

```bash
# 執行所有 E2E 測試
npx playwright test

# 或僅執行 Demo 相關測試
npx playwright test tests/demo-*.spec.js
```

#### Step 6：查看測試截圖

```bash
# 截圖存放在 test-results/ 目錄
ls test-results/demo-*.png
```

**預期檔案**：
- `demo-login-page.png`
- `demo-after-login.png`
- `demo-dashboard.png`
- `demo-products.png`
- `demo-orders.png`
- `demo-gateway.png`
- `demo-routes.png`
- `demo-security.png`
- `demo-audit.png`
- `demo-api-test.png`
- `demo-about.png`

### 驗收標準
- ✅ API E2E 測試全數通過（17+ 個測試）
- ✅ UI E2E 測試全數通過（12+ 個測試）
- ✅ 測試截圖完整產生
- ✅ 測試報告可檢視

---

## 附錄：截圖清單

以下為各情境需要截圖的位置總覽（供展示準備時參考）：

| 編號 | 檔名 | 情境 | 說明 |
|------|------|------|------|
| 1 | login-page.png | 情境一 | 登入頁面全貌 |
| 2 | dashboard-after-login.png | 情境一 | 登入後 Dashboard |
| 3 | products-list.png | 情境二 | 商品列表 |
| 4 | products-filter-dairy.png | 情境二 | Dairy 分類篩選 |
| 5 | products-create-dialog.png | 情境二 | 新增商品對話框 |
| 6 | products-after-create.png | 情境二 | 新增商品後列表 |
| 7 | products-edit-dialog.png | 情境二 | 編輯商品對話框 |
| 8 | orders-create-dialog.png | 情境三 | 建立訂單對話框 |
| 9 | orders-after-create.png | 情境三 | 建立訂單後列表 |
| 10 | products-stock-decreased.png | 情境三 | 庫存扣減後 |
| 11 | orders-cancelled.png | 情境三 | 取消訂單後 |
| 12 | orders-detail-dialog.png | 情境三 | 訂單詳情 |
| 13 | gateway-status-online.png | 情境四 | 閘道上線狀態 |
| 14 | api-test-gateway-result.png | 情境四 | 閘道請求結果 |
| 15 | security-overview.png | 情境五 | 安全功能頁面 |
| 16 | security-pii-masking.png | 情境五 | PII 遮罩示範 |
| 17 | audit-logs-list.png | 情境六 | 稽核日誌列表 |
| 18 | api-test-health.png | 情境七 | Health Check |
| 19 | api-test-products.png | 情境七 | 商品列表結果 |
| 20 | api-test-login.png | 情境七 | 登入回傳 Token |
| 21 | docker-ps-healthy.png | 情境九 | Docker 服務狀態 |
| 22 | e2e-api-results.png | 情境十 | API E2E 結果 |
| 23 | e2e-report.png | 情境十 | 測試報告 |

---

*本手冊最後更新：2025 年*
