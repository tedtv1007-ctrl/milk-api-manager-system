const { defineConfig } = require('@playwright/test');

const baseURL = process.env.BASE_URL || 'http://127.0.0.1:5000';

module.exports = defineConfig({
  testDir: './tests',
  timeout: 60000,
  retries: 0,
  use: {
    baseURL,
    headless: true,
    viewport: { width: 1400, height: 900 },
    screenshot: 'on'
  },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'test-report', open: 'never' }]
  ],
  outputDir: 'test-results',
  workers: 1
});
