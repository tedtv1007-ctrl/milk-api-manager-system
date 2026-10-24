# 系統強化工作報告

**日期：** 2026-10-24
**專案：** Milk API Manager System

## 摘要

修正測試環境原本未使用 PostgreSQL 的問題，並讓 CI 真正檢查資料庫遷移與 API readiness。以獨立 PostgreSQL 啟動驗證時，進一步修復兩項會阻斷 PostgreSQL migration 的錯誤，以及啟動時吞掉初始化失敗的行為。

## 完成項目

1. 新增共用資料庫 provider 設定解析：`Database:UseInMemory` 可明確指定資料庫；未設定時保留既有行為（測試模式使用 InMemory）。
2. 測試 Compose 的 API 與 Worker 改用同一個 PostgreSQL，Worker 等 API 完成遷移並通過 health check 後再啟動。
3. 修正 PostgreSQL migration：
   - `AuditLogs.DetailsJson` 從 `text` 轉為 `jsonb` 時明確使用 PostgreSQL `USING` cast，並提供反向轉換。
   - Partial index 條件引用大小寫敏感的 `"Status"` 欄位；同步更新 EF model、migration designer 與 model snapshot。
4. API 啟動初始化失敗時記錄 Critical 後重新拋出例外，避免資料庫遷移失敗仍讓服務啟動。
5. CI 與 Linux 驗證腳本改用 `/health/ready` 驗證資料庫 readiness，等待逾時即失敗；README 補充測試資料庫設定。
6. 新增資料庫 provider 設定的單元測試。

## 驗證結果

| 驗證項目 | 結果 |
|---|---|
| `dotnet test backend\MilkApiManager.sln --no-restore` | 通過，343/343 |
| Docker Compose 設定解析 | 通過 |
| Git Bash `-n` 腳本語法檢查 | 通過 |
| `git diff --check` | 通過 |
| 隔離 PostgreSQL 啟動 smoke test | API readiness HTTP 200；3 筆 EF migration 已套用；`AuditLogs.DetailsJson` 為 `jsonb` |
| Worker PostgreSQL smoke test | Worker 成功啟動並對 `SyncOutboxEntries` 執行資料庫查詢 |

## 技能使用範圍

依本次實際改動採用 API 設計、Backend Testing、TDD、Code Review、PostgreSQL、Docker 與 .NET 慣例技能。另透過 find-skills 找到並檢視 `modern-csharp-coding-standards`。Python/FastAPI、UI 設計及 .NET 發版技能與本次資料庫/CI 修正無直接關係，因此不做無關變更。

## 待處理事項

原測試輸出包含 `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.8.1` 的中度警告（GHSA-4625-4j76-fww9）。後續已將 `MilkApiManager` 與 `MilkShared` 的直接相依版本升級至 `1.15.3`，並在本次相依性評估中一併修補 NuGet audit 列出的其他套件；詳見下方追記。

## 安全修補追記

- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 已於 `MilkApiManager.csproj` 與 `MilkShared.csproj` 升級至 `1.15.3`。
- `dotnet restore backend\MilkApiManager.sln --force-evaluate` 成功；resolved package graph 確認兩個專案均使用 `1.15.3`。
- 完整 .NET 測試通過：MilkApiManager.Tests 339/339，MilkWorker.Tests 5/5。
- `dotnet list backend\MilkApiManager.sln package --vulnerable --include-transitive` 不再列出 `OpenTelemetry.Exporter.OpenTelemetryProtocol` 或 GHSA-4625-4j76-fww9。
- 本次相依性評估追記：下述修補完成後，NuGet audit 不再列出已知漏洞套件。

## NuGet 相依性評估與升級追記

### 相容性決策

- 保留應用程式 `net8.0`、EF Core 8 與 Npgsql EF provider 8，不跨 .NET/EF 主版本升級。
- 對共享資料層的傳遞相依加入明確版本，讓 API、Worker 及 Blazor 使用同一安全版本。
- Blazor 的 EF SQLite provider 8 仍維持原主版本；以 `SQLitePCLRaw.lib.e_sqlite3 3.53.3` 覆寫其原先解析到的 2.1.6 native SQLite runtime。GitHub Advisory 指出受影響版本為 `<=2.1.11`，建議 SQLite 3.50.2 以上；3.53.3 是 NuGet 穩定版。
- `System.Net.Http 4.3.0` 與 `System.Text.RegularExpressions 4.3.0` 的舊傳遞相依追溯至 xUnit 2.5.3/舊 `NETStandard.Library`；升級 xUnit 到 2.9.3（仍在 xUnit 2 主版本），移除這些易受攻擊項目。

### 升級版本

| 套件 | 舊版 | 新版 | 影響範圍 |
|---|---:|---:|---|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.0 | 8.0.2 | MilkShared |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.0 | 8.0.2 | MilkShared |
| `Microsoft.Extensions.Caching.Memory` | 8.0.0 | 8.0.1 | MilkShared、MilkApiManager |
| `Npgsql` | 8.0.0 | 8.0.3 | MilkShared、MilkApiManager |
| `System.Text.Json` | 8.0.0 | 8.0.5 | MilkShared、MilkApiManager、MilkWorker |
| `System.IdentityModel.Tokens.Jwt` | 7.0.3 | 7.1.2 | MilkApiManager |
| `SQLitePCLRaw.lib.e_sqlite3` | 2.1.6 | 3.53.3 | MilkAdminBlazor |
| `xunit` | 2.5.3 | 2.9.3 | 兩個測試專案 |

### 驗證

- `dotnet restore backend\MilkApiManager.sln --force-evaluate` 成功，更新後相依圖可解析。
- `dotnet test backend\MilkApiManager.sln --no-restore` 通過：MilkApiManager.Tests 338/338、MilkWorker.Tests 5/5。
- `dotnet list backend\MilkApiManager.sln package --vulnerable --include-transitive`：六個專案皆回報沒有易受攻擊套件。
- `dotnet nuget why` 確認 SQLite native runtime 經 `Microsoft.EntityFrameworkCore.Sqlite 8.0.0` 解析到 3.53.3；舊測試框架造成的 System.Net.Http/System.Text.RegularExpressions 易受攻擊相依已不再出現。
- 新增 SQLite native provider 整合測試，實際開啟記憶體資料庫並驗證 native engine 為 3.50.2 以上。
