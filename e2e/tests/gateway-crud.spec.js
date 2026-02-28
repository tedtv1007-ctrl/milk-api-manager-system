const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5001';
const API_KEY = 'milk-admin-secret-key-change-me';
const AUTH_HEADERS = { 'X-API-KEY': API_KEY };

/**
 * Gateway 管理 CRUD 完整生命週期 E2E 測試
 * 驗證 Service、Upstream、SSL、GlobalRule、ServerInfo API 的增刪改查操作
 *
 * 這些測試在 Test Mode (MockApisixClient) 下運行。
 */

// ============================================================
// Service API CRUD
// ============================================================
test.describe.serial('Service API CRUD 完整生命週期', () => {
    const TEST_SERVICE_ID = `e2esvc${Date.now()}`;
    const servicePayload = {
        id: TEST_SERVICE_ID,
        name: 'E2E Test Service',
        description: 'Created by E2E test',
        upstream: {
            type: 'roundrobin',
            nodes: {
                'httpbin.org:80': 1
            }
        }
    };

    test('Create - 建立新 Service', async ({ request }) => {
        const response = await request.put(`${BASE_URL}/api/Service/${TEST_SERVICE_ID}`, {
            headers: AUTH_HEADERS,
            data: servicePayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Service');
        console.log(`✅ 成功建立 Service: ${TEST_SERVICE_ID}`);
    });

    test('Read (List) - 取得 Service 列表', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Service`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);
        const found = data.find(s => s.id === TEST_SERVICE_ID);
        expect(found, `應能找到 Service ${TEST_SERVICE_ID}`).toBeTruthy();
        console.log(`✅ Service 列表中找到 ${TEST_SERVICE_ID}`);
    });

    test('Read (Single) - 取得單一 Service', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Service/${TEST_SERVICE_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Service');
        expect(data).toHaveProperty('upstream');
        console.log(`✅ 成功取得 Service: ${TEST_SERVICE_ID}`);
    });

    test('Update - 修改 Service', async ({ request }) => {
        const updatedPayload = {
            ...servicePayload,
            name: 'E2E Test Service (Updated)',
            description: 'Updated by E2E test',
        };

        const response = await request.put(`${BASE_URL}/api/Service/${TEST_SERVICE_ID}`, {
            headers: AUTH_HEADERS,
            data: updatedPayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Service (Updated)');
        console.log(`✅ 成功修改 Service: ${TEST_SERVICE_ID}`);
    });

    test('Delete - 刪除 Service', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/Service/${TEST_SERVICE_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(204);
        console.log(`✅ 成功刪除 Service: ${TEST_SERVICE_ID}`);
    });

    test('Read after Delete - 驗證 Service 已刪除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Service/${TEST_SERVICE_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect([404, 500]).toContain(response.status());
        console.log(`✅ Service 已成功刪除，回傳 ${response.status()}`);
    });
});

// ============================================================
// Upstream API CRUD
// ============================================================
test.describe.serial('Upstream API CRUD 完整生命週期', () => {
    const TEST_UPSTREAM_ID = `e2eups${Date.now()}`;
    const upstreamPayload = {
        id: TEST_UPSTREAM_ID,
        name: 'E2E Test Upstream',
        desc: 'Created by E2E test',
        type: 'roundrobin',
        nodes: {
            '10.0.0.1:8080': 1,
            '10.0.0.2:8080': 2
        },
        retries: 3,
        scheme: 'http'
    };

    test('Create - 建立新 Upstream', async ({ request }) => {
        const response = await request.put(`${BASE_URL}/api/Upstream/${TEST_UPSTREAM_ID}`, {
            headers: AUTH_HEADERS,
            data: upstreamPayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Upstream');
        expect(data).toHaveProperty('type', 'roundrobin');
        console.log(`✅ 成功建立 Upstream: ${TEST_UPSTREAM_ID}`);
    });

    test('Read (List) - 取得 Upstream 列表', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Upstream`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);
        const found = data.find(u => u.id === TEST_UPSTREAM_ID);
        expect(found, `應能找到 Upstream ${TEST_UPSTREAM_ID}`).toBeTruthy();
        console.log(`✅ Upstream 列表中找到 ${TEST_UPSTREAM_ID}`);
    });

    test('Read (Single) - 取得單一 Upstream', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Upstream/${TEST_UPSTREAM_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Upstream');
        expect(data).toHaveProperty('nodes');
        console.log(`✅ 成功取得 Upstream: ${TEST_UPSTREAM_ID}`);
    });

    test('Update - 修改 Upstream 負載均衡策略', async ({ request }) => {
        const updatedPayload = {
            ...upstreamPayload,
            name: 'E2E Test Upstream (Updated)',
            type: 'ewma',
            retries: 5,
        };

        const response = await request.put(`${BASE_URL}/api/Upstream/${TEST_UPSTREAM_ID}`, {
            headers: AUTH_HEADERS,
            data: updatedPayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('name', 'E2E Test Upstream (Updated)');
        console.log(`✅ 成功修改 Upstream: ${TEST_UPSTREAM_ID}`);
    });

    test('Delete - 刪除 Upstream', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/Upstream/${TEST_UPSTREAM_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(204);
        console.log(`✅ 成功刪除 Upstream: ${TEST_UPSTREAM_ID}`);
    });

    test('Read after Delete - 驗證 Upstream 已刪除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Upstream/${TEST_UPSTREAM_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect([404, 500]).toContain(response.status());
        console.log(`✅ Upstream 已成功刪除，回傳 ${response.status()}`);
    });
});

// ============================================================
// SSL API CRUD
// ============================================================
test.describe.serial('SSL API CRUD 完整生命週期', () => {
    const TEST_SSL_ID = `e2essl${Date.now()}`;
    const sslPayload = {
        id: TEST_SSL_ID,
        cert: '-----BEGIN CERTIFICATE-----\nMIIBkTCB+wIJALRiMLAh...E2E-TEST\n-----END CERTIFICATE-----',
        key: '-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQ...E2E-TEST\n-----END RSA PRIVATE KEY-----',
        snis: ['e2e-test.example.com', '*.e2e-test.example.com'],
        status: 1
    };

    test('Create - 建立新 SSL 憑證', async ({ request }) => {
        const response = await request.put(`${BASE_URL}/api/SSL/${TEST_SSL_ID}`, {
            headers: AUTH_HEADERS,
            data: sslPayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('id', TEST_SSL_ID);
        expect(data).toHaveProperty('snis');
        console.log(`✅ 成功建立 SSL 憑證: ${TEST_SSL_ID}`);
    });

    test('Read (List) - 取得 SSL 列表（不含敏感內容）', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/SSL`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);

        // 列表中應該隱藏 cert/key 內容
        if (data.length > 0) {
            const firstSsl = data[0];
            expect(firstSsl).toHaveProperty('hasCert');
            expect(firstSsl).toHaveProperty('hasKey');
            // 不應包含 cert/key 原文
            expect(firstSsl).not.toHaveProperty('cert');
            expect(firstSsl).not.toHaveProperty('key');
        }
        console.log(`✅ SSL 列表取得成功，共 ${data.length} 筆`);
    });

    test('Read (Single) - 取得單一 SSL 憑證', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/SSL/${TEST_SSL_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('snis');
        expect(data.snis).toContain('e2e-test.example.com');
        console.log(`✅ 成功取得 SSL 憑證: ${TEST_SSL_ID}`);
    });

    test('Create without cert - 缺少憑證應回傳 BadRequest', async ({ request }) => {
        const invalidPayload = {
            snis: ['invalid.example.com'],
            status: 1
            // 缺少 cert 和 key
        };

        const response = await request.put(`${BASE_URL}/api/SSL/invalid-ssl`, {
            headers: AUTH_HEADERS,
            data: invalidPayload,
        });

        expect(response.status()).toBe(400);
        console.log(`✅ 缺少憑證正確回傳 400`);
    });

    test('Delete - 刪除 SSL 憑證', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/SSL/${TEST_SSL_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(204);
        console.log(`✅ 成功刪除 SSL 憑證: ${TEST_SSL_ID}`);
    });

    test('Read after Delete - 驗證 SSL 已刪除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/SSL/${TEST_SSL_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect([404, 500]).toContain(response.status());
        console.log(`✅ SSL 憑證已成功刪除，回傳 ${response.status()}`);
    });
});

// ============================================================
// GlobalRule API CRUD
// ============================================================
test.describe.serial('GlobalRule API CRUD 完整生命週期', () => {
    const TEST_RULE_ID = `e2erule${Date.now()}`;
    const rulePayload = {
        id: TEST_RULE_ID,
        plugins: {
            prometheus: {},
            cors: {
                allow_origins: '*',
                allow_methods: 'GET,POST,PUT,DELETE'
            }
        }
    };

    test('Create - 建立新 Global Rule', async ({ request }) => {
        const response = await request.put(`${BASE_URL}/api/GlobalRule/${TEST_RULE_ID}`, {
            headers: AUTH_HEADERS,
            data: rulePayload,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('plugins');
        console.log(`✅ 成功建立 Global Rule: ${TEST_RULE_ID}`);
    });

    test('Read (List) - 取得 Global Rule 列表', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/GlobalRule`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);
        const found = data.find(r => r.id === TEST_RULE_ID);
        expect(found, `應能找到 Global Rule ${TEST_RULE_ID}`).toBeTruthy();
        console.log(`✅ Global Rule 列表中找到 ${TEST_RULE_ID}`);
    });

    test('Update - 修改 Global Rule Plugins', async ({ request }) => {
        const updatedPayload = {
            id: TEST_RULE_ID,
            plugins: {
                prometheus: {},
                'limit-count': {
                    count: 1000,
                    time_window: 60,
                    rejected_code: 429
                }
            }
        };

        const response = await request.put(`${BASE_URL}/api/GlobalRule/${TEST_RULE_ID}`, {
            headers: AUTH_HEADERS,
            data: updatedPayload,
        });

        expect(response.status()).toBe(200);
        console.log(`✅ 成功修改 Global Rule: ${TEST_RULE_ID}`);
    });

    test('Delete - 刪除 Global Rule', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/GlobalRule/${TEST_RULE_ID}`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(204);
        console.log(`✅ 成功刪除 Global Rule: ${TEST_RULE_ID}`);
    });

    test('Read after Delete - 驗證 Global Rule 已刪除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/GlobalRule`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        const found = data.find(r => r.id === TEST_RULE_ID);
        expect(found, `Global Rule ${TEST_RULE_ID} 應已從列表中移除`).toBeFalsy();
        console.log(`✅ Global Rule ${TEST_RULE_ID} 已成功移除`);
    });
});

// ============================================================
// ServerInfo / Dashboard Stats API
// ============================================================
test.describe.serial('ServerInfo API 讀取', () => {
    test('取得 APISIX Server Info', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/ServerInfo`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const text = await response.text();
        expect(text.length).toBeGreaterThan(0);
        console.log(`✅ ServerInfo 取得成功`);
    });

    test('取得 Dashboard Stats', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/ServerInfo/dashboard`, {
            headers: AUTH_HEADERS,
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('routeCount');
        expect(data).toHaveProperty('serviceCount');
        expect(data).toHaveProperty('upstreamCount');
        expect(data).toHaveProperty('consumerCount');
        expect(data).toHaveProperty('sslCount');
        expect(data).toHaveProperty('globalRuleCount');

        // 所有 count 都應為數字
        expect(typeof data.routeCount).toBe('number');
        expect(typeof data.serviceCount).toBe('number');
        expect(typeof data.upstreamCount).toBe('number');
        console.log(`✅ Dashboard Stats: Routes=${data.routeCount}, Services=${data.serviceCount}, Upstreams=${data.upstreamCount}, Consumers=${data.consumerCount}, SSL=${data.sslCount}, GlobalRules=${data.globalRuleCount}`);
    });
});
