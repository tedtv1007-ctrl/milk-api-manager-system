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

原測試輸出包含 `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.8.1` 的中度警告（GHSA-4625-4j76-fww9）。後續修補已將 `MilkApiManager` 與 `MilkShared` 的直接相依版本升級至 `1.15.3`；詳見本報告末尾的安全修補追記。

## 安全修補追記

- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 已於 `MilkApiManager.csproj` 與 `MilkShared.csproj` 升級至 `1.15.3`。
- `dotnet restore backend\MilkApiManager.sln --force-evaluate` 成功；resolved package graph 確認兩個專案均使用 `1.15.3`。
- 完整 .NET 測試通過：MilkApiManager.Tests 338/338，MilkWorker.Tests 5/5。
- `dotnet list backend\MilkApiManager.sln package --vulnerable --include-transitive` 不再列出 `OpenTelemetry.Exporter.OpenTelemetryProtocol` 或 GHSA-4625-4j76-fww9。
- NuGet audit 仍列出其他既存傳遞相依套件警告（包括 Microsoft.Extensions.Caching.Memory 8.0.0、Npgsql 8.0.0、System.Text.Json 8.0.0、IdentityModel 7.0.3，以及測試相依套件）。本次依需求僅處理 OpenTelemetry exporter；其餘套件需另行評估升級相容性。
