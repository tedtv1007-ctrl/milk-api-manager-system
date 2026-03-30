const fs = require('fs');
const path = require('path');

const intermediateDir = '.understand-anything/intermediate';
const batchFiles = fs.readdirSync(intermediateDir).filter(f => f.startsWith('batch-') && f.endsWith('.json'));

let allNodes = [];
let allEdges = [];

batchFiles.forEach(f => {
  const data = JSON.parse(fs.readFileSync(path.join(intermediateDir, f), 'utf8'));
  if (data.nodes) allNodes = allNodes.concat(data.nodes);
  if (data.edges) allEdges = allEdges.concat(data.edges);
});

// Deduplicate nodes (keep last)
const nodeMap = new Map();
allNodes.forEach(n => {
  nodeMap.set(n.id, n);
});
const mergedNodes = Array.from(nodeMap.values());

// Deduplicate edges
const edgeMap = new Map();
allEdges.forEach(e => {
  const key = `${e.source}|${e.target}|${e.type}`;
  edgeMap.set(key, e);
});
let mergedEdges = Array.from(edgeMap.values());

// Remove dangling edges
const nodeIds = new Set(mergedNodes.map(n => n.id));
mergedEdges = mergedEdges.filter(e => {
  return nodeIds.has(e.source) && nodeIds.has(e.target);
});

const assembled = {
  nodes: mergedNodes,
  edges: mergedEdges
};

fs.writeFileSync(path.join(intermediateDir, 'assembled-raw.json'), JSON.stringify(assembled, null, 2));
console.log(`Assembled ${mergedNodes.length} nodes and ${mergedEdges.length} edges.`);
