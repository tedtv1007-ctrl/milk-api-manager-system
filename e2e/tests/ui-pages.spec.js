const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

const screenshotDir = path.join(__dirname, '..', 'screenshots');

const pages = [
  {
    name: 'api-list',
    path: '/apis',
    title: 'API 治理與盤點',
    ready: async (page) => {
      // 等待 Blazor 連線完成 + 內容載入
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.waitForTimeout(2000);
    }
  },
  {
    name: 'api-inventory',
    path: '/api-inventory',
    title: 'API 合規盤點',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.waitForTimeout(2000);
    }
  },
  {
    name: 'consumers',
    path: '/consumers',
    title: '消費者管理',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.getByText(/消費者權限管理|Consumers/).first().waitFor({ timeout: 15000 });
    }
  },
  {
    name: 'blacklist',
    path: '/blacklist',
    title: 'IP 黑名單管理',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.waitForTimeout(2000);
    }
  },
  {
    name: 'consumer-analytics',
    path: '/consumer-analytics',
    title: '消費者統計分析',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.waitForTimeout(2000);
    }
  },
  {
    name: 'reports',
    path: '/reports',
    title: '統計報表',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.getByText(/Consumer Stats|統計報表/).first().waitFor({ timeout: 15000 });
    }
  },
  {
    name: 'sync-status',
    path: '/sync-status',
    title: '群組同步狀態',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.getByText(/Group Sync Status|群組同步/).first().waitFor({ timeout: 15000 });
    }
  }
];

test.describe('Milk Admin UI 頁面截圖驗證', () => {
  test.beforeAll(() => {
    fs.mkdirSync(screenshotDir, { recursive: true });
  });

  for (const pageDef of pages) {
    test(`${pageDef.name} 頁面載入與截圖（${pageDef.title}）`, async ({ page }) => {
      test.setTimeout(90000); // 增加超時時間以容納 Blazor 首次載入
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

  test('Load Testing 頁面載入與執行（壓力測試中心）', async ({ page }) => {
    test.setTimeout(65000); // 壓測會跑比較久
    await page.goto('/load-testing', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(2000);
    await page.waitForLoadState('networkidle', { timeout: 30000 });
    await page.getByText(/Stress Test Center/i).first().waitFor({ timeout: 15000 });

    const startTestBtn = page.getByRole('button', { name: /START STRESS TEST/i });
    await startTestBtn.click();

    await page.getByText(/Executing stress test against/i).waitFor({ timeout: 5000 });
    await page.getByText(/Error: Server|http_reqs|k6 execution failed/).first().waitFor({ timeout: 45000 });

    const screenshotPath = path.join(screenshotDir, 'load-testing-result.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    expect(fs.existsSync(screenshotPath)).toBe(true);
  });
});
