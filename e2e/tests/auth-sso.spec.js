const { test, expect } = require('@playwright/test');

/**
 * SSO 認證 E2E 測試
 * 驗證 JWT 登入流程、角色存取控制、API Key 認證
 */

const BASE_URL = 'http://localhost:5001';
const API_KEY = 'milk-admin-secret-key-change-me';

test.describe('SSO 認證流程 (Authentication & Authorization)', () => {

    test('POST /api/auth/login - 使用 admin 帳號登入成功', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'admin' }
        });

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body).toHaveProperty('token');
        expect(body).toHaveProperty('expiresAt');
        expect(body).toHaveProperty('displayName', 'admin');
        expect(body.roles).toContain('Admin');
        expect(body.roles).toContain('Operator');
        expect(body.roles).toContain('Viewer');
        console.log('✅ Admin 登入成功，取得 JWT Token');
    });

    test('POST /api/auth/login - 錯誤密碼回傳 401', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'wrong' }
        });

        expect(response.status()).toBe(401);
        const body = await response.json();
        expect(body).toHaveProperty('error');
        console.log('✅ 錯誤密碼正確回傳 401');
    });

    test('POST /api/auth/login - 空白帳號回傳 400', async ({ request }) => {
        const response = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: '', password: '' }
        });

        expect(response.status()).toBe(400);
        console.log('✅ 空白帳號正確回傳 400');
    });

    test('GET /api/auth/me - JWT Token 存取個人資訊', async ({ request }) => {
        // Step 1: Login
        const loginResp = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'admin' }
        });
        const { token } = await loginResp.json();

        // Step 2: Access /me with JWT
        const meResp = await request.get(`${BASE_URL}/api/auth/me`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        expect(meResp.status()).toBe(200);
        const me = await meResp.json();
        expect(me.username).toBe('admin');
        expect(me.isAuthenticated).toBe(true);
        expect(me.roles).toContain('Admin');
        console.log('✅ JWT Token 成功存取 /me 端點');
    });

    test('GET /api/auth/me - 無 Token 回傳 401', async ({ request }) => {
        const response = await request.get(`${BASE_URL}/api/auth/me`);

        expect(response.status()).toBe(401);
        console.log('✅ 無 Token 正確被拒絕');
    });

    test('JWT Token 存取受保護的 Blacklist API', async ({ request }) => {
        // Login as admin
        const loginResp = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'admin' }
        });
        const { token } = await loginResp.json();

        // Access protected endpoint
        const resp = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        const status = resp.status();
        console.log(`Blacklist with JWT: HTTP ${status}`);
        // 200 or 500 (if APISIX offline), but NOT 401/403
        expect([200, 500]).toContain(status);
        console.log('✅ JWT Token 成功存取 Blacklist API');
    });

    test('API Key 認證方式仍然有效', async ({ request }) => {
        const resp = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: { 'X-API-KEY': API_KEY }
        });

        const status = resp.status();
        expect([200, 500]).toContain(status);
        console.log('✅ API Key 認證方式仍然有效');
    });

    test('無認證存取受保護端點回傳 401', async ({ request }) => {
        const resp = await request.get(`${BASE_URL}/api/Blacklist`);

        expect(resp.status()).toBe(401);
        console.log('✅ 無認證正確回傳 401');
    });

    test('Viewer 角色無法存取 Admin 限定的 Blacklist', async ({ request }) => {
        // Login as viewer
        const loginResp = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'viewer', password: 'viewer' }
        });
        const { token } = await loginResp.json();

        // Try to access Admin-only endpoint
        const resp = await request.get(`${BASE_URL}/api/Blacklist`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        // Should be 403 Forbidden (authenticated but not authorized)
        expect(resp.status()).toBe(403);
        console.log('✅ Viewer 角色正確被拒絕存取 Admin 端點');
    });

    test('Operator 角色可以存取 PII Masking 端點', async ({ request }) => {
        // Login as operator
        const loginResp = await request.post(`${BASE_URL}/api/auth/login`, {
            data: { username: 'operator', password: 'operator' }
        });
        const { token } = await loginResp.json();

        const resp = await request.get(`${BASE_URL}/api/PiiMasking`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        const status = resp.status();
        expect([200, 500]).toContain(status);
        console.log('✅ Operator 角色成功存取 PII Masking 端點');
    });

    test('Health 端點不需要認證', async ({ request }) => {
        const resp = await request.get(`${BASE_URL}/health`);

        expect(resp.status()).toBe(200);
        console.log('✅ /health 端點不需認證');
    });

    test('Swagger 端點不需要認證', async ({ request }) => {
        const resp = await request.get(`${BASE_URL}/swagger/v1/swagger.json`);

        expect(resp.status()).toBe(200);
        console.log('✅ Swagger 端點不需認證');
    });
});
