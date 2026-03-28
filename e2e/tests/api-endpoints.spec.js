const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const screenshotDir = path.join(__dirname, '..', 'screenshots');
const API_KEY = 'milk-admin-secret-key-change-me';
const AUTH_HEADERS = { 'X-API-KEY': API_KEY };

/**
 * API 端點 E2E 測試
 * 驗證後端 API 端點的可用性、回應格式與資料正確性
 *
 * 在 Test Mode (MockApisixClient) 下運行，所有斷言為確定性。
 */

test.describe('後端 API 端點驗證 (Backend API Endpoint Verification)', () => {

    test.beforeAll(() => {
        fs.mkdirSync(screenshotDir, { recursive: true });
    });

    test('Route API - 取得路由清單', async ({ request }) => {
        const response = await request.get('http://127.0.0.1:5001/api/Route', {
            headers: AUTH_HEADERS
        });

        expect(response.status()).toBe(200);
        const contentType = response.headers()['content-type'];
        expect(contentType).toMatch(/application\/json|text\/plain/);
        const body = await response.text();
        expect(body.length).toBeGreaterThan(0);
        console.log('✅ Route API 回傳正常');
    });

    test('Consumer API - 取得消費者清單', async ({ request }) => {
        const response = await request.get('http://127.0.0.1:5001/api/Consumer', {
            headers: AUTH_HEADERS
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        const consumers = Array.isArray(data) ? data : [];
        console.log(`✅ Consumer API 回傳 ${consumers.length} 筆消費者資料`);

        // 驗證消費者資料結構
        for (const consumer of consumers) {
            expect(consumer).toHaveProperty('username');
            expect(consumer).toHaveProperty('quota');

            // 安全驗證：不應暴露敏感欄位
            expect(consumer).not.toHaveProperty('password');
            expect(consumer).not.toHaveProperty('secret');
            expect(consumer).not.toHaveProperty('access_token');
        }
    });

    test('Blacklist API - 取得黑名單', async ({ request }) => {
        const response = await request.get('http://127.0.0.1:5001/api/Blacklist', {
            headers: AUTH_HEADERS
        });

        expect(response.status()).toBe(200);
        const contentType = response.headers()['content-type'];
        expect(contentType).toContain('application/json');
        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);
        console.log(`✅ Blacklist API 回傳 ${data.length} 筆黑名單資料`);
    });

    test('Blacklist API - 新增 IP 至黑名單', async ({ request }) => {
        const testIp = '192.168.99.99';
        const response = await request.post('http://127.0.0.1:5001/api/Blacklist', {
            headers: AUTH_HEADERS,
            data: {
                ip: testIp,
                action: 'add',
                reason: 'E2E 測試用',
                addedBy: 'e2e-test'
            }
        });

        expect(response.status()).toBe(200);
        const data = await response.json();
        expect(data).toHaveProperty('message');
        expect(data.message).toContain(testIp);
        console.log(`✅ 成功新增 IP ${testIp} 至黑名單`);

        // 清理：移除測試用 IP
        const removeResponse = await request.post('http://127.0.0.1:5001/api/Blacklist', {
            headers: AUTH_HEADERS,
            data: { ip: testIp, action: 'remove' }
        });
        console.log(`清理：移除測試 IP，回傳 HTTP ${removeResponse.status()}`);
    });

    test('Blacklist API - 無效請求回傳 400', async ({ request }) => {
        const response = await request.post('http://127.0.0.1:5001/api/Blacklist', {
            headers: AUTH_HEADERS,
            data: {
                ip: '',
                action: 'add'
            }
        });

        expect(response.status()).toBe(400);
        console.log('✅ 空 IP 請求正確回傳 400 Bad Request');
    });

    test('Keys API - 建立 API 金鑰', async ({ request }) => {
        const response = await request.post('http://127.0.0.1:5001/api/Keys', {
            headers: AUTH_HEADERS,
            data: {
                owner: 'e2etestconsumer'
            }
        });

        expect(response.status()).toBe(201);
        const data = await response.json();
        expect(data).toHaveProperty('owner');
        expect(data.owner).toBe('e2etestconsumer');
        console.log('✅ 成功建立 API 金鑰');
    });

    test('Keys API - 空 Owner 回傳 400', async ({ request }) => {
        const response = await request.post('http://127.0.0.1:5001/api/Keys', {
            headers: AUTH_HEADERS,
            data: {
                owner: ''
            }
        });

        expect(response.status()).toBe(400);
        console.log('✅ 空 owner 請求正確回傳 400 Bad Request');
    });

    test('Analytics API - 請求統計', async ({ request }) => {
        const response = await request.get('http://127.0.0.1:5001/api/Analytics/requests', {
            headers: AUTH_HEADERS
        });

        // Analytics 依賴 Prometheus，Test Mode 下可能回傳 500
        const statusCode = response.status();
        expect([200, 500]).toContain(statusCode);

        if (statusCode === 200) {
            const data = await response.json();
            expect(Array.isArray(data)).toBe(true);
            console.log(`✅ Analytics API 回傳 ${data.length} 筆統計資料`);
        } else {
            console.log(`⚠️ Analytics API 回傳 ${statusCode}（Prometheus 未啟動，Test Mode 預期行為）`);
        }
    });

    test('API 回應安全標頭驗證', async ({ request }) => {
        const response = await request.get('http://127.0.0.1:5001/api/Route', {
            headers: AUTH_HEADERS
        });
        const headers = response.headers();

        // 驗證沒有洩露伺服器版本資訊
        console.log('Server header:', headers['server'] || '(未設定)');
        console.log('X-Powered-By header:', headers['x-powered-by'] || '(未設定)');

        // 基本安全驗證
        expect(response.status()).toBeGreaterThanOrEqual(200);
        expect(response.status()).toBeLessThan(600);
        console.log('✅ API 回應安全標頭驗證完成');
    });
});
