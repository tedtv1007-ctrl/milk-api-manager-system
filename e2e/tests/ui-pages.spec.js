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
      await page.getByText(/API Governance/).first().waitFor({ timeout: 15000 });
    }
  },
  {
    name: 'api-inventory',
    path: '/api-inventory',
    title: 'API 合規盤點',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.locator('.alert.alert-info').first().waitFor({ timeout: 15000 });
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
      await page.getByText(/IP Blacklist Management/).first().waitFor({ timeout: 15000 });
    }
  },
  {
    name: 'consumer-analytics',
    path: '/consumer-analytics',
    title: '消費者統計分析',
    ready: async (page) => {
      await page.waitForLoadState('networkidle', { timeout: 30000 });
      await page.getByText(/Consumer Statistics|消費者統計/).first().waitFor({ timeout: 15000 });
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
});
