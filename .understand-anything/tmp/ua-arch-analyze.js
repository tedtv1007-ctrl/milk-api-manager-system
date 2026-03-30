const fs = require('fs');
const path = require('path');

const inputPath = process.argv[2];
const outputPath = process.argv[3];

if (!inputPath || !outputPath) {
  process.exit(1);
}

const input = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
const { fileNodes, importEdges, allEdges } = input;

const directoryGroups = {};
const nodeTypeGroups = {};
const fileStats = {
  totalFileNodes: fileNodes.length,
  filesPerGroup: {},
  nodeTypeCounts: {}
};

function getGroup(filePath) {
  if (!filePath) return 'root';
  const p = filePath.toLowerCase().replace(/\\/g, '/');
  
  if (p.includes('/skills/') || p.includes('.agent') || p.includes('.agents')) return 'skills';
  if (p.includes('backend/milkadminblazor')) return 'ui';
  if (p.includes('backend/milkapimanager.tests')) return 'test';
  if (p.includes('backend/milkapimanager/controllers')) return 'api';
  if (p.includes('backend/milkapimanager/services')) return 'service';
  if (p.includes('backend/milkapimanager/models') || p.includes('backend/milkapimanager/data') || p.includes('backend/milkapimanager/migrations')) return 'data';
  if (p.includes('backend/milkapimanager')) return 'api-core';
  if (p.includes('backend/milkshared') || p.includes('backend/milkworker')) return 'worker';
  if (p.includes('apisix_conf') || p.includes('deploy') || p.includes('monitoring') || p.includes('dashboard_conf')) return 'infrastructure';
  if (p.includes('docs/')) return 'documentation';
  if (p.includes('.github/')) return 'ci-cd';
  
  // Root level files
  if (!p.includes('/')) return 'root';
  
  return 'root';
}

fileNodes.forEach(node => {
  const filePath = node.filePath || '';
  const group = getGroup(filePath);
  
  if (!directoryGroups[group]) directoryGroups[group] = [];
  directoryGroups[group].push(node.id);
  
  if (!nodeTypeGroups[node.type]) nodeTypeGroups[node.type] = [];
  nodeTypeGroups[node.type].push(node.id);
  
  fileStats.filesPerGroup[group] = (fileStats.filesPerGroup[group] || 0) + 1;
  fileStats.nodeTypeCounts[node.type] = (fileStats.nodeTypeCounts[node.type] || 0) + 1;
});

const interGroupImports = [];
const groupImports = {};

importEdges.forEach(edge => {
  const sourceNode = fileNodes.find(n => n.id === edge.source);
  const targetNode = fileNodes.find(n => n.id === edge.target);
  if (sourceNode && targetNode) {
    const sourceGroup = getGroup(sourceNode.filePath);
    const targetGroup = getGroup(targetNode.filePath);
    if (sourceGroup !== targetGroup) {
      const key = `${sourceGroup}->${targetGroup}`;
      groupImports[key] = (groupImports[key] || 0) + 1;
    }
  }
});

for (const key in groupImports) {
  const [from, to] = key.split('->');
  interGroupImports.push({ from, to, count: groupImports[key] });
}

const patternMatches = {};
for (const group in directoryGroups) {
  patternMatches[group] = group; 
}

const infraFiles = fileNodes.filter(n => 
  n.type === 'service' || n.type === 'pipeline' || n.type === 'resource' || 
  (n.filePath && (n.filePath.toLowerCase().includes('dockerfile') || n.filePath.toLowerCase().includes('docker-compose') || n.filePath.toLowerCase().includes('.github')))
).map(n => n.filePath);

const result = {
  scriptCompleted: true,
  directoryGroups,
  nodeTypeGroups,
  interGroupImports,
  patternMatches,
  deploymentTopology: {
    hasDockerfile: infraFiles.some(f => f && f.toLowerCase().includes('dockerfile')),
    hasCompose: infraFiles.some(f => f && f.toLowerCase().includes('docker-compose')),
    hasCI: infraFiles.some(f => f && f.toLowerCase().includes('.github')),
    infraFiles
  },
  fileStats
};

fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
console.log('Architectural analysis script (v4) completed.');
