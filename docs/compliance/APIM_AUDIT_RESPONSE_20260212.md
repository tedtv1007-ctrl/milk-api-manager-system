# APIM 金檢查核回覆說明 (2026-02-12)

本文件針對 APIM 金檢查核問題提供詳細回覆與技術實作說明。

## 1. API 分級標準及管理程序
*   **分級標準**：API 依據資料敏感度分為 `Public` (公開), `Internal` (內部), `Sensitive` (敏感) 三級。
*   **管理程序**：所有 API 需在 APIM (Apache APISIX) 進行註冊，並依分級套用不同的安全策略（如：`Sensitive` 等級強制要求 OIDC 認證）。
*   **定期盤點**：每月由開發部門執行 API 盤點，產出 `API 清單` 與 `授權矩陣`。

## 2. API 部署及使用授權
*   **部署流程**：API 透過 CI/CD 流水線部署。高風險 API 部署需經過資安部門 (Security Review) 簽核。
*   **授權機制**：使用 APISIX Consumer 機制管理存取權限。高風險 API (如金流、個資) 僅開放給特定 Consumer 且需經過雙因子或憑證驗證。

## 3. API 認證及授權程序 (含金鑰管理)
*   **認證方式**：支援 Key Authentication, JWT, 以及 OIDC。
*   **金鑰管理**：API Key 與憑證儲存於加密的 Secret Management 系統。金鑰具備有效期限，並定期進行汰換。

## 4. API 連線存取活動監控
*   **監控活動**：即時監控 API 請求頻率、回應時間與錯誤率。
*   **覆核機制**：監控日誌每日匯總，並由系統管理員每週進行異常行為覆核。

## 5. APIM 管理情形
*   **產品名稱**：Apache APISIX + APISIX Dashboard。
*   **操作日誌**：APISIX Dashboard 記錄所有設定異動，日誌留存於 ElasticSearch。
*   **覆核機制**：重大變更需經過管理員覆核方可生效。

## 6. 參數檢核機制
*   **檢核實作**：利用 APISIX `request-validation` 插件進行參數檢核。
*   **有效範圍**：定義 JSON Schema 驗證傳入參數之型別、範圍及列舉值。
*   **無效處理**：當參數不合法時，APIM 立即回傳 HTTP 400 Bad Request 並終止請求，防止異常資料進入後端。

## 7. API 呼叫紀錄與收容
*   **紀錄內容**：包含 Request ID, Client IP, Timestamp, Response Time, Status Code, Consumer ID。
*   **收容系統**：所有存取紀錄透過 `http-logger` 插件即時推送至集中式日誌收容系統 (ELK Stack)。

## 8. 效能監控與告警
*   **指標訂定**：針對回應時間 (>500ms)、CPU (>80%)、記憶體使用率等訂定告警指標。
*   **自動警示**：整合 Prometheus 與 Grafana，當觸發告警時，透過電子郵件或 Slack 發送警示。

## 9. 敏感資料管控機制 (TSP/代理人)
*   **管控措施**：對第三方機構強制實施 IP 白名單 (IP Restriction) 與特定憑證認證。
*   **金鑰發放**：金鑰發放採限時、限額機制，且所有傳輸均經過 TLS 加密。

---
*本文件由 新光IT團購小幫手 自動生成，以協助金檢查核作業。*
