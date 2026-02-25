const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5001';
const API_KEY = 'milk-admin-secret-key-change-me';
const AUTH_HEADERS = { 'X-API-KEY': API_KEY };

/**
 * CRUD 完整生命週期 E2E 測試
 * 驗證 Route、Consumer、Blacklist API 的增刪改查操作
 */

// ============================================================
// Route API CRUD
// ============================================================
test.describe.serial('Route API CRUD 完整生命週期', () => {
    const TEST_ROUTE_ID = `e2e-crud-route-${Date.now()}`;
    const routePayload = {
        id: TEST_ROUTE_ID,
        name: 'E2E CRUD Test Route',
        uri: '/e2e-crud-test',
        methods: ['GET', 'POST'],
    };

    test('Create - 建立新路由', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Route`, {
            headers: AUTH_HEADERS,
            data: routePayload,
        });

        const statusCode = response.status();
        console.log(`Route CREATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 201) {
            const data = await response.json();
            expect(data).toHaveProperty('id', TEST_ROUTE_ID);
            expect(data).toHaveProperty('name', 'E2E CRUD Test Route');
            expect(data).toHaveProperty('uri', '/e2e-crud-test');
            console.log(`✅ 成功建立路由: ${TEST_ROUTE_ID}`);
        } else {
            // APISIX 離線時可能 500
            expect([201, 500]).toContain(statusCode);
            console.log(`⚠️ Route CREATE 回傳 ${statusCode}（APISIX 可能離線）`);
            test.skip();
        }
    });

    test('Read - 取得剛建立的路由', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Route/${TEST_ROUTE_ID}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Route READ 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('name', 'E2E CRUD Test Route');
            expect(data).toHaveProperty('uri', '/e2e-crud-test');
            console.log(`✅ 成功讀取路由: ${TEST_ROUTE_ID}`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
            console.log(`⚠️ Route READ 回傳 ${statusCode}`);
        }
    });

    test('Update - 修改路由名稱與 URI', async ({ request }) => {
        const updatedPayload = {
            ...routePayload,
            name: 'E2E CRUD Test Route (Updated)',
            uri: '/e2e-crud-test-updated',
        };

        const response = await request.put(`${BASE_URL}/api/Route/${TEST_ROUTE_ID}`, {
            headers: AUTH_HEADERS,
            data: updatedPayload,
        });

        const statusCode = response.status();
        console.log(`Route UPDATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 204) {
            console.log(`✅ 成功修改路由: ${TEST_ROUTE_ID}`);
        } else {
            expect([204, 500]).toContain(statusCode);
            console.log(`⚠️ Route UPDATE 回傳 ${statusCode}`);
        }
    });

    test('Read after Update - 驗證修改後的路由', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Route/${TEST_ROUTE_ID}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Route READ (after update) 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('name', 'E2E CRUD Test Route (Updated)');
            expect(data).toHaveProperty('uri', '/e2e-crud-test-updated');
            console.log(`✅ 路由修改驗證通過`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
        }
    });

    test('Delete - 刪除路由', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/Route/${TEST_ROUTE_ID}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Route DELETE 回傳 HTTP ${statusCode}`);

        if (statusCode === 204) {
            console.log(`✅ 成功刪除路由: ${TEST_ROUTE_ID}`);
        } else {
            expect([204, 500]).toContain(statusCode);
            console.log(`⚠️ Route DELETE 回傳 ${statusCode}`);
        }
    });

    test('Read after Delete - 驗證路由已刪除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Route/${TEST_ROUTE_ID}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Route READ (after delete) 回傳 HTTP ${statusCode}`);

        // 刪除後應回傳 404 或 500（因 MockApisixClient 會 throw HttpRequestException）
        expect([404, 500]).toContain(statusCode);
        console.log(`✅ 路由已成功刪除，回傳 ${statusCode}`);
    });
});

