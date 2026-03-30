const fs = require('fs');
const path = require('path');

const dir = 'D:/tedtv_github/milk-api-manager-system/.understand-anything/intermediate';
const batchFiles = fs.readdirSync(dir).filter(f => f.startsWith('batch-') && f.endsWith('.json'));

batchFiles.forEach(file => {
  const filePath = path.join(dir, file);
  let content = fs.readFileSync(filePath, 'utf8');
  
  // Fix the double "from" in batch-2
  content = content.replace(/"from":\s*"from":/g, '"from":');
  
  // Replace "from" and "to" with "source" and "target" for the GraphEdge schema
  content = content.replace(/"from":/g, '"source":');
  content = content.replace(/"to":/g, '"target":');
  
  fs.writeFileSync(filePath, content);
});

console.log('Fixed schema and malformed keys in ' + batchFiles.length + ' batch files.');
