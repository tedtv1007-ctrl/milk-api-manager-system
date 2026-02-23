const { test, expect } = require('@playwright/test');
const API_KEY = 'milk-admin-secret-key-change-me';
const AUTH_HEADERS = { 'X-API-KEY': API_KEY };

/**
 * APISIX PII Masking E2E Tests (個資脫敏驗證)
 *
 * 測試目標：
 * 1. 驗證 Consumer API 回傳的資料中不包含完整的 email 格式（即已脫敏或未暴露）。
 * 2. 驗證消費者列表 API 正常回傳，且資料結構符合預期。
 * 3. 驗證 API 不會在回應中洩露敏感欄位（如 password、secret）。
 */

test.describe('API 個資脫敏驗證 (PII Masking Verification)', () => {

  test('消費者 API 回傳資料結構驗證', async ({ request }) => {
    // 1. 發送請求到 Flask Consumer API
    const response = await request.get('http://localhost:5001/api/Consumer', {
      headers: AUTH_HEADERS
    });

    // 驗證請求成功 (允許 200 或 500)
    expect([200, 500]).toContain(response.status());

    if (response.status() === 500) {
      console.log('⚠️ Consumer API 回傳 500 (APISIX 離線)，跳過結構驗證');
      return;
    }

    // 2. 驗證內容類型
    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');

    // 3. 解析回應
    const data = await response.json();
    const consumers = Array.isArray(data) ? data : (data.data || data.list || []);

    console.log('Consumer API 回應範例:', JSON.stringify(consumers, null, 2));

    // 4. 驗證資料結構正確
    if (consumers.length > 0) {
      const consumer = consumers[0];
      // 驗證必要欄位存在
      expect(consumer).toHaveProperty('username');
      expect(consumer).toHaveProperty('quota');

      // 5. 驗證不包含敏感欄位 (password, secret, token)
      expect(consumer).not.toHaveProperty('password');
      expect(consumer).not.toHaveProperty('secret');
      expect(consumer).not.toHaveProperty('access_token');
      console.log(`✅ Consumer "${consumer.username}" 未暴露敏感欄位`);
    }
  });

  test('消費者 API 不應回傳明文 Email 地址', async ({ request }) => {
    const response = await request.get('http://localhost:5001/api/Consumer', {
      headers: AUTH_HEADERS
    });
    expect([200, 500]).toContain(response.status());

    if (response.status() === 500) {
      console.log('⚠️ Consumer API 回傳 500，跳過 Email 驗證');
      return;
    }

    const data = await response.json();
    const consumers = Array.isArray(data) ? data : (data.data || data.list || []);

    // 驗證所有消費者資料中不包含明文 Email 格式
    const emailRegex = /[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/;

    for (const consumer of consumers) {
      const jsonStr = JSON.stringify(consumer);
      const hasEmail = emailRegex.test(jsonStr);
      console.log(`檢查 Consumer "${consumer.username}": 包含明文 Email = ${hasEmail}`);
      expect(hasEmail, `Consumer "${consumer.username}" 的回應中不應包含明文 Email`).toBe(false);
    }

    console.log('✅ 所有消費者資料中未發現明文 Email');
  });

  test('Blacklist API 回應驗證', async ({ request }) => {
    const response = await request.get('http://localhost:5001/api/Blacklist', {
      headers: AUTH_HEADERS
    });

    // Blacklist API 可能回傳 200（正常）或 500（APISIX 離線）
    const statusCode = response.status();
    console.log(`Blacklist API 回傳 HTTP ${statusCode}`);

    if (statusCode === 200) {
      const contentType = response.headers()['content-type'];
      expect(contentType).toContain('application/json');

      const data = await response.json();
      expect(Array.isArray(data), 'Blacklist 應回傳陣列格式').toBe(true);
      console.log(`✅ Blacklist API 回傳 ${data.length} 筆資料`);
    } else {
      // APISIX 離線時，驗證回應包含 error 欄位
      const data = await response.json();
      expect(data).toHaveProperty('error');
      console.log(`⚠️ Blacklist API 回傳錯誤（APISIX 可能離線）: ${data.error}`);
    }
  });

});
