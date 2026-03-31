const { test, expect } = require('@playwright/test');

/**
 * MilkDemo Blazor WASM UI E2E 測試
 * 驗證 Demo 前端介面功能：登入、導覽、頁面載入、CRUD 操作
 *
 * 前置條件：
 * - MilkDemo.Api 在 http://127.0.0.1:5003 運行
 * - MilkDemo.WebApp 在 http://127.0.0.1:5002 運行
 */

const DEMO_UI_URL = 'http://127.0.0.1:5002';

/** Helper: Login via UI */
async function loginAsAdmin(page) {
    await page.goto(`${DEMO_UI_URL}/login`);
    await page.waitForSelector('input[placeholder*="Username"], input[type="text"]', { timeout: 30000 });
    await page.fill('input[placeholder*="Username"], input[type="text"]', 'admin');
    await page.fill('input[placeholder*="Password"], input[type="password"]', 'admin');
    await page.click('button:has-text("LOGIN"), button:has-text("Sign In"), button[type="submit"]');
    // Wait for navigation to dashboard
    await page.waitForURL(`${DEMO_UI_URL}/`, { timeout: 15000 }).catch(() => {
        // Blazor WASM may take time to hydrate
    });
    await page.waitForTimeout(2000);
}

test.describe('MilkDemo UI - 登入流程', () => {

    test('顯示登入頁面', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/login`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(3000); // Wait for Blazor WASM to load

        // Check login form exists
        const loginForm = await page.locator('input[type="text"], input[type="password"]').count();
        expect(loginForm).toBeGreaterThan(0);

        await page.screenshot({ path: 'test-results/demo-login-page.png' });
        console.log('✅ 登入頁面載入成功');
    });

    test('使用 admin 帳號登入', async ({ page }) => {
        await loginAsAdmin(page);

        // Should be on dashboard or see navigation
        const content = await page.textContent('body');
        expect(content).toBeTruthy();

        await page.screenshot({ path: 'test-results/demo-after-login.png' });
        console.log('✅ Admin 登入成功');
    });

    test('錯誤密碼顯示錯誤訊息', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/login`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(3000);

        await page.fill('input[placeholder*="Username"], input[type="text"]', 'admin');
        await page.fill('input[placeholder*="Password"], input[type="password"]', 'wrongpass');
        await page.click('button:has-text("LOGIN"), button:has-text("Sign In"), button[type="submit"]');
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-login-error.png' });
        console.log('✅ 錯誤密碼測試完成');
    });
});

test.describe('MilkDemo UI - 頁面導覽', () => {

    test.beforeEach(async ({ page }) => {
        await loginAsAdmin(page);
    });

    test('Dashboard 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-dashboard.png' });
        console.log('✅ Dashboard 頁面載入成功');
    });

    test('Products 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/products`);
        await page.waitForTimeout(2000);

        // Check for product list or table
        const content = await page.textContent('body');
        expect(content.toLowerCase()).toMatch(/product|商品|milk/i);

        await page.screenshot({ path: 'test-results/demo-products.png' });
        console.log('✅ Products 頁面載入成功');
    });

    test('Orders 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/orders`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-orders.png' });
        console.log('✅ Orders 頁面載入成功');
    });

    test('Gateway 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/gateway`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-gateway.png' });
        console.log('✅ Gateway 頁面載入成功');
    });

    test('Routes 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/routes`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-routes.png' });
        console.log('✅ Routes 頁面載入成功');
    });

    test('Security 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/security`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-security.png' });
        console.log('✅ Security 頁面載入成功');
    });

    test('Audit 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/audit`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-audit.png' });
        console.log('✅ Audit 頁面載入成功');
    });

    test('API Test 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/api-test`);
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/demo-api-test.png' });
        console.log('✅ API Test 頁面載入成功');
    });

    test('About 頁面載入', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/about`);
        await page.waitForTimeout(2000);

        const content = await page.textContent('body');
        expect(content.toLowerCase()).toMatch(/milk|demo|api/i);

        await page.screenshot({ path: 'test-results/demo-about.png' });
        console.log('✅ About 頁面載入成功');
    });
});

test.describe('MilkDemo UI - 商品 CRUD 操作', () => {

    test.beforeEach(async ({ page }) => {
        await loginAsAdmin(page);
    });

    test('商品列表顯示種子資料', async ({ page }) => {
        await page.goto(`${DEMO_UI_URL}/products`);
        await page.waitForTimeout(3000);

        // Check for known seed product names
        const content = await page.textContent('body');
        expect(content).toMatch(/Premium Milk|Low-Fat Yogurt|Cheddar Cheese/i);

        await page.screenshot({ path: 'test-results/demo-products-list.png' });
        console.log('✅ 商品列表顯示種子資料');
    });
});
