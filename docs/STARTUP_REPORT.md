啟動報告 - Milk API Manager
===========================

日期: 2026-02-10

摘要:
- 已以 `docker-compose` 建構並啟動整套服務。
- 已確認並修正主機設定：`vm.max_map_count=262144`（已永久寫入 `/etc/sysctl.d/99-elasticsearch.conf`）。
- 啟動順序：先 `etcd`，確認 `etcd` 已就緒，接著啟動 `apisix` 與 `apisix-dashboard`，最後啟動其他服務（elasticsearch、grafana、kibana、logstash、prometheus、jaeger）。

當前狀態（摘錄）:
- `etcd`: Up, SERVING，leader 已選出。
- `apisix`: 已啟動（監聽 9080/9091/9443）。
- `apisix-dashboard`: 已啟動（監聽 9000），啟動期間曾因 `etcd` 尚未就緒出現 `connect: connection refused` 警告，但後續仍成功啟動。
- `elasticsearch`: 已啟動並變為 GREEN（先前有 vm.max_map_count 警告，已修正）。

已執行項目:
1. 檢索並觀察日誌（`apisix`、`apisix-dashboard`、`elasticsearch`）
2. 設定並永久保存 `vm.max_map_count=262144`
3. 逐步啟動服務（先 etcd -> apisix/apidash -> 其餘）
4. 執行 `scripts/sync-openapi-to-apisix.sh`（已觸發）

建議後續:
- 若要驗證完整流量，執行 API 呼叫測試（e.g., curl 到 `apisix` 端點）。
- 若需自動化啟動，可在 `docker-compose.yml` 優化啟動順序或加入 healthcheck 與 restart policy。

備註: 日誌顯示的 `connection refused` 多為 `etcd` 尚未完全就緒時產生，已透過逐步啟動降低該情況。
