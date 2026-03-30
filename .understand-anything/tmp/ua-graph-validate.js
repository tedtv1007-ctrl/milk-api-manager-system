const fs = require('fs');
const path = require('path');

const inputPath = process.argv[2];
const outputPath = process.argv[3];

if (!inputPath || !outputPath) {
  process.exit(1);
}

try {
  const full = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
  const { nodes, edges, layers, tour } = full;

  const issues = [];
  const warnings = [];
  const stats = {
    totalNodes: nodes.length,
    totalEdges: edges.length,
    totalLayers: Object.keys(layers).length,
    tourSteps: tour.length,
    nodeTypes: {},
    edgeTypes: {}
  };

  const nodeIds = new Set(nodes.map(n => n.id));
  const validNodeTypes = new Set(['file', 'function', 'class', 'module', 'concept', 'config', 'document', 'service', 'table', 'endpoint', 'pipeline', 'schema', 'resource']);
  const validEdgeTypes = new Set(['imports', 'exports', 'contains', 'inherits', 'implements', 'calls', 'subscribes', 'publishes', 'middleware', 'reads_from', 'writes_to', 'transforms', 'validates', 'depends_on', 'tested_by', 'configures', 'related', 'similar_to', 'deploys', 'serves', 'migrates', 'documents', 'provisions', 'routes', 'defines_schema', 'triggers']);

  // Check 1: Nodes
  nodes.forEach((n, idx) => {
    if (!n.id || !n.type || !n.label || !n.summary || !n.tags || !n.complexity) {
      issues.push(`Node at index ${idx} is missing required fields: ${n.id || 'NO_ID'}`);
    }
    if (!validNodeTypes.has(n.type)) issues.push(`Invalid node type '${n.type}' for node ${n.id}`);
    stats.nodeTypes[n.type] = (stats.nodeTypes[n.type] || 0) + 1;
  });

  // Check 2: Edges
  edges.forEach((e, idx) => {
    if (!e.source || !e.target || !e.type) {
      issues.push(`Edge at index ${idx} is missing source, target, or type`);
    } else {
      if (!nodeIds.has(e.source)) issues.push(`Edge index ${idx} references non-existent source: ${e.source}`);
      if (!nodeIds.has(e.target)) issues.push(`Edge index ${idx} references non-existent target: ${e.target}`);
      if (!validEdgeTypes.has(e.type)) issues.push(`Invalid edge type '${e.type}' for edge ${e.source}->${e.target}`);
    }
    stats.edgeTypes[e.type] = (stats.edgeTypes[e.type] || 0) + 1;
  });

  // Check 3: Layers
  Object.keys(layers).forEach(layerName => {
    const layerNodeIds = layers[layerName];
    layerNodeIds.forEach(id => {
      if (!nodeIds.has(id)) issues.push(`Layer '${layerName}' references non-existent node: ${id}`);
    });
  });

  // Check 4: Tour
  tour.forEach((step, idx) => {
    if (!step.nodeIds || step.nodeIds.length === 0) issues.push(`Tour step ${step.order} has no nodeIds`);
    step.nodeIds.forEach(id => {
      if (!nodeIds.has(id)) issues.push(`Tour step ${step.order} references non-existent node: ${id}`);
    });
  });

  // Check 5: Completeness
  if (nodes.length === 0) issues.push("Zero nodes found");
  if (edges.length === 0) issues.push("Zero edges found");
  if (Object.keys(layers).length === 0) issues.push("Zero layers found");
  if (tour.length === 0) issues.push("Zero tour steps found");

  const result = {
    scriptCompleted: true,
    issues,
    warnings,
    stats
  };

  fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
  console.log('Graph validation script completed.');
  process.exit(0);
} catch (err) {
  console.error(err);
  process.exit(1);
}