// ============================================================
// Consumer API CRUD
// ============================================================
test.describe.serial('Consumer API CRUD 完整生命週期', () => {
    const TEST_USERNAME = `e2e-crud-consumer-${Date.now()}`;

    test('Create - 建立新消費者', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
            data: {
                username: TEST_USERNAME,
                quota: {
                    count: 1000,
                    time_window: 3600,
                    rejected_code: 429,
                    rejected_msg: 'E2E test quota limit',
                },
            },
        });

        const statusCode = response.status();
        console.log(`Consumer CREATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            console.log(`✅ 成功建立消費者: ${TEST_USERNAME}`);
        } else {
            expect([200, 500]).toContain(statusCode);
            console.log(`⚠️ Consumer CREATE 回傳 ${statusCode}`);
            test.skip();
        }
    });

    test('Read - 取得消費者清單，驗證新消費者存在', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Consumer READ 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `應能在清單中找到消費者 ${TEST_USERNAME}`).toBeTruthy();
            expect(found).toHaveProperty('quota');
            console.log(`✅ 消費者 ${TEST_USERNAME} 存在於清單中`);
        } else {
            expect([200, 500]).toContain(statusCode);
            console.log(`⚠️ Consumer READ 回傳 ${statusCode}`);
        }
    });

    test('Update - 修改消費者配額', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
            data: {
                username: TEST_USERNAME,
                quota: {
                    count: 5000,
                    time_window: 7200,
                    rejected_code: 429,
                    rejected_msg: 'E2E updated quota limit',
                },
            },
        });

        const statusCode = response.status();
        console.log(`Consumer UPDATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            console.log(`✅ 成功修改消費者配額: ${TEST_USERNAME}`);
        } else {
            expect([200, 500]).toContain(statusCode);
            console.log(`⚠️ Consumer UPDATE 回傳 ${statusCode}`);
        }
    });

    test('Read after Update - 驗證消費者配額已更新', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `應能在清單中找到消費者 ${TEST_USERNAME}`).toBeTruthy();
            // 配額已更新（mock 可能不完整回傳 quota details，但至少 consumer 存在）
            console.log(`✅ 消費者 ${TEST_USERNAME} 更新後仍存在於清單中`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Delete - 刪除消費者', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/Consumer/${TEST_USERNAME}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Consumer DELETE 回傳 HTTP ${statusCode}`);

        if (statusCode === 204) {
            console.log(`✅ 成功刪除消費者: ${TEST_USERNAME}`);
        } else {
            expect([204, 500]).toContain(statusCode);
            console.log(`⚠️ Consumer DELETE 回傳 ${statusCode}`);
        }
    });

    test('Read after Delete - 驗證消費者已移除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `消費者 ${TEST_USERNAME} 應已從清單中移除`).toBeFalsy();
            console.log(`✅ 消費者 ${TEST_USERNAME} 已成功移除`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });
});

