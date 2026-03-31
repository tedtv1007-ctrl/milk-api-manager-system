# 系統強化工作報告 v2 (System Enhancement Work Report v2)

**日期**: 2026-03-31  
**專案**: Milk API Manager System  
**範圍**: 對標企業級 API 管理平台 (Azure APIM / Kong / AWS API Gateway) 進行 6 項核心功能強化  
**方法論**: Test-Driven Development (TDD) — 先寫測試、驗證失敗、實作通過  
**建置結果**: ✅ 0 錯誤, 334 測試全數通過 (新增 79 測試)

---

## 一、強化背景 (Enhancement Rationale)

透過深度分析 Apache APISIX 插件生態、Azure API Management 核心功能、Kong Gateway 架構，以及 AWS API Gateway 特性，識別出本系統與企業級平台之間的功能差距：

| 企業級功能 | Azure APIM | Kong | APISIX 原生 | Milk 系統 (強化前) | 強化後 |
|---|---|---|---|---|---|
| Circuit Breaker (熔斷器) | ✅ | ✅ | ✅ api-breaker | ❌ 無管理介面 | ✅ 完整 CRUD API |
| Response Caching (回應快取) | ✅ | ✅ | ✅ proxy-cache | ❌ 無管理介面 | ✅ 完整 CRUD API |
| Request/Response Transform | ✅ | ✅ | ✅ proxy-rewrite | ❌ 無管理介面 | ✅ 完整 CRUD API |
| Upstream Health Check Config | ✅ | ✅ | ✅ checks | ❌ 無管理介面 | ✅ 完整 CRUD API |
| Canary Release / Traffic Split | ✅ | ✅ | ✅ traffic-split | ❌ 無支援 | ✅ 完整 CRUD + Rollback/Promote |
| API Lifecycle Management | ✅ | ✅ | ❌ | ❌ 無支援 | ✅ 完整生命週期管理 |

---

## 二、新增功能詳述 (Feature Details)

### 🔧 功能 1: Circuit Breaker 熔斷器管理

**問題**: APISIX 內建 `api-breaker` 插件但系統缺乏管理介面，運維人員無法透過控制面板配置熔斷策略。

**解決方案**:
- **Model**: `CircuitBreakerConfig` — 每路由獨立配置熔斷參數
- **Controller**: `CircuitBreakerController` — 完整 CRUD API
- **資料庫**: 新增 `CircuitBreakerConfigs` 表，RouteId 唯一索引
- **稽核**: 所有變更記錄至 Audit Log

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/CircuitBreaker` | 列出所有熔斷配置 |
| GET | `/api/CircuitBreaker/{routeId}` | 查詢指定路由的熔斷配置 |
| POST | `/api/CircuitBreaker` | 新增熔斷配置 |
| PUT | `/api/CircuitBreaker/{routeId}` | 更新熔斷配置 |
| DELETE | `/api/CircuitBreaker/{routeId}` | 刪除熔斷配置 |

**配置參數**:
- `BreakResponseCode`: 熔斷時返回的 HTTP 狀態碼 (預設 502)
- `BreakResponseBody`: 熔斷時返回的訊息體
- `MaxBreakerSec`: 最大熔斷時間 (預設 300 秒，指數退避上限)
- `UnhealthyHttpStatuses`: 判定為不健康的狀態碼 (預設 "500,503")
- `UnhealthyFailures`: 觸發熔斷的連續失敗次數 (預設 3)
- `HealthyHttpStatuses`: 判定為健康的狀態碼 (預設 "200")
- `HealthySuccesses`: 恢復健康的連續成功次數 (預設 3)

**測試涵蓋**: 10 個測試案例

---

### 🗄️ 功能 2: Response Caching 回應快取管理

**問題**: 高頻 API 呼叫缺乏快取策略，增加後端負載與回應延遲。

**解決方案**:
- **Model**: `CachePolicy` — 每路由獨立的快取策略
- **Controller**: `CachePolicyController` — 完整 CRUD API
- **資料庫**: 新增 `CachePolicies` 表，RouteId 唯一索引

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/CachePolicy` | 列出所有快取策略 |
| GET | `/api/CachePolicy/{routeId}` | 查詢指定路由的快取策略 |
| POST | `/api/CachePolicy` | 新增快取策略 |
| PUT | `/api/CachePolicy/{routeId}` | 更新快取策略 |
| DELETE | `/api/CachePolicy/{routeId}` | 刪除快取策略 |

