# Milk API Manager System: 金鑰生命週期與 Vault 整合方案 (Issue #16)

## 1. 金鑰發放安全機制
對應保險業 Q3 與 Q9，確保 API Key 在產生、傳遞與存儲過程中的保密性。

### 1.1 簽發流程 (C# Backend + Vault)
1.  **請求**：C# 管理後台發起金鑰生成請求。
2.  **存儲**：呼叫 **HashiCorp Vault** 的 Transit 或 KV Engine 儲存原始金鑰。
3.  **分發**：僅向開發者顯示一次金鑰，並同時更新 APISIX Consumer 的 `key-auth` 插件配置。

## 2. 自動化輪轉 (Rotation) 策略
*   **週期性更換**：設定每 90 天自動輪轉一次金鑰。
*   **預警通知**：金鑰到期前 7 天，自動發送電子郵件或 Mattermost 訊息給專案負責人。
*   **異常撤銷**：偵測到洩漏時，管理後台可一鍵「吊銷」該金鑰並即時同步至 APISIX。

## 3. APISIX 配置連動
```yaml
consumers:
  - username: "tsp-partner-01"
    plugins:
      key-auth:
        key: "VAULT_SECRET_KEY" # 此處由 Controller 定期更新
```