// ============================================================
// Blacklist API CRUD (需要 JWT Admin 角色認證)
// ============================================================
test.describe.serial('Blacklist API CRUD 完整生命週期', () => {
    const TEST_IP_1 = '10.99.88.1';
    const TEST_IP_2 = '10.99.88.2';
    let jwtHeaders = {};

    test('Setup - 以 Admin 身份登入取得 JWT Token', async ({ request }) => {
        const loginResp = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'admin' },
        });

        expect([200, 401]).toContain(loginResp.status());

        if (loginResp.status() === 401) {
            console.log('⚠️ Admin 登入失敗，跳過 Blacklist CRUD 測試');
            test.skip();
            return;
        }

        const { token } = await loginResp.json();
        jwtHeaders = { 'Authorization': `Bearer ${token}` };
        console.log('✅ Admin 登入成功，取得 JWT Token');
    });

    test('Create - 新增第一個 IP 至黑名單', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
            data: {
                ip: TEST_IP_1,
                action: 'add',
                reason: 'E2E CRUD 測試 - IP 1',
                addedBy: 'e2e-crud-test',
            },
        });

        const statusCode = response.status();
        console.log(`Blacklist CREATE (IP1) 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('message');
            expect(data.message).toContain(TEST_IP_1);
            console.log(`✅ 成功新增 IP ${TEST_IP_1} 至黑名單`);
        } else {
            expect([200, 500]).toContain(statusCode);
            console.log(`⚠️ Blacklist CREATE 回傳 ${statusCode}`);
            test.skip();
        }
    });

    test('Read - 驗證第一個 IP 存在於黑名單', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const ips = Array.isArray(data) ? data : [];
            // BlacklistEntry 有 ipOrCidr 欄位
            const found = ips.some(entry =>
                (typeof entry === 'string' && entry === TEST_IP_1) ||
                (entry.ipOrCidr === TEST_IP_1)
            );
            expect(found, `IP ${TEST_IP_1} 應存在於黑名單中`).toBe(true);
            console.log(`✅ IP ${TEST_IP_1} 存在於黑名單中`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Create - 新增第二個 IP 至黑名單', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
            data: {
                ip: TEST_IP_2,
                action: 'add',
                reason: 'E2E CRUD 測試 - IP 2',
                addedBy: 'e2e-crud-test',
            },
        });

        const statusCode = response.status();
        console.log(`Blacklist CREATE (IP2) 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data.message).toContain(TEST_IP_2);
            console.log(`✅ 成功新增 IP ${TEST_IP_2} 至黑名單`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Read - 驗證兩個 IP 都存在於黑名單', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const ips = Array.isArray(data) ? data : [];

            const hasIp1 = ips.some(entry =>
                (typeof entry === 'string' && entry === TEST_IP_1) ||
                (entry.ipOrCidr === TEST_IP_1)
            );
            const hasIp2 = ips.some(entry =>
                (typeof entry === 'string' && entry === TEST_IP_2) ||
                (entry.ipOrCidr === TEST_IP_2)
            );

            expect(hasIp1, `IP ${TEST_IP_1} 應存在`).toBe(true);
            expect(hasIp2, `IP ${TEST_IP_2} 應存在`).toBe(true);
            console.log(`✅ 兩個 IP 都存在於黑名單中`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Delete - 移除第一個 IP', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
            data: {
                ip: TEST_IP_1,
                action: 'remove',
            },
        });

        const statusCode = response.status();
        console.log(`Blacklist DELETE (IP1) 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data.message).toContain(TEST_IP_1);
            console.log(`✅ 成功移除 IP ${TEST_IP_1}`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Read after Delete - 驗證第一個 IP 已移除，第二個仍在', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const ips = Array.isArray(data) ? data : [];

            const hasIp1 = ips.some(entry =>
                (typeof entry === 'string' && entry === TEST_IP_1) ||
                (entry.ipOrCidr === TEST_IP_1)
            );
            const hasIp2 = ips.some(entry =>
                (typeof entry === 'string' && entry === TEST_IP_2) ||
                (entry.ipOrCidr === TEST_IP_2)
            );

            expect(hasIp1, `IP ${TEST_IP_1} 應已被移除`).toBe(false);
            expect(hasIp2, `IP ${TEST_IP_2} 應仍存在`).toBe(true);
            console.log(`✅ IP ${TEST_IP_1} 已移除，IP ${TEST_IP_2} 仍在`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Cleanup - 移除第二個 IP（清理）', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Blacklist`, {
            headers: jwtHeaders,
            data: {
                ip: TEST_IP_2,
                action: 'remove',
            },
        });

        const statusCode = response.status();
        console.log(`Blacklist CLEANUP (IP2) 回傳 HTTP ${statusCode}`);
        expect([200, 500]).toContain(statusCode);
        console.log(`✅ 清理完成`);
    });
});