**配置參數**:
- `CacheTtlSeconds`: 快取存活時間 (預設 300 秒)
- `CacheHttpMethods`: 快取的 HTTP 方法 (預設 "GET")
- `CacheHttpStatuses`: 快取的 HTTP 回應碼 (預設 "200")
- `CacheStrategy`: 快取策略 "memory" 或 "disk"
- `CacheKey`: 自訂快取鍵模板
- `VaryHeaders`: 依據 Header 變化快取

**驗證規則**: TTL 必須 ≥ 0

**測試涵蓋**: 12 個測試案例

---

### 🔄 功能 3: Request/Response Transformation 請求/回應轉換

**問題**: API 閘道常需在請求轉發前後修改 Header、URI、Host 等，但系統缺乏管理介面。

**解決方案**:
- **Model**: `RequestTransformRule` — 可配置的轉換規則
- **Controller**: `TransformController` — 完整 CRUD API
- **資料庫**: 新增 `RequestTransformRules` 表，複合索引 (RouteId + Phase + Priority)

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/Transform` | 列出所有轉換規則 |
| GET | `/api/Transform/route/{routeId}` | 查詢指定路由的轉換規則 |
| POST | `/api/Transform` | 新增轉換規則 |
| PUT | `/api/Transform/{id}` | 更新轉換規則 |
| DELETE | `/api/Transform/{id}` | 刪除轉換規則 |

**支援的轉換操作**:
- `add_header` — 新增 HTTP Header
- `remove_header` — 移除 HTTP Header
- `rename_header` — 重新命名 Header
- `rewrite_uri` — 重寫請求 URI
- `rewrite_host` — 重寫目標 Host

**Phase**: `request` (請求階段) 或 `response` (回應階段)

**驗證規則**: Phase 和 OperationType 嚴格枚舉驗證

**測試涵蓋**: 12 個測試案例

---

### 🏥 功能 4: Upstream Health Check 上游健康檢查管理

**問題**: APISIX 支援主動/被動健康檢查但無統一管理介面，難以視覺化監控上游服務狀態。

**解決方案**:
- **Model**: `HealthCheckConfig` — 每 Upstream 獨立的健康檢查配置
- **Controller**: `HealthCheckController` — 完整 CRUD API
- **資料庫**: 新增 `HealthCheckConfigs` 表，UpstreamId 唯一索引

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/HealthCheck` | 列出所有健康檢查配置 |
| GET | `/api/HealthCheck/{upstreamId}` | 查詢指定上游的健康檢查配置 |
| POST | `/api/HealthCheck` | 新增健康檢查配置 |
| PUT | `/api/HealthCheck/{upstreamId}` | 更新健康檢查配置 |
| DELETE | `/api/HealthCheck/{upstreamId}` | 刪除健康檢查配置 |

**Active Health Check (主動探測)**:
- 定時向上游發送探測請求
- 可配置檢查路徑、間隔、成功/失敗閾值、超時

**Passive Health Check (被動監控)**:
- 根據實際流量判斷健康狀態
- 可配置健康/不健康狀態碼、超時次數

**驗證規則**: ActiveIntervalSeconds 必須 > 0

**測試涵蓋**: 11 個測試案例

---

### 🐤 功能 5: Canary Release 金絲雀發佈管理

**問題**: 新版本上線風險高，缺乏漸進式流量切換能力。

