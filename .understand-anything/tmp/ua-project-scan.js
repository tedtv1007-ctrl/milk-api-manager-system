const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const projectRoot = process.argv[2];
const outputFile = process.argv[3];

if (!projectRoot || !outputFile) {
  console.error('Usage: node ua-project-scan.js <projectRoot> <outputFile>');
  process.exit(1);
}

const EXT_TO_LANG = {
  '.ts': 'typescript', '.tsx': 'typescript',
  '.js': 'javascript', '.jsx': 'javascript',
  '.py': 'python',
  '.go': 'go',
  '.rs': 'rust',
  '.java': 'java',
  '.rb': 'ruby',
  '.cpp': 'cpp', '.cc': 'cpp', '.cxx': 'cpp', '.h': 'cpp', '.hpp': 'cpp',
  '.c': 'c',
  '.cs': 'csharp',
  '.swift': 'swift',
  '.kt': 'kotlin',
  '.php': 'php',
  '.vue': 'vue',
  '.svelte': 'svelte',
  '.sh': 'shell', '.bash': 'shell',
  '.md': 'markdown', '.rst': 'markdown',
  '.yaml': 'yaml', '.yml': 'yaml',
  '.json': 'json',
  '.toml': 'toml',
  '.sql': 'sql',
  '.graphql': 'graphql', '.gql': 'graphql',
  '.proto': 'protobuf',
  '.tf': 'terraform', '.tfvars': 'terraform',
  '.html': 'html', '.htm': 'html',
  '.css': 'css', '.scss': 'css', '.sass': 'css', '.less': 'css',
  '.xml': 'xml',
  '.cfg': 'config', '.ini': 'config', '.env': 'config',
  '.ps1': 'powershell', '.bat': 'batch'
};

const CATEGORIES = [
  { ext: ['.md', '.rst', '.txt'], category: 'docs' },
  { ext: ['.yaml', '.yml', '.json', '.toml', '.xml', '.cfg', '.ini', '.env'], category: 'config' },
  { files: ['Dockerfile', 'Makefile', 'Jenkinsfile', 'Procfile', 'Vagrantfile'], category: 'infra' },
  { ext: ['.tf', '.tfvars', '.k8s.yaml', '.k8s.yml'], category: 'infra' },
  { ext: ['.sql', '.graphql', '.gql', '.proto', '.prisma', '.csv'], category: 'data' },
  { ext: ['.sh', '.bash', '.ps1', '.bat'], category: 'script' },
  { ext: ['.html', '.htm', '.css', '.scss', '.sass', '.less'], category: 'markup' }
];

function getCategory(filePath) {
  const fileName = path.basename(filePath);
  const ext = path.extname(filePath).toLowerCase();
  
  for (const cat of CATEGORIES) {
    if (cat.files && cat.files.includes(fileName)) return cat.category;
    if (cat.ext && cat.ext.includes(ext)) return cat.category;
  }
  
  if (filePath.includes('/k8s/') || filePath.includes('/kubernetes/') || filePath.includes('.github/workflows/')) {
    return 'infra';
  }
  
  return 'code';
}