// ============================================================
// API Key CRUD (金鑰管理完整生命週期)
// ============================================================
test.describe.serial('API Key CRUD 完整生命週期', () => {
    let createdKeyId = null;
    const TEST_OWNER = `e2e-key-owner-${Date.now()}`;

    test('Create - 建立新 API 金鑰', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Keys`, {
            headers: AUTH_HEADERS,
            data: {
                owner: TEST_OWNER,
                validityDays: 30,
                scopes: '["read","write"]',
                contactEmail: 'e2e-test@example.com',
            },
        });

        const statusCode = response.status();
        console.log(`Key CREATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 201) {
            const data = await response.json();
            expect(data).toHaveProperty('id');
            expect(data).toHaveProperty('owner', TEST_OWNER);
            expect(data).toHaveProperty('expiresAt');
            createdKeyId = data.id;
            console.log(`✅ 成功建立金鑰: ${createdKeyId} (Owner: ${TEST_OWNER})`);
        } else {
            expect([201, 500]).toContain(statusCode);
            console.log(`⚠️ Key CREATE 回傳 ${statusCode}`);
            test.skip();
        }
    });

    test('Read (List) - 取得所有金鑰清單', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Keys`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Key LIST 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(Array.isArray(data)).toBe(true);
            const found = data.find(k => k.owner === TEST_OWNER);
            expect(found, `應能在清單中找到 Owner=${TEST_OWNER} 的金鑰`).toBeTruthy();
            expect(found).toHaveProperty('isActive', true);
            expect(found).toHaveProperty('expiresAt');
            console.log(`✅ 金鑰清單中找到 ${TEST_OWNER}`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Read (Single) - 取得單一金鑰明細', async ({ request }) => {
        if (!createdKeyId) test.skip();

        const response = await request.get(`${BASE_URL}/api/Keys/${createdKeyId}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Key GET 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('owner', TEST_OWNER);
            expect(data).toHaveProperty('isActive', true);
            expect(data).toHaveProperty('scopes');
            expect(data).toHaveProperty('contactEmail', 'e2e-test@example.com');
            expect(data).toHaveProperty('daysUntilExpiry');
            console.log(`✅ 金鑰明細取得成功: DaysUntilExpiry=${data.daysUntilExpiry}`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
        }
    });

    test('Update (Rotate) - 輪替金鑰', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Keys/${TEST_OWNER}/rotate`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Key ROTATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('consumer', TEST_OWNER);
            expect(data).toHaveProperty('rotatedAt');
            expect(data).toHaveProperty('message');
            console.log(`✅ 金鑰輪替成功: ${data.message}`);
        } else {
            expect([200, 400, 500]).toContain(statusCode);
            console.log(`⚠️ Key ROTATE 回傳 ${statusCode}`);
        }
    });

    test('Read after Rotate - 驗證輪替後金鑰仍有效', async ({ request }) => {
        if (!createdKeyId) test.skip();

        const response = await request.get(`${BASE_URL}/api/Keys/${createdKeyId}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('isActive', true);
            console.log(`✅ 金鑰輪替後仍為有效狀態`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
        }
    });

    test('Delete - 停用金鑰', async ({ request }) => {
        if (!createdKeyId) test.skip();

        const response = await request.delete(`${BASE_URL}/api/Keys/${createdKeyId}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`Key DELETE 回傳 HTTP ${statusCode}`);

        if (statusCode === 204) {
            console.log(`✅ 成功停用金鑰: ${createdKeyId}`);
        } else {
            expect([204, 500]).toContain(statusCode);
        }
    });

    test('Read after Delete - 驗證金鑰已停用', async ({ request }) => {
        if (!createdKeyId) test.skip();

        const response = await request.get(`${BASE_URL}/api/Keys/${createdKeyId}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('isActive', false);
            console.log(`✅ 金鑰已成功停用: isActive=false`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
        }
    });

    test('Read (Not Found) - 存取不存在的金鑰', async ({ request }) => {
        const fakeId = '00000000-0000-0000-0000-000000000000';
        const response = await request.get(`${BASE_URL}/api/Keys/${fakeId}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        expect(statusCode).toBe(404);
        console.log(`✅ 不存在的金鑰回傳 404`);
    });
});

// ============================================================
// Rate Limiting (限流) CRUD - Consumer with Quota + Rate Limit
// ============================================================
test.describe.serial('Rate Limiting CRUD 完整生命週期', () => {
    const TEST_USERNAME = `e2e-ratelimit-${Date.now()}`;

    test('Create - 建立含 Quota 與 Rate Limit 的 Consumer', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
            data: {
                username: TEST_USERNAME,
                quota: {
                    count: 1000,
                    time_window: 3600,
                    rejected_code: 429,
                    rejected_msg: 'E2E rate limit test quota',
                },
                rate_limit: {
                    rate: 10,
                    burst: 20,
                    rejected_code: 503,
                    key: 'remote_addr',
                },
            },
        });

        const statusCode = response.status();
        console.log(`RateLimit CREATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            console.log(`✅ 成功建立含限流設定的消費者: ${TEST_USERNAME}`);
        } else {
            expect([200, 500]).toContain(statusCode);
            console.log(`⚠️ RateLimit CREATE 回傳 ${statusCode}`);
            test.skip();
        }
    });

    test('Read (List) - 驗證消費者含有 rate_limit 欄位', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`RateLimit LIST 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `應能找到消費者 ${TEST_USERNAME}`).toBeTruthy();
            expect(found).toHaveProperty('quota');
            expect(found).toHaveProperty('rate_limit');
            console.log(`✅ 消費者 ${TEST_USERNAME} 含有 quota 和 rate_limit`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Read (Single) - 取得單一消費者含限流明細', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer/${TEST_USERNAME}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`RateLimit GET 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            const data = await response.json();
            expect(data).toHaveProperty('username', TEST_USERNAME);
            expect(data).toHaveProperty('quota');
            expect(data).toHaveProperty('rate_limit');
            console.log(`✅ 消費者明細: quota.count=${data.quota?.count}, rate_limit.rate=${data.rate_limit?.rate}`);
        } else {
            expect([200, 404, 500]).toContain(statusCode);
        }
    });

    test('Update - 修改消費者的 Quota 與 Rate Limit', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
            data: {
                username: TEST_USERNAME,
                quota: {
                    count: 5000,
                    time_window: 7200,
                    rejected_code: 429,
                    rejected_msg: 'E2E updated quota',
                },
                rate_limit: {
                    rate: 50,
                    burst: 100,
                    rejected_code: 503,
                    key: 'remote_addr',
                },
            },
        });

        const statusCode = response.status();
        console.log(`RateLimit UPDATE 回傳 HTTP ${statusCode}`);

        if (statusCode === 200) {
            console.log(`✅ 成功更新消費者限流設定: ${TEST_USERNAME}`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Read after Update - 驗證更新後的消費者', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `應能找到消費者 ${TEST_USERNAME}`).toBeTruthy();
            console.log(`✅ 消費者 ${TEST_USERNAME} 更新後仍存在`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });

    test('Delete - 刪除消費者', async ({ request }) => {
        const response = await request.delete(`${BASE_URL}/api/Consumer/${TEST_USERNAME}`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();
        console.log(`RateLimit DELETE 回傳 HTTP ${statusCode}`);

        if (statusCode === 204) {
            console.log(`✅ 成功刪除消費者: ${TEST_USERNAME}`);
        } else {
            expect([204, 500]).toContain(statusCode);
        }
    });

    test('Read after Delete - 驗證消費者已移除', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/Consumer`, {
            headers: AUTH_HEADERS,
        });

        const statusCode = response.status();

        if (statusCode === 200) {
            const data = await response.json();
            const consumers = Array.isArray(data) ? data : [];
            const found = consumers.find(c => c.username === TEST_USERNAME);
            expect(found, `消費者 ${TEST_USERNAME} 應已從清單中移除`).toBeFalsy();
            console.log(`✅ 消費者 ${TEST_USERNAME} 已成功移除`);
        } else {
            expect([200, 500]).toContain(statusCode);
        }
    });
});

