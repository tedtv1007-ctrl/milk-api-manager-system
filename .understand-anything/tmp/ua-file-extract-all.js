const fs = require('fs');
const path = require('path');

const batchIndex = process.argv[2];
if (batchIndex === undefined) {
    console.error("Please provide batch index");
    process.exit(1);
}

const inputPath = `.understand-anything/tmp/ua-file-analyzer-input-${batchIndex}.json`;
const outputPath = `.understand-anything/tmp/ua-file-extract-results-${batchIndex}.json`;

if (!fs.existsSync(inputPath)) {
    console.error(`Input file not found: ${inputPath}`);
    process.exit(1);
}

const inputData = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
const projectRoot = inputData.projectRoot;
const results = [];

for (const fileInfo of inputData.batchFiles) {
    const filePath = path.isAbsolute(fileInfo.path) ? fileInfo.path : path.join(projectRoot, fileInfo.path);
    if (!fs.existsSync(filePath)) {
        results.push({ path: fileInfo.path, error: "File not found at " + filePath });
        continue;
    }

    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    const fileResult = {
        path: fileInfo.path,
        category: fileInfo.fileCategory,
        language: fileInfo.language,
        lineCount: lines.length,
        extracted: {}
    };

    try {
        if (fileInfo.fileCategory === 'code') {
            // Classes/Interfaces/Structs
            const classMatches = content.matchAll(/(?:class|interface|struct|enum|record)\s+([a-zA-Z0-9_]+)/g);
            fileResult.extracted.classes = [...new Set([...classMatches].map(m => m[1]))];

            // Functions/Methods
            // JS/TS: function name(), name = () =>, name() {
            // C#: ReturnType Name(Args) {
            let functions = [];
            if (fileInfo.language === 'typescript' || fileInfo.language === 'javascript') {
                const f1 = content.matchAll(/function\s+([a-zA-Z0-9_]+)\s*\(/g);
                const f2 = content.matchAll(/(?:const|let|var)\s+([a-zA-Z0-9_]+)\s*=\s*(?:async\s*)?\([^)]*\)\s*=>/g);
                const f3 = content.matchAll(/^\s*(?:async\s+)?([a-zA-Z0-9_]+)\s*\([^)]*\)\s*\{/gm);
                functions = [...f1, ...f2, ...f3].map(m => m[1]);
            } else if (fileInfo.language === 'csharp') {
                const f1 = content.matchAll(/(?:public|private|protected|internal|static|async|virtual|override|new)\s+[a-zA-Z0-9_<>[\]]+\s+([a-zA-Z0-9_]+)\s*\(/g);
                functions = [...f1].map(m => m[1]);
            }
            fileResult.extracted.functions = [...new Set(functions.filter(f => !['if', 'for', 'while', 'switch', 'catch', 'using', 'lock', 'return'].includes(f)))];

            // Exports (JS/TS)
            if (fileInfo.language === 'typescript' || fileInfo.language === 'javascript') {
                const exportMatches = content.matchAll(/export\s+(?:const|let|var|function|class|interface|type|enum|default)\s+([a-zA-Z0-9_]+)/g);
                fileResult.extracted.exports = [...new Set([...exportMatches].map(m => m[1]))];
            }

            // Imports/Usings
            const imports = [];
            if (fileInfo.language === 'typescript' || fileInfo.language === 'javascript') {
                const impMatches = content.matchAll(/import\s+.*\s+from\s+['"](.*)['"]/g);
                for (const m of impMatches) imports.push(m[1]);
                const reqMatches = content.matchAll(/require\(['"](.*)['"]\)/g);
                for (const m of reqMatches) imports.push(m[1]);
            } else if (fileInfo.language === 'csharp') {
                const usingMatches = content.matchAll(/using\s+([a-zA-Z0-9_.]+);/g);
                for (const m of usingMatches) imports.push(m[1]);
            }
            fileResult.extracted.imports = [...new Set(imports)];
            
            if (fileInfo.language === 'csharp') {
                const nsMatch = content.match(/namespace\s+([a-zA-Z0-9_.]+)/);
                if (nsMatch) fileResult.extracted.namespace = nsMatch[1];
            }
        } else if (fileInfo.fileCategory === 'docs') {
            const headings = lines.filter(l => l.startsWith('#')).map(l => l.replace(/^#+\s*/, '').trim());
            fileResult.extracted.headings = headings;
        } else if (fileInfo.fileCategory === 'infra') {
            if (fileInfo.language === 'dockerfile') {
                const stages = lines.filter(l => l.toUpperCase().startsWith('FROM')).map(l => {
                    const parts = l.split(/\s+/);
                    const asIndex = parts.findIndex(p => p.toUpperCase() === 'AS');
                    return asIndex !== -1 ? parts[asIndex + 1] : parts[1];
                });
                fileResult.extracted.stages = stages;
            } else if (fileInfo.language === 'yaml' || fileInfo.language === 'yml') {
                const servicesMatch = content.match(/^\s*services:\s*\n((\s+.*\n)+)/m);
                if (servicesMatch) {
                    const services = servicesMatch[1].split('\n')
                        .filter(l => /^\s{2,4}[a-zA-Z0-9_-]+:/.test(l))
                        .map(l => l.trim().replace(':', ''));
                    fileResult.extracted.services = services;
                }
            }
        } else if (fileInfo.fileCategory === 'config') {
            if (fileInfo.language === 'json') {
                try {
                    const json = JSON.parse(content);
                    fileResult.extracted.keys = Object.keys(json);
                } catch (e) {}
            } else if (fileInfo.language === 'yaml' || fileInfo.language === 'yml') {
                const keys = lines.filter(l => /^[a-zA-Z0-9_-]+:/.test(l)).map(l => l.split(':')[0].trim());
                fileResult.extracted.keys = keys;
            }
        } else if (fileInfo.fileCategory === 'data') {
            if (fileInfo.language === 'sql') {
                const tableMatches = content.matchAll(/CREATE\s+TABLE\s+([a-zA-Z0-9_."`[\]]+)/gi);
                fileResult.extracted.tables = [...new Set([...tableMatches].map(m => m[1]))];
            }
        }
    } catch (err) {
        fileResult.error = err.message;
    }

    results.push(fileResult);
}

fs.writeFileSync(outputPath, JSON.stringify({ scriptCompleted: true, results }, null, 2));
console.log(`Results written to ${outputPath}`);
