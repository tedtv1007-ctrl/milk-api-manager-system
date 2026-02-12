const { test, expect } = require('@playwright/test');

/**
 * APISIX PII Masking E2E Tests
 * 
 * 測試目標：
 * 1. 驗證 API Response 中的敏感資料 (如 Email, 身分證號) 是否經過脫敏處理 (Masking)。
 * 2. 模擬請求並檢查回傳內容。
 * 
 * 注意：
 * - 測試假設後端 `/api/consumers` 或類似 Endpoint 會回傳包含敏感個資的列表。
 * - 預期脫敏格式通常包含 '*' 符號。
 */

test.describe('APISIX PII Masking Verification', () => {

  test('should mask sensitive data (Email, ID) in API response', async ({ request }) => {
    // 1. 發送請求到目標 API (假設 /api/consumers 返回用戶列表)
    // 這裡的路徑應對應到 APISIX Proxy 的路徑
    const response = await request.get('/api/consumers');

    // 驗證請求成功
    expect(response.ok(), 'API request failed').toBeTruthy();

    // 2. 解析回應資料
    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');
    const data = await response.json();

    console.log('API Response Sample:', JSON.stringify(data, null, 2));

    // 3. 驗證資料結構與脫敏
    // 假設回傳格式為 { data: [...] } 或直接是陣列 [...]
    const users = Array.isArray(data) ? data : (data.data || data.list || []);

    if (users.length === 0) {
      console.warn('Warning: No user data returned. Cannot verify PII masking.');
      return;
    }

    // 檢查每一筆資料
    for (const user of users) {
      // 驗證 Email Masking
      if (user.email) {
        // 預期格式範例: t****@example.com 或 *****
        const isMasked = user.email.includes('*');
        console.log(`Checking Email: ${user.email} => Masked: ${isMasked}`);
        expect(isMasked, `Email should be masked: ${user.email}`).toBe(true);
      }

      // 驗證身分證號 (ID Card) Masking
      if (user.id_card || user.national_id) {
        const id = user.id_card || user.national_id;
        const isMasked = id.includes('*');
        console.log(`Checking ID: ${id} => Masked: ${isMasked}`);
        expect(isMasked, `ID Card should be masked: ${id}`).toBe(true);
      }

      // 驗證手機號 (Phone) Masking
      if (user.phone || user.mobile) {
        const phone = user.phone || user.mobile;
        const isMasked = phone.includes('*');
        console.log(`Checking Phone: ${phone} => Masked: ${isMasked}`);
        expect(isMasked, `Phone should be masked: ${phone}`).toBe(true);
      }
    }
  });

  // 測試情境：模擬特定查詢參數，確保回應依然脫敏
  test('should mask sensitive data even when querying specific user', async ({ request }) => {
    // 假設我們知道一個存在的 ID (這可能需要先從 List 獲取，或寫死)
    // 這裡僅作示範邏輯
    const listResponse = await request.get('/api/consumers');
    const listData = await listResponse.json();
    const users = Array.isArray(listData) ? listData : (listData.data || []);

    if (users.length > 0) {
      const targetId = users[0].id;
      if (targetId) {
        const detailResponse = await request.get(`/api/consumers/${targetId}`);
        if (detailResponse.ok()) {
            const detail = await detailResponse.json();
            if (detail.email) {
                expect(detail.email.includes('*'), `Detail API email should be masked: ${detail.email}`).toBe(true);
            }
        }
      }
    }
  });

});
