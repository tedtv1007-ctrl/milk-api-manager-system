const fs = require('fs');
const path = require('path');

const inputPath = process.argv[2];
const outputPath = process.argv[3];

if (!inputPath || !outputPath) {
  process.exit(1);
}

const input = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
const { nodes, edges, layers } = input;

const nodeMap = new Map();
nodes.forEach(n => nodeMap.set(n.id, n));

const adj = new Map();
const revAdj = new Map();
nodes.forEach(n => {
  adj.set(n.id, []);
  revAdj.set(n.id, []);
});

edges.forEach(e => {
  if (adj.has(e.source) && adj.has(e.target)) {
    adj.get(e.source).push(e.target);
    revAdj.get(e.target).push(e.source);
  }
});

// A. Fan-In Ranking
const fanInRanking = nodes.map(n => ({
  id: n.id,
  fanIn: revAdj.get(n.id).length,
  name: n.label || n.name || n.id.split(':').pop()
})).sort((a, b) => b.fanIn - a.fanIn).slice(0, 20);

// B. Fan-Out Ranking
const fanOutRanking = nodes.map(n => ({
  id: n.id,
  fanOut: adj.get(n.id).length,
  name: n.label || n.name || n.id.split(':').pop()
})).sort((a, b) => b.fanOut - a.fanOut).slice(0, 20);

// C. Entry Point Candidates
const entryPointCandidates = nodes.map(n => {
  let score = 0;
  const id = n.id.toLowerCase();
  const name = (n.label || n.name || '').toLowerCase();
  
  if (id.includes('readme.md')) score += 5;
  if (name.includes('program.cs') || name.includes('main.ts') || name.includes('index.ts') || name.includes('app.razor')) score += 3;
  
  const fanIn = revAdj.get(n.id).length;
  const fanOut = adj.get(n.id).length;
  if (fanIn <= 1) score += 1;
  if (fanOut >= 5) score += 1;
  
  return { id: n.id, score, name: n.label || n.name, summary: n.summary };
}).sort((a, b) => b.score - a.score).slice(0, 10);

// D. BFS Traversal
const topEntry = entryPointCandidates[0]?.id;
const bfsTraversal = { startNode: topEntry, order: [], depthMap: {}, byDepth: {} };

if (topEntry) {
  const queue = [[topEntry, 0]];
  const visited = new Set([topEntry]);
  
  while (queue.length > 0) {
    const [u, d] = queue.shift();
    bfsTraversal.order.push(u);
    bfsTraversal.depthMap[u] = d;
    if (!bfsTraversal.byDepth[d]) bfsTraversal.byDepth[d] = [];
    bfsTraversal.byDepth[d].push(u);
    
    if (d < 5) { // Limit depth
      (adj.get(u) || []).forEach(v => {
        if (!visited.has(v)) {
          visited.add(v);
          queue.push([v, d + 1]);
        }
      });
    }
  }
}

// E. Non-Code File Inventory
const nonCodeFiles = { documentation: [], infrastructure: [], data: [], config: [] };
nodes.forEach(n => {
  const type = (n.type || '').toLowerCase();
  const id = n.id.toLowerCase();
  const item = { id: n.id, name: n.label || n.name, summary: n.summary };
  
  if (type === 'document' || id.includes('.md')) nonCodeFiles.documentation.push(item);
  else if (type === 'service' || type === 'pipeline' || type === 'resource' || id.includes('dockerfile') || id.includes('yml')) nonCodeFiles.infrastructure.push(item);
  else if (type === 'table' || type === 'schema' || type === 'endpoint') nonCodeFiles.data.push(item);
  else if (type === 'config' || id.includes('.json')) nonCodeFiles.config.push(item);
});

// F. Clusters
const clusters = [];
const processedPairs = new Set();
edges.forEach(e => {
  const pair = [e.source, e.target].sort().join('|');
  if (processedPairs.has(pair)) return;
  processedPairs.add(pair);
  
  // Simple bidirectional or strong link check
  const backEdge = edges.find(be => be.source === e.target && be.target === e.source);
  if (backEdge) {
    clusters.push({ nodes: [e.source, e.target], edgeCount: 2 });
  }
});

// G. Layer List
const layersResult = { count: (layers || []).length, list: layers || [] };

// H. Node Summary Index
const nodeSummaryIndex = {};
nodes.forEach(n => {
  nodeSummaryIndex[n.id] = { name: n.label || n.name, type: n.type, summary: n.summary };
});

const result = {
  scriptCompleted: true,
  entryPointCandidates,
  fanInRanking,
  fanOutRanking,
  bfsTraversal,
  nonCodeFiles,
  clusters: clusters.slice(0, 10),
  layers: layersResult,
  nodeSummaryIndex,
  totalNodes: nodes.length,
  totalEdges: edges.length
};

fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
console.log('Tour analysis script completed.');