**解決方案**:
- **Model**: `CanaryRelease` — 流量分割配置
- **Controller**: `CanaryReleaseController` — 完整 CRUD + Rollback + Promote
- **資料庫**: 新增 `CanaryReleases` 表，MatchRulesJson 支援 JSONB

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/CanaryRelease` | 列出所有金絲雀發佈 |
| GET | `/api/CanaryRelease/{id}` | 查詢指定發佈 |
| POST | `/api/CanaryRelease` | 新增金絲雀發佈 |
| PUT | `/api/CanaryRelease/{id}` | 更新流量權重 |
| POST | `/api/CanaryRelease/{id}/rollback` | 🔴 回滾 (100% 穩定版) |
| POST | `/api/CanaryRelease/{id}/promote` | 🟢 升級 (100% 金絲雀版) |
| DELETE | `/api/CanaryRelease/{id}` | 刪除金絲雀發佈 |

**工作流程**:
1. **建立**: 配置穩定版/金絲雀版 Upstream 與權重 (如 90/10)
2. **觀察**: 監控金絲雀版本的錯誤率與延遲
3. **調整**: 逐步增加金絲雀權重 (50/50, 20/80...)
4. **升級**: Promote 完成全量切換
5. **回滾**: Rollback 在問題時立即回到穩定版

**狀態機**: `active` → `completed` (promote) 或 `rolled_back` (rollback)

**驗證規則**: StableWeight + CanaryWeight 必須等於 100

**測試涵蓋**: 14 個測試案例

---

### 📅 功能 6: API Lifecycle Management API 生命週期管理

**問題**: 企業 API 版本迭代缺乏正式的淘汰流程，消費者不知道何時 API 會下線。

**解決方案**:
- **Model**: `ApiLifecycleEntry` — API 版本生命週期記錄
- **Controller**: `ApiLifecycleController` — 完整 CRUD + Deprecate + Retire + 查詢
- **資料庫**: 新增 `ApiLifecycleEntries` 表，(ApiIdentifier, Version) 唯一索引

**API 端點**:
| Method | Route | 說明 |
|---|---|---|
| GET | `/api/ApiLifecycle` | 列出所有生命週期記錄 |
| GET | `/api/ApiLifecycle/api/{apiIdentifier}` | 按 API 查詢所有版本 |
| GET | `/api/ApiLifecycle/{id}` | 查詢指定記錄 |
| GET | `/api/ApiLifecycle/deprecated` | 📋 列出所有已棄用的 API |
| POST | `/api/ApiLifecycle` | 新增生命週期記錄 |
| PUT | `/api/ApiLifecycle/{id}` | 更新記錄 |
| POST | `/api/ApiLifecycle/{id}/deprecate?notice=...` | ⚠️ 標記為棄用 |
| POST | `/api/ApiLifecycle/{id}/retire` | 🛑 標記為退役 |
| DELETE | `/api/ApiLifecycle/{id}` | 刪除記錄 |

**生命週期狀態**:
```
planning → active → deprecated → retired
```

**關鍵欄位**:
- `PublishedAt`: 上線日期 (進入 active 時自動填充)
- `DeprecatedAt`: 標記棄用日期
- `SunsetAt`: 預定下線日期
- `RetiredAt`: 實際退役日期
- `DeprecationNotice`: 棄用通知訊息
- `SuccessorUrl`: 繼任版本 URL

**驗證規則**: Status 嚴格枚舉驗證 (planning/active/deprecated/retired)

**測試涵蓋**: 20 個測試案例

---

## 三、技術實作統計 (Implementation Statistics)

### 新增檔案

| 類型 | 檔案 | 數量 |
|---|---|---|
| **Models** | `CircuitBreakerConfig.cs`, `CachePolicy.cs`, `RequestTransformRule.cs`, `HealthCheckConfig.cs`, `CanaryRelease.cs`, `ApiLifecycleEntry.cs` | 6 |
| **Controllers** | `CircuitBreakerController.cs`, `CachePolicyController.cs`, `TransformController.cs`, `HealthCheckController.cs`, `CanaryReleaseController.cs`, `ApiLifecycleController.cs` | 6 |
| **Tests** | `CircuitBreakerControllerTests.cs`, `CachePolicyControllerTests.cs`, `TransformControllerTests.cs`, `HealthCheckControllerTests.cs`, `CanaryReleaseControllerTests.cs`, `ApiLifecycleControllerTests.cs` | 6 |

### 修改檔案

| 檔案 | 變更 |
|---|---|
| `MilkShared/Data/AppDbContext.cs` | 新增 6 個 DbSet + 6 段 EF Core Model 配置 (索引、約束、時區轉換) |

### 測試結果

| 指標 | 結果 |
|---|---|
| 建置錯誤 | **0** |
| 建置警告 | **2** (既有 Blazor 警告，非本次變更引入) |
| 既有測試 | **255 / 255 通過** ✅ (零回歸) |
| 新增測試 | **79 / 79 通過** ✅ |
| 總測試數 | **334 / 334 通過** ✅ |

### 新增測試分佈

| 測試類別 | 數量 |
|---|---|
| CircuitBreakerControllerTests | 10 |
| CachePolicyControllerTests | 12 |
| TransformControllerTests | 12 |
| HealthCheckControllerTests | 11 |
| CanaryReleaseControllerTests | 14 |
| ApiLifecycleControllerTests | 20 |
| **合計** | **79** |

---

## 四、安全設計 (Security Design)

所有新增 API 端點均遵循既有安全框架：

| 安全面向 | 實作方式 |
|---|---|
| **認證** | JWT Bearer + API Key 雙重認證 |
| **授權** | 三層 RBAC: Viewer (GET), Operator (POST/PUT), Admin (DELETE) |
| **稽核** | 所有 CUD 操作記錄至 AuditLog |
| **錯誤格式** | 統一使用 `ApiError` record，不洩露堆疊資訊 |
| **輸入驗證** | 枚舉白名單驗證 (Phase, OperationType, Status)、數值範圍驗證 |
| **SQL 注入** | Entity Framework Core 參數化查詢 |
| **結構化日誌** | 使用命名佔位符 `{RouteId}` 而非字串插值 |

---

## 五、資料庫設計 (Database Design)

### 新增資料表

| 資料表 | 主要索引 | 說明 |
|---|---|---|
| `CircuitBreakerConfigs` | `RouteId` (唯一) | 每路由一筆熔斷配置 |
| `CachePolicies` | `RouteId` (唯一) | 每路由一筆快取策略 |
| `RequestTransformRules` | `(RouteId, Phase, Priority)` | 多筆轉換規則按優先序排列 |
| `HealthCheckConfigs` | `UpstreamId` (唯一) | 每上游一筆健康檢查配置 |
| `CanaryReleases` | `RouteId` | 多筆金絲雀發佈 (允許歷史記錄) |
| `ApiLifecycleEntries` | `(ApiIdentifier, Version)` (唯一) | 每 API 每版本一筆生命週期記錄 |

### PostgreSQL 最佳化
- 所有 DateTime 欄位自動轉換為 UTC
- CanaryRelease.MatchRulesJson 使用原生 `jsonb` 類型
- 適當的索引覆蓋常見查詢路徑

---

## 六、系統架構變化 (Architecture Impact)

### 強化前: 24 個 Controllers
### 強化後: 30 個 Controllers (+6)

```
新增 API 端點:
├── /api/CircuitBreaker      (熔斷器管理)
├── /api/CachePolicy         (快取策略管理)
├── /api/Transform           (轉換規則管理)
├── /api/HealthCheck         (健康檢查配置)
├── /api/CanaryRelease       (金絲雀發佈)
└── /api/ApiLifecycle        (API 生命週期)
```

### 對標 APISIX 插件配置能力

| APISIX 插件 | 對應控制面板功能 |
|---|---|
| `api-breaker` | CircuitBreaker Controller |
| `proxy-cache` | CachePolicy Controller |
| `proxy-rewrite` / `response-rewrite` | Transform Controller |
| upstream `checks` | HealthCheck Controller |
| `traffic-split` | CanaryRelease Controller |
| — (企業級功能) | ApiLifecycle Controller |

---

## 七、運用技能 (Applied Skills)

| # | Skill | 應用範圍 |
|---|-------|---------|
| 1 | api-design | RESTful API 端點設計、錯誤回應格式、HTTP 方法語義 |
| 2 | test-driven-development | 先寫 79 個測試、驗證失敗、實作通過 |
| 3 | find-skills | 搜尋可用技能輔助開發 |
| 4 | security-audit | OWASP Top 10 安全合規 (注入防護、授權、稽核) |
| 5 | postgresql-optimization | JSONB 類型、索引策略、EF Core 配置 |
| 6 | net-conventions | 結構化日誌、命名規範、async 模式 |
| 7 | backend-testing | 單元測試策略、InMemory DB、Mock 模式 |

---

## 八、建議後續行動 (Recommended Follow-ups)

| 優先順序 | 建議 |
|----------|------|
| 🔴 高 | 執行 `dotnet ef migrations add AddEnhancementTables` 產生資料庫遷移 |
| 🔴 高 | 實作 Background Worker 將 CircuitBreaker/CachePolicy 配置同步至 APISIX |
| 🟡 中 | 為 Blazor Admin UI 新增 6 個管理頁面 (對應 6 個新 Controller) |
| 🟡 中 | 新增 API Lifecycle 通知功能 (棄用 API 到期前自動通知消費者) |
| 🟡 中 | 為 Canary Release 整合 Prometheus 指標自動判斷是否回滾 |
| 🟢 低 | 新增 OpenAPI/Swagger 文件標註 (`[ProducesResponseType]`) |
| 🟢 低 | 為 Transform 功能新增批量匯入/匯出 CSV 功能 |

---

## 九、影響範圍 (Impact Scope)

```
新增檔案: 18 檔
修改檔案: 1 檔 (AppDbContext.cs)
涵蓋專案:
  ├── MilkShared (Models + DbContext)
  ├── MilkApiManager (Controllers)
  └── MilkApiManager.Tests (Unit Tests)
  
總測試: 334 (新增 79, 既有 255 全數回歸通過)
```