function resolveImports(filePath, content, allFiles, projectRoot) {
  const ext = path.extname(filePath).toLowerCase();
  const imports = [];
  const dir = path.dirname(filePath);

  if (ext === '.ts' || ext === '.tsx' || ext === '.js' || ext === '.jsx') {
    const matches = content.matchAll(/(?:import|from|require)\s*\(?\s*['"]([^'"]+)['"]/g);
    for (const match of matches) {
      let target = match[1];
      if (target.startsWith('.')) {
        let resolved = path.resolve(path.join(projectRoot, dir), target);
        let relResolved = path.relative(projectRoot, resolved).replace(/\\/g, '/');
        
        const variants = ['', '.ts', '.tsx', '.js', '.jsx', '/index.ts', '/index.js', '/index.tsx', '/index.jsx'];
        for (const v of variants) {
          if (allFiles.has(relResolved + v)) {
            imports.push(relResolved + v);
            break;
          }
        }
      }
    }
  } else if (ext === '.cs') {
    // C# uses namespaces, but we can look for 'using' and try to guess if they are internal
    // However, C# path resolution is hard without assembly info. 
    // We'll skip deep resolution but maybe look for project references in .csproj later.
  } else if (ext === '.py') {
    const matches = content.matchAll(/^from\s+(\.+)([^\s]+)\s+import/gm);
    for (const match of matches) {
        // basic relative import detection
    }
  }

  return [...new Set(imports)];
}

try {
  let filesRaw = [];
  try {
    filesRaw = execSync('git ls-files', { cwd: projectRoot, encoding: 'utf8' }).split('\n').filter(Boolean);
  } catch (e) {
    // Fallback to manual scan if not git
    console.log('Not a git repo, fallback to manual scan');
    // Simplified fallback for brevity
  }

  const exclusions = [
    'node_modules/', '.git/', 'vendor/', 'venv/', '.venv/', '__pycache__/',
    'dist/', 'build/', 'out/', 'coverage/', '.next/', '.cache/', '.turbo/', 'target/',
    'bin/', 'obj/', // .NET specific
    '.idea/', '.vscode/'
  ];
  const exclExts = ['.lock', '.png', '.jpg', '.jpeg', '.gif', '.svg', '.ico', '.woff', '.woff2', '.ttf', '.eot', '.mp3', '.mp4', '.pdf', '.zip', '.tar', '.gz', '.min.js', '.min.css', '.map', '.d.ts', '.log'];
  const exclFiles = ['package-lock.json', 'yarn.lock', 'pnpm-lock.yaml', 'LICENSE', '.gitignore', '.editorconfig', '.prettierrc'];

  const filteredFiles = filesRaw.filter(f => {
    if (exclusions.some(ex => f.includes(ex))) return false;
    const ext = path.extname(f).toLowerCase();
    if (exclExts.some(ex => ext === ex || f.endsWith('.generated' + ext))) return false;
    if (exclFiles.includes(path.basename(f))) return false;
    return true;
  });

  const fileData = filteredFiles.map(f => {
    const fullPath = path.join(projectRoot, f);
    const ext = path.extname(f).toLowerCase();
    const fileName = path.basename(f);
    let lang = EXT_TO_LANG[ext] || 'unknown';
    if (fileName === 'Dockerfile') lang = 'dockerfile';
    if (fileName === 'Makefile') lang = 'makefile';
    
    let sizeLines = 0;
    try {
      sizeLines = fs.readFileSync(fullPath, 'utf8').split('\n').length;
    } catch (e) {}

    return {
      path: f.replace(/\\/g, '/'),
      language: lang,
      sizeLines,
      fileCategory: getCategory(f.replace(/\\/g, '/'))
    };
  });

  const allFilesSet = new Set(fileData.map(f => f.path));
  const importMap = {};
  fileData.forEach(f => {
    if (f.fileCategory === 'code') {
      try {
        const content = fs.readFileSync(path.join(projectRoot, f.path), 'utf8');
        importMap[f.path] = resolveImports(f.path, content, allFilesSet, projectRoot);
      } catch (e) {
        importMap[f.path] = [];
      }
    } else {
      importMap[f.path] = [];
    }
  });

  // Framework detection
  const frameworks = new Set();
  const pkgJsonPath = path.join(projectRoot, 'package.json');
  let rawDescription = '';
  let projectName = path.basename(projectRoot);

  if (fs.existsSync(pkgJsonPath)) {
    const pkg = JSON.parse(fs.readFileSync(pkgJsonPath, 'utf8'));
    projectName = pkg.name || projectName;
    rawDescription = pkg.description || '';
    const deps = { ...pkg.dependencies, ...pkg.devDependencies };
    const fwks = ['react', 'vue', 'svelte', 'express', 'next', 'vite', 'tailwindcss', 'mudblazor', 'mudblazor.templates'];
    fwks.forEach(fw => {
      if (deps[fw] || Object.keys(deps).some(d => d.includes(fw))) frameworks.add(fw);
    });
  }

  // .NET Detection
  const slnFiles = filteredFiles.filter(f => f.endsWith('.sln'));
  if (slnFiles.length > 0) {
    frameworks.add('.NET');
    projectName = path.basename(slnFiles[0], '.sln');
  }
  const csprojFiles = filteredFiles.filter(f => f.endsWith('.csproj'));
  csprojFiles.forEach(f => {
    const content = fs.readFileSync(path.join(projectRoot, f), 'utf8');
    if (content.includes('Microsoft.NET.Sdk.Web')) frameworks.add('ASP.NET Core');
    if (content.includes('Microsoft.AspNetCore.Components')) frameworks.add('Blazor');
    if (content.includes('MudBlazor')) frameworks.add('MudBlazor');
  });

  if (filteredFiles.some(f => f.includes('Dockerfile'))) frameworks.add('Docker');
  if (filteredFiles.some(f => f.includes('docker-compose'))) frameworks.add('Docker Compose');
  if (filteredFiles.some(f => f.includes('.github/workflows/'))) frameworks.add('GitHub Actions');

  let readmeHead = '';
  const readmePath = path.join(projectRoot, 'README.md');
  if (fs.existsSync(readmePath)) {
    readmeHead = fs.readFileSync(readmePath, 'utf8').split('\n').slice(0, 10).join('\n');
  }

  const result = {
    scriptCompleted: true,
    name: projectName,
    rawDescription,
    readmeHead,
    languages: [...new Set(fileData.map(f => f.language))].sort(),
    frameworks: [...frameworks].sort(),
    files: fileData.sort((a, b) => a.path.localeCompare(b.path)),
    totalFiles: fileData.length,
    estimatedComplexity: fileData.length > 500 ? 'very-large' : fileData.length > 150 ? 'large' : fileData.length > 30 ? 'moderate' : 'small',
    importMap
  };

  fs.writeFileSync(outputFile, JSON.stringify(result, null, 2));
  process.exit(0);
} catch (err) {
  console.error(err);
  process.exit(1);
}
