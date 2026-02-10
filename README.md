# Milk API Manager System

本專案旨在基於 **Apache APISIX** 建構一套企業級的 API 管理系統（API Management System, APIM）。

## 🚀 專案願景
將高效能的數據面（APISIX）與自定義的管理控制面（Management Plane）結合，提供 API 註冊、安全性控管、流量分析及開發者門戶功能。

## 🛠️ 核心架構規劃
1. **底層引擎 (Data Plane)**: Apache APISIX (高效能路由、插件執行)。
2. **管理中心 (Control Plane)**:
   - 自研管理後台（支援 AI 輔助配置）。
   - 整合 **Keycloak** 進行帳號與權限管理 (RBAC)。
3. **安全防護**:
   - 內建 PII 脫敏插件。
   - WAF (Web Application Firewall) 整合。
4. **可觀測性**: 整合 Prometheus + Grafana 以及 ELK Stack。

## 📅 開發藍圖 (Roadmap)
- [x] **Phase 1**: 基礎設施搭建 (APISIX + Dashboard 容器化佈署)。
- [x] **Phase 2**: API 註冊與自動化路由配置功能。
- [x] **Phase 3**: 整合 Keycloak OIDC 認證流。
- [x] **Phase 4**: 實作自定義管理功能 (API 金鑰管理、配額控管)。

## 🔑 Phase 4: API 金鑰管理與配額控管

### 已完成的功能
- ✅ 添加 PostgreSQL 資料庫用於儲存 API 金鑰與配額資料。
- ✅ 實作 Entity Framework 模型 (ApiKey, Quota, UsageRecord)。
- ✅ 更新 API 金鑰建立流程，包含配額設定。
- ✅ 實作 QuotaService 用於配額檢查與使用記錄。
- ✅ 更新 KeysController 以支援配額管理 API。
- ✅ 整合 APISIX limit-count 插件以強制執行速率限制。
- ✅ 添加資料庫遷移與初始化。

### 新增的 API 端點
- `POST /api/keys`: 建立新 API 金鑰並設定配額。
- `GET /api/keys`: 列出所有 API 金鑰及其配額。
- `PUT /api/keys/{id}/quota`: 更新指定金鑰的配額。

### 環境變數配置
```yaml
DATABASE_CONNECTION_STRING: Host=milkapi-postgres;Database=milkapi;Username=milkapi;Password=milkapi
```

### 資料庫架構
- **ApiKeys**: 儲存 API 金鑰雜湊、擁有者、狀態等。
- **Quotas**: 儲存每金鑰的請求限制 (每分鐘/小時/天)。
- **UsageRecords**: 記錄 API 使用情況以供分析。

## 🔐 Phase 3: Keycloak OIDC 認證整合

### 已完成的功能
- ✅ 添加 Keycloak 服務至 docker-compose.yml
- ✅ 啟用 APISIX openid-connect 插件
- ✅ 在 MilkApiManager 後端實作 OIDC 認證
- ✅ 保護所有管理 API 端點需要認證
- ✅ 建立 Keycloak 管理服務用於自動化配置
- ✅ 添加認證相關的控制器端點 (login/logout/user/setup)

### 服務端口
- **Milk API Manager**: http://localhost:5000 (管理 API)
- **Keycloak**: http://localhost:8080 (認證服務)
- **APISIX**: http://localhost:9080 (API 網關)
- **APISIX Dashboard**: http://localhost:9000 (管理介面)

### 初始設定步驟
1. 啟動所有服務：
   ```bash
   docker-compose up -d
   ```

2. 初始化 Keycloak：
   ```bash
   curl -X POST http://localhost:5000/api/auth/setup
   ```

3. 訪問管理介面：
   - 登入 Keycloak: http://localhost:8080
   - 使用者名稱: admin
   - 密碼: admin

4. 測試 OIDC 認證：
   - 訪問受保護的 API: http://localhost:5000/api/apis
   - 會自動重定向至 Keycloak 登入

### 環境變數配置
```yaml
KEYCLOAK_AUTHORITY: http://keycloak:8080/realms/milk-api-manager
KEYCLOAK_CLIENT_ID: milk-api-manager-client
KEYCLOAK_CLIENT_SECRET: client-secret
```

## 🤝 協作說明
本專案由 **Milk (主代理人)** 與 **龍蝦助手 (外部協作)** 共同開發。
詳細進度請參考 [Issues](https://github.com/tedtv1007-ctrl/milk-api-manager-system/issues)。