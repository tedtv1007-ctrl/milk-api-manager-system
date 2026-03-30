# 系統強化工作報告 (System Hardening Work Report)

**日期**: 2025-07-16  
**專案**: Milk API Manager System  
**範圍**: 運用 12 項技能進行系統全面強化  
**建置結果**: ✅ 0 錯誤, 260 測試全數通過

---

## 一、運用技能 (Applied Skills)

| # | Skill | 應用範圍 |
|---|-------|---------|
| 1 | api-design | 統一 API 錯誤回應格式 |
| 2 | backend-testing | 確保測試在 rate limiting 下仍能通過 |
| 3 | code-review | 全面程式碼審查與修正 |
| 4 | docker | Dockerfile 安全強化 |
| 5 | dotnet-release-management | 建置驗證流程 |
| 6 | fastapi | SSRF 防護設計參考 |
| 7 | net-conventions | 結構化日誌、命名規範 |
| 8 | postgresql-optimization | JSONB GIN 索引、查詢最佳化 |
| 9 | python-project-structure | SDK 結構檢視 |
| 10 | test-driven-development | 測試先行驗證 |
| 11 | web-design-guidelines | Blazor UI 審查 |
| 12 | security-audit (implied) | OWASP Top 10 安全強化 |

---

## 二、變更摘要 (Change Summary)

### 🔒 安全性 (Security) — 6 項修正

#### 1. Swagger 生產環境保護
- **檔案**: `backend/MilkApiManager/Program.cs`
- **問題**: Swagger UI 在所有環境中都可存取，可能洩露 API 結構
- **修正**: 將 `app.UseSwagger()` 和 `app.UseSwaggerUI()` 包裹在 `if (!app.Environment.IsProduction())` 條件中

#### 2. Rate Limiting (速率限制)
- **檔案**: `backend/MilkApiManager/Program.cs`
- **問題**: 無任何請求速率限制，易受暴力破解攻擊
- **修正**: 使用 ASP.NET Core 8 內建 `AddRateLimiter()` 添加兩個策略：
  - `auth`: 每分鐘 5 次（登入端點）
  - `api`: 每分鐘 100 次（一般 API）
  - 測試/Demo 模式下自動放寬限制（每分鐘 10,000 次）

#### 3. SSRF 防護 (Server-Side Request Forgery)
- **檔案**: `backend/MilkShared/Services/NotificationService.cs`
- **問題**: Webhook URL 未驗證，攻擊者可訪問內部網路
- **修正**: 新增 `IsAllowedWebhookUrl()` 方法，封鎖所有私有 IP 範圍：
  - `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`
  - `169.254.0.0/16` (link-local), `127.0.0.0/8` (localhost)

