const fs = require('fs');
const path = require('path');
const data = JSON.parse(fs.readFileSync('.understand-anything/intermediate/scan-result.json', 'utf8'));
const files = data.files;
const importMap = data.importMap || {};
const batchSize = 25;
const projectRoot = 'D:/tedtv_github/milk-api-manager-system';

for (let i = 0; i < files.length; i += batchSize) {
  const batchFiles = files.slice(i, i + batchSize);
  const batchIndex = Math.floor(i / batchSize);
  const batchImportData = {};
  batchFiles.forEach(f => {
    batchImportData[f.path] = importMap[f.path] || [];
  });
  const input = {
    projectRoot,
    batchFiles,
    batchImportData
  };
  fs.writeFileSync(`.understand-anything/tmp/ua-file-analyzer-input-${batchIndex}.json`, JSON.stringify(input, null, 2));
}
console.log('Created ' + Math.ceil(files.length / batchSize) + ' batch input files.');
