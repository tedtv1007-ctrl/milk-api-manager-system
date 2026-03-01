const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const screenshotDir = path.join(__dirname, '..', 'screenshots');

/**
 * Gateway 管理 UI 頁面截圖驗證
 * 驗證 6 個新的 Gateway Control 頁面是否正常載入
 */

const gatewayPages = [
    {
        name: 'gateway-dashboard',
        path: '/gateway',
        title: 'Gateway Dashboard 總覽',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/Gateway Dashboard|Gateway Control|閘道總覽/).first().waitFor({ timeout: 15000 });
        }
    },
    {
        name: 'routes-management',
        path: '/routes-management',
        title: 'Routes 路由管理',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/Route|路由/).first().waitFor({ timeout: 15000 });
        }
    },
    {
        name: 'services-management',
        path: '/services-management',
        title: 'Services 服務管理',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/Service|服務/).first().waitFor({ timeout: 15000 });
        }
    },
    {
        name: 'upstreams-management',
        path: '/upstreams-management',
        title: 'Upstreams 上游管理',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/Upstream|上游/).first().waitFor({ timeout: 15000 });
        }
    },
    {
        name: 'ssl-management',
        path: '/ssl-management',
        title: 'SSL 憑證管理',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/SSL|憑證|Certificate/).first().waitFor({ timeout: 15000 });
        }
    },
    {
        name: 'global-plugins',
        path: '/global-plugins',
        title: 'Global Plugins 全域插件',
        ready: async (page) => {
            await page.waitForLoadState('networkidle', { timeout: 30000 });
            await page.getByText(/Global|Plugin|全域/).first().waitFor({ timeout: 15000 });
        }
    }
];

test.describe('Gateway Control UI 頁面截圖驗證', () => {
    test.beforeAll(() => {
        fs.mkdirSync(screenshotDir, { recursive: true });
    });

    for (const pageDef of gatewayPages) {
        test(`${pageDef.name} 頁面載入與截圖（${pageDef.title}）`, async ({ page }) => {
            test.setTimeout(90000);
            await page.goto(pageDef.path, { waitUntil: 'domcontentloaded', timeout: 30000 });

            // 等待 Blazor SignalR 連線
            await page.waitForTimeout(2000);

            await pageDef.ready(page);

            const screenshotPath = path.join(screenshotDir, `${pageDef.name}.png`);
            await page.screenshot({ path: screenshotPath, fullPage: true });

            // 驗證截圖檔案已建立
            expect(fs.existsSync(screenshotPath), `截圖應存在: ${pageDef.name}.png`).toBe(true);

            // 驗證 URL 正確
            await expect(page).toHaveURL(new RegExp(`${pageDef.path.replace('/', '\\/')}$`));
        });
    }

    test('Gateway Dashboard 顯示統計數字', async ({ page }) => {
        test.setTimeout(90000);
        await page.goto('/gateway', { waitUntil: 'domcontentloaded', timeout: 30000 });
        await page.waitForLoadState('networkidle', { timeout: 30000 });
        await page.waitForTimeout(3000);

        // 驗證 Dashboard 顯示了資源統計卡片
        const statsText = await page.textContent('body');
        // 應該包含這些關鍵字（來自 DashboardStatsDto）
        const keywords = ['Route', 'Service', 'Upstream', 'Consumer', 'SSL', 'Global'];
        for (const keyword of keywords) {
            expect(statsText, `頁面應包含關鍵字: ${keyword}`).toContain(keyword);
        }

        const screenshotPath = path.join(screenshotDir, 'gateway-dashboard-stats.png');
        await page.screenshot({ path: screenshotPath, fullPage: true });
        expect(fs.existsSync(screenshotPath)).toBe(true);
    });

    test('Navigation 包含 Gateway Control 區塊', async ({ page }) => {
        test.setTimeout(60000);
        await page.goto('/gateway', { waitUntil: 'domcontentloaded', timeout: 30000 });
        await page.waitForLoadState('networkidle', { timeout: 30000 });
        await page.waitForTimeout(2000);

        // 驗證導航列包含 GATEWAY CONTROL 區塊
        const navText = await page.textContent('nav') || await page.textContent('.mud-drawer') || await page.textContent('body');

        const navLinks = [
            'Dashboard',
            'Routes',
            'Services',
            'Upstreams',
            'SSL Certificates',
            'Global Plugins'
        ];

        for (const link of navLinks) {
            expect(navText, `導航列應包含: ${link}`).toContain(link);
        }

        const screenshotPath = path.join(screenshotDir, 'gateway-navigation.png');
        await page.screenshot({ path: screenshotPath, fullPage: true });
        expect(fs.existsSync(screenshotPath)).toBe(true);
    });
});