#### 4. Path Traversal 防護
- **檔案**: `backend/MilkShared/Services/VaultService.cs`
- **問題**: 檔案路徑未消毒，可能被注入 `../` 存取任意檔案
- **修正**: 新增 `SanitizeKey()` 方法，過濾 `..`, `/`, `\`, 空白字元等危險字符

#### 5. 敏感資訊日誌降級
- **檔案**: `backend/MilkShared/Services/VaultService.cs`
- **問題**: 明文儲存密鑰的行為僅以 Information 等級記錄
- **修正**: 調整為 Warning 等級記錄

#### 6. Docker Compose 安全參數化
- **檔案**: `docker-compose.yml`
- **問題**: etcd 無認證、Elasticsearch 安全關閉，硬編碼在設定中
- **修正**: 使用環境變數參數化：
  - `${ETCD_ALLOW_NONE_AUTH:-yes}` — 生產環境應設為 `no`
  - `${ES_SECURITY_ENABLED:-false}` — 生產環境應設為 `true`

---

### 🏗️ API 設計 (API Design) — 2 項修正

#### 7. 統一錯誤回應格式
- **檔案**: 全部 24 個 Controllers
- **問題**: 錯誤回應使用不一致的純字串格式
- **修正**: 全面改用 `ApiError` record 格式：
  ```csharp
  // Before
  StatusCode(500, "Internal server error")
  BadRequest("Invalid input")

  // After
  StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."))
  BadRequest(new ApiError("ValidationError", "Invalid input"))
  ```

#### 8. WhitelistController 查詢最佳化
- **檔案**: `backend/MilkApiManager/Controllers/WhitelistController.cs`
- **問題**: 使用鏈式 `.Where().Where()` 造成多次 SQL 條件
- **修正**: 合併為單一 `.Where()` 帶 `&&` 條件

---

### 📋 .NET 慣例 (Conventions) — 2 項修正

#### 9. 結構化日誌修正
- **檔案**: 8 個服務/控制器
  - `SecurityAutomationService.cs` (4 處)
  - `AccessRequestController.cs`
  - `ApisixRouteSyncService.cs`
  - `AdGroupSyncService.cs`
  - `PiiMaskingController.cs`
  - `MockController.cs`
  - `TestExecutionController.cs`
  - `LoadTestService.cs`
- **問題**: 使用 `$""` 字串插值進行日誌記錄，繞過結構化日誌解析
- **修正**: 全面改用命名佔位符格式：
  ```csharp
  // Before
  _logger.LogInformation($"Processing IP {ip}");

  // After
  _logger.LogInformation("Processing IP {Ip}", ip);
  ```

#### 10. Console.WriteLine 移除
- **檔案**: `backend/MilkShared/Services/SecurityAutomationService.cs`
- **問題**: 使用 `Console.WriteLine` 而非結構化 Logger
- **修正**: 改用 `_logger.LogWarning()`

---

### 🐳 Docker 強化 — 3 項修正

#### 11. 非 Root 用戶執行
- **檔案**: 
  - `backend/MilkApiManager/Dockerfile`
  - `backend/MilkAdminBlazor/Dockerfile`
  - `backend/MilkWorker/Dockerfile`
- **問題**: 使用未定義的 `$APP_UID` 變數，容器實際以 root 運行
- **修正**: 使用明確的 `addgroup`/`adduser` 指令建立 UID 1654 的 `appuser`

#### 12. .dockerignore 強化
- **檔案**: `backend/MilkApiManager/.dockerignore`
- **修正**: 新增排除規則：`.git/`, `.github/`, `.vscode/`, `.idea/`, `*.user`, `*.suo`, `*.md`, `__pycache__/`, `*.pyc`, `test-results/`, `e2e/`

---

### 🐘 PostgreSQL 最佳化 — 2 項修正

#### 13. JSONB GIN 索引
- **檔案**: `backend/MilkShared/Data/AppDbContext.cs`
- **問題**: `AuditLogs.DetailsJson` (JSONB) 欄位無索引，JSON 查詢效能差
- **修正**: 新增 GIN 索引 `IX_AuditLogs_DetailsJson_GIN`

#### 14. User 欄位索引
- **檔案**: `backend/MilkShared/Data/AppDbContext.cs`
- **問題**: `AuditLogs.User` 欄位常用於查詢但無索引
- **修正**: 新增索引 `IX_AuditLogs_User`

---

### 🧹 程式碼品質 — 1 項修正

#### 15. Memory Leak 修正
- **檔案**: `backend/MilkWorker/AutoBlockWorker.cs`
- **問題**: `ConcurrentDictionary` 快取無清理機制，長期運行會無限增長
- **修正**: 新增 `CleanupExpiredBlockCache()` 方法，每次迴圈迭代清理 30 分鐘前的過期項目

---

## 三、驗證結果 (Verification)

| 指標 | 結果 |
|------|------|
| 建置錯誤 | **0** |
| 建置警告 | **2** (既有 Blazor 警告，非本次變更引入) |
| 測試通過 | **260 / 260** |
| 測試失敗 | **0** |

---

## 四、建議後續行動 (Recommended Follow-ups)

| 優先順序 | 建議 |
|----------|------|
| 🔴 高 | 生產環境部署前設定 `ETCD_ALLOW_NONE_AUTH=no` 和 `ES_SECURITY_ENABLED=true` |
| 🔴 高 | 執行 `dotnet ef migrations add AddAuditLogIndexes` 產生資料庫遷移 |
| 🟡 中 | 為 AuthController 增加帳戶鎖定機制（連續失敗 N 次鎖定） |
| 🟡 中 | 為 Blazor Admin Dashboard 增加 CSP (Content Security Policy) headers |
| 🟢 低 | 考慮將 Rate Limiting 配置移至 `appsettings.json` 以便動態調整 |
| 🟢 低 | 增加 E2E 測試涵蓋率（目前主要集中在單元/整合測試） |

---

## 五、影響範圍 (Impact Scope)

```
修改檔案數量: ~35 檔
涵蓋專案:
  ├── MilkApiManager (API 主服務)
  ├── MilkAdminBlazor (Blazor 管理介面)  
  ├── MilkWorker (背景排程服務)
  ├── MilkShared (共用程式庫)
  └── docker-compose (容器編排)
```
