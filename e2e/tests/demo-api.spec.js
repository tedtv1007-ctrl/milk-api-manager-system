const { test, expect } = require('@playwright/test');

/**
 * MilkDemo API 端點 E2E 測試
 * 驗證 Demo 專案的 API 端點功能，包含認證、商品 CRUD、訂單管理
 *
 * 前置條件：MilkDemo.Api 必須在 http://127.0.0.1:5003 運行中
 */

const DEMO_API_URL = 'http://127.0.0.1:5003';

/** Helper: Login and get JWT token */
async function getToken(request, username = 'admin', password = 'admin') {
    const response = await request.post(`${DEMO_API_URL}/api/auth/login`, {
        data: { username, password }
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    return body.token;
}

test.describe('MilkDemo 認證端點 (Authentication)', () => {

    test('POST /api/auth/login - admin 登入成功', async ({ request }) => {
        const response = await request.post(`${DEMO_API_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'admin' }
        });

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body).toHaveProperty('token');
        expect(body).toHaveProperty('expiresAt');
        expect(body).toHaveProperty('displayName', 'Admin User');
        expect(body.roles).toContain('Admin');
        console.log('✅ Demo Admin 登入成功');
    });

    test('POST /api/auth/login - demo 帳號登入成功', async ({ request }) => {
        const response = await request.post(`${DEMO_API_URL}/api/auth/login`, {
            data: { username: 'demo', password: 'demo' }
        });

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body.displayName).toBe('Demo User');
        expect(body.roles).toContain('Viewer');
        console.log('✅ Demo User 登入成功');
    });

    test('POST /api/auth/login - 錯誤密碼回傳 401', async ({ request }) => {
        const response = await request.post(`${DEMO_API_URL}/api/auth/login`, {
            data: { username: 'admin', password: 'wrong' }
        });

        expect(response.status()).toBe(401);
        console.log('✅ 錯誤密碼正確被拒');
    });

    test('GET /health - 健康檢查端點', async ({ request }) => {
        const response = await request.get(`${DEMO_API_URL}/health`);

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body).toHaveProperty('status', 'Healthy');
        expect(body).toHaveProperty('timestamp');
        console.log('✅ 健康檢查通過');
    });
});

test.describe('MilkDemo 商品 API (Products CRUD)', () => {

    test('GET /api/products - 取得商品列表（含種子資料）', async ({ request }) => {
        const response = await request.get(`${DEMO_API_URL}/api/products`);

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body).toHaveProperty('items');
        expect(body).toHaveProperty('totalCount');
        expect(body.totalCount).toBeGreaterThanOrEqual(10);
        expect(body.items.length).toBeGreaterThan(0);
        console.log(`✅ 取得 ${body.totalCount} 筆商品`);
    });

    test('GET /api/products?category=Dairy - 按分類篩選', async ({ request }) => {
        const response = await request.get(`${DEMO_API_URL}/api/products?category=Dairy`);

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body.items.length).toBeGreaterThan(0);
        for (const item of body.items) {
            expect(item.category).toBe('Dairy');
        }
        console.log(`✅ 按分類篩選：取得 ${body.items.length} 筆 Dairy 商品`);
    });

    test('GET /api/products?page=1&pageSize=3 - 分頁功能', async ({ request }) => {
        const response = await request.get(`${DEMO_API_URL}/api/products?page=1&pageSize=3`);

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body.items.length).toBeLessThanOrEqual(3);
        expect(body).toHaveProperty('page', 1);
        expect(body).toHaveProperty('pageSize', 3);
        console.log(`✅ 分頁功能正常：第 1 頁，共 ${body.items.length} 筆`);
    });

    test('POST /api/products - 新增商品（需要認證）', async ({ request }) => {
        const token = await getToken(request);

        const response = await request.post(`${DEMO_API_URL}/api/products`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: {
                name: 'E2E Test Product',
                description: 'Created by E2E test',
                price: 99.99,
                stockQuantity: 50,
                category: 'Test'
            }
        });

        expect(response.status()).toBe(201);
        const product = await response.json();
        expect(product.name).toBe('E2E Test Product');
        expect(product.price).toBe(99.99);
        expect(product.isActive).toBe(true);
        console.log(`✅ 新增商品成功：ID=${product.id}`);
    });

    test('POST /api/products - 無認證回傳 401', async ({ request }) => {
        const response = await request.post(`${DEMO_API_URL}/api/products`, {
            data: {
                name: 'Unauthorized Product',
                price: 10.00,
                stockQuantity: 1,
                category: 'Test'
            }
        });

        expect(response.status()).toBe(401);
        console.log('✅ 未認證建立商品被正確拒絕');
    });

    test('PUT /api/products/{id} - 更新商品', async ({ request }) => {
        const token = await getToken(request);

        // Create a product first
        const createResp = await request.post(`${DEMO_API_URL}/api/products`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: { name: 'Update Me', description: 'Before update', price: 10.00, stockQuantity: 5, category: 'Test' }
        });
        const created = await createResp.json();

        // Update it
        const updateResp = await request.put(`${DEMO_API_URL}/api/products/${created.id}`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: { name: 'Updated Product', description: 'After update', price: 25.00, stockQuantity: 10, category: 'Test' }
        });

        expect(updateResp.status()).toBe(200);
        const updated = await updateResp.json();
        expect(updated.name).toBe('Updated Product');
        expect(updated.price).toBe(25.00);
        console.log(`✅ 更新商品成功：ID=${updated.id}`);
    });

    test('DELETE /api/products/{id} - 刪除商品', async ({ request }) => {
        const token = await getToken(request);

        // Create, then delete
        const createResp = await request.post(`${DEMO_API_URL}/api/products`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: { name: 'Delete Me', price: 1.00, stockQuantity: 1, category: 'Test' }
        });
        const created = await createResp.json();

        const deleteResp = await request.delete(`${DEMO_API_URL}/api/products/${created.id}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        expect(deleteResp.status()).toBe(204);

        // Verify deleted
        const getResp = await request.get(`${DEMO_API_URL}/api/products/${created.id}`);
        expect(getResp.status()).toBe(404);
        console.log(`✅ 刪除商品成功：ID=${created.id}`);
    });

    test('GET /api/products/categories - 取得分類列表', async ({ request }) => {
        const response = await request.get(`${DEMO_API_URL}/api/products/categories`);

        expect(response.status()).toBe(200);
        const categories = await response.json();
        expect(Array.isArray(categories)).toBeTruthy();
        expect(categories.length).toBeGreaterThan(0);
        expect(categories).toContain('Dairy');
        console.log(`✅ 取得 ${categories.length} 個商品分類`);
    });
});

test.describe('MilkDemo 訂單 API (Orders)', () => {

    test('GET /api/orders - 取得訂單列表（需認證）', async ({ request }) => {
        const token = await getToken(request);

        const response = await request.get(`${DEMO_API_URL}/api/orders`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body).toHaveProperty('items');
        expect(body).toHaveProperty('totalCount');
        expect(body.totalCount).toBeGreaterThanOrEqual(2); // seed data
        console.log(`✅ 取得 ${body.totalCount} 筆訂單`);
    });

    test('POST /api/orders - 建立新訂單', async ({ request }) => {
        const token = await getToken(request);

        // Get first product for ordering
        const productsResp = await request.get(`${DEMO_API_URL}/api/products`);
        const products = await productsResp.json();
        const product = products.items[0];

        const response = await request.post(`${DEMO_API_URL}/api/orders`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: {
                customerName: 'E2E Test Customer',
                customerEmail: 'e2e@test.com',
                customerPhone: '0999999999',
                items: [
                    { productId: product.id, quantity: 2 }
                ]
            }
        });

        expect(response.status()).toBe(201);
        const order = await response.json();
        expect(order.customerName).toBe('E2E Test Customer');
        expect(order.status).toBe('Pending');
        expect(order.items.length).toBe(1);
        expect(order.totalAmount).toBe(product.price * 2);
        console.log(`✅ 建立訂單成功：ID=${order.id}, 金額=${order.totalAmount}`);
    });

    test('GET /api/orders/{id} - 取得訂單詳情', async ({ request }) => {
        const token = await getToken(request);

        // Get first order
        const listResp = await request.get(`${DEMO_API_URL}/api/orders`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const orders = await listResp.json();
        const orderId = orders.items[0].id;

        const response = await request.get(`${DEMO_API_URL}/api/orders/${orderId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        expect(response.status()).toBe(200);
        const order = await response.json();
        expect(order.id).toBe(orderId);
        expect(order).toHaveProperty('items');
        expect(order).toHaveProperty('customerName');
        console.log(`✅ 取得訂單詳情：ID=${order.id}, 客戶=${order.customerName}`);
    });

    test('PUT /api/orders/{id}/cancel - 取消訂單', async ({ request }) => {
        const token = await getToken(request);

        // Create an order to cancel
        const productsResp = await request.get(`${DEMO_API_URL}/api/products`);
        const products = await productsResp.json();

        const createResp = await request.post(`${DEMO_API_URL}/api/orders`, {
            headers: { 'Authorization': `Bearer ${token}` },
            data: {
                customerName: 'Cancel Test',
                customerEmail: 'cancel@test.com',
                items: [{ productId: products.items[0].id, quantity: 1 }]
            }
        });
        const order = await createResp.json();

        // Cancel it
        const cancelResp = await request.put(`${DEMO_API_URL}/api/orders/${order.id}/cancel`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        expect(cancelResp.status()).toBe(200);
        const cancelled = await cancelResp.json();
        expect(cancelled.status).toBe('Cancelled');
        console.log(`✅ 取消訂單成功：ID=${order.id}`);
    });
});

test.describe('MilkDemo Gateway 整合 (API Manager Integration)', () => {

    test('GET /api/gateway/status - 取得閘道狀態', async ({ request }) => {
        const token = await getToken(request);

        const response = await request.get(`${DEMO_API_URL}/api/gateway/status`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        // May return 200 (if API Manager running) or 503 (if not)
        const status = response.status();
        expect([200, 503]).toContain(status);

        if (status === 200) {
            const body = await response.json();
            expect(body).toHaveProperty('isHealthy');
            console.log(`✅ 閘道狀態：healthy=${body.isHealthy}`);
        } else {
            console.log('⚠️ API Manager 未啟動，閘道狀態回傳 503（預期行為）');
        }
    });

    test('GET /api/gateway/routes - 取得路由配置', async ({ request }) => {
        const token = await getToken(request);

        const response = await request.get(`${DEMO_API_URL}/api/gateway/routes`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        const status = response.status();
        expect([200, 503]).toContain(status);
        console.log(`✅ 路由查詢回傳 ${status}`);
    });

    test('GET /api/gateway/audit-logs - 取得稽核日誌', async ({ request }) => {
        const token = await getToken(request);

        const response = await request.get(`${DEMO_API_URL}/api/gateway/audit-logs?count=5`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        const status = response.status();
        expect([200, 503]).toContain(status);
        console.log(`✅ 稽核日誌查詢回傳 ${status}`);
    });
});
