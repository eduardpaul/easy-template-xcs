// Benchmark orchestrator: easy-template-x (JavaScript) vs Easy.Template.XCS
// compiled to WebAssembly, both running in Node.js. A native .NET run is
// included as a reference when the .NET SDK is available.
//
// Every engine runs in its own fresh process so JIT state, GC heaps and
// caches never leak from one engine into another.
//
//   node bench.mjs [--engines js,wasm-interp,wasm-aot,dotnet-native] [--scenarios a,b]
//                  [--warmup-ms 1000] [--measure-ms 3000] [--cold-runs 5] [--out results]

import { execFileSync, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { engineNames, wasmBundleDir } from './lib/engines.mjs';
import { findScenarios } from './lib/scenarios.mjs';

const benchDir = import.meta.dirname;
const repoDir = path.resolve(benchDir, '..');
const fixturesDir = path.join(repoDir, 'src', 'Easy.Template.XCS.Test', 'Fixtures', 'Files');
const nativeDll = path.join(benchDir, 'dotnet', 'bin', 'Release', 'net10.0', 'Easy.Template.XCS.Bench.dll');
const allEngines = [...engineNames, 'dotnet-native'];

const { values: args } = parseArgs({
    options: {
        engines: { type: 'string', default: allEngines.join(',') },
        scenarios: { type: 'string', default: '' },
        'warmup-ms': { type: 'string', default: '1000' },
        'measure-ms': { type: 'string', default: '3000' },
        'cold-runs': { type: 'string', default: '5' },
        out: { type: 'string', default: path.join(benchDir, 'results') }
    }
});

const scenarios = findScenarios(args.scenarios ? args.scenarios.split(',') : []);
const engines = args.engines.split(',').filter(isAvailable);
const loopArgs = ['--warmup-ms', args['warmup-ms'], '--measure-ms', args['measure-ms']];
const scenarioArg = ['--scenarios', scenarios.map(s => s.name).join(',')];

const results = { environment: environment(), settings: { ...args }, engines: {} };

for (const engine of engines) {
    log(`\n[${engine}] steady state`);
    const steady = engine === 'dotnet-native' ? runNative() : runNode(engine, ['--mode', 'steady', ...loopArgs]);

    let cold;
    if (engine !== 'dotnet-native') {
        log(`[${engine}] cold start x${args['cold-runs']}`);
        const runs = Array.from({ length: Number(args['cold-runs']) }, () => runNode(engine, ['--mode', 'cold']));
        cold = {
            startupMs: median(runs.map(r => r.startupMs)),
            firstCallMs: Object.fromEntries(scenarios.map(s => [s.name, median(runs.map(r => r.firstCallMs[s.name]))]))
        };
    }

    results.engines[engine] = { ...steady, cold, bundle: bundleSize(engine) };
}

mkdirSync(args.out, { recursive: true });
const markdown = report(results);
writeFileSync(path.join(args.out, 'results.json'), JSON.stringify(results, null, 2) + '\n');
writeFileSync(path.join(args.out, 'results.md'), markdown);
console.log('\n' + markdown);
log(`Results written to ${path.relative(process.cwd(), args.out) || '.'}/results.{md,json}`);

//
// runners
//

function runNode(engine, extraArgs) {
    return run(process.execPath, ['--expose-gc', path.join(benchDir, 'lib', 'run-engine.mjs'), '--engine', engine, ...scenarioArg, ...extraArgs]);
}

function runNative() {
    // hand the exact same data to .NET: JSON, with binary values as base64
    // (the same encoding lib/engines.mjs uses for the WebAssembly engine)
    const exported = scenarios.map(s => ({
        name: s.name,
        templatePath: path.join(fixturesDir, s.template),
        json: JSON.stringify(s.data(), function (key, value) {
            const raw = this[key];
            return raw instanceof Uint8Array ? Buffer.from(raw).toString('base64') : value;
        })
    }));
    const file = path.join(benchDir, 'dist', 'scenarios.json');
    mkdirSync(path.dirname(file), { recursive: true });
    writeFileSync(file, JSON.stringify(exported));
    return run(dotnetPath(), [nativeDll, file, args['warmup-ms'], args['measure-ms']]);
}

function run(command, commandArgs) {
    const child = spawnSync(command, commandArgs, { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
    if (child.status !== 0)
        throw new Error(`${path.basename(command)} ${commandArgs.join(' ')} failed (${child.status}):\n${child.stderr}${child.stdout}`);
    const lastLine = child.stdout.trim().split('\n').pop();
    return JSON.parse(lastLine);
}

function isAvailable(engine) {
    if (!allEngines.includes(engine))
        throw new Error(`Unknown engine '${engine}'. Expected one of: ${allEngines.join(', ')}`);
    const missing =
        engine.startsWith('wasm-') && !existsSync(path.join(wasmBundleDir(engine.slice(5)), 'dotnet.js')) ? 'WebAssembly bundle not built' :
        engine === 'dotnet-native' && !existsSync(nativeDll) ? 'native harness not built' :
        engine === 'dotnet-native' && !dotnetPath() ? '.NET SDK not found' :
        null;
    if (missing)
        log(`Skipping ${engine}: ${missing} (run 'npm run build')`);
    return !missing;
}

function dotnetPath() {
    const candidates = [process.env.DOTNET_ROOT && path.join(process.env.DOTNET_ROOT, 'dotnet'), path.join(os.homedir(), '.dotnet', 'dotnet')];
    const found = candidates.find(c => c && existsSync(c));
    if (found)
        return found;
    const which = spawnSync('which', ['dotnet'], { encoding: 'utf8' });
    return which.status === 0 ? which.stdout.trim() : null;
}

//
// measurements
//

function bundleSize(engine) {
    if (engine === 'js') {
        // easy-template-x and its runtime dependencies (json5, jszip, lodash.get, @xmldom/xmldom)
        const deps = ['easy-template-x', 'json5', 'jszip', 'lodash.get', '@xmldom/xmldom', 'pako', 'lie', 'immediate',
            'readable-stream', 'setimmediate', 'core-util-is', 'inherits', 'isarray', 'process-nextick-args',
            'safe-buffer', 'string_decoder', 'util-deprecate'];
        return { bytes: deps.reduce((sum, d) => sum + dirSize(path.join(benchDir, 'node_modules', d)), 0), what: 'node_modules (package + dependencies)' };
    }
    if (engine.startsWith('wasm-'))
        return { bytes: dirSize(wasmBundleDir(engine.slice(5))), what: '_framework (runtime + assemblies)' };
    return null;
}

function dirSize(dir) {
    if (!existsSync(dir))
        return 0;
    return readdirSync(dir, { withFileTypes: true }).reduce((sum, entry) => {
        const full = path.join(dir, entry.name);
        return sum + (entry.isDirectory() ? dirSize(full) : statSync(full).size);
    }, 0);
}

function environment() {
    let dotnet = null;
    try {
        dotnet = execFileSync(dotnetPath(), ['--version'], { encoding: 'utf8' }).trim();
    } catch { /* not installed */ }
    return {
        date: new Date().toISOString(),
        node: process.version,
        v8: process.versions.v8,
        dotnetSdk: dotnet,
        os: `${os.type()} ${os.release()} ${os.arch()}`,
        cpu: `${os.cpus()[0]?.model ?? 'unknown'} (${os.cpus().length} cores)`,
        memoryGb: Math.round(os.totalmem() / 1024 ** 3)
    };
}

//
// report
//

function report({ environment: env, settings, engines: data }) {
    const names = Object.keys(data);
    const baseline = data.js;
    const lines = [];
    const table = (header, rows) => {
        lines.push(`| ${header.join(' | ')} |`, `| ${header.map((_, i) => (i === 0 ? '---' : '---:')).join(' | ')} |`);
        for (const row of rows)
            lines.push(`| ${row.join(' | ')} |`);
        lines.push('');
    };
    const ratio = (value, base) => (base ? ` (${(value / base).toFixed(1)}x)` : '');

    lines.push('# Benchmark results', '');
    lines.push(`- Date: ${env.date}`, `- Node.js ${env.node} (V8 ${env.v8})${env.dotnetSdk ? `, .NET SDK ${env.dotnetSdk}` : ''}`,
        `- ${env.os}, ${env.cpu}, ${env.memoryGb} GB RAM`,
        `- Warmup ${settings['warmup-ms']} ms, measure ${settings['measure-ms']} ms per scenario, cold start = median of ${settings['cold-runs']} fresh processes`, '');
    lines.push('Engines:', '');
    for (const name of names)
        lines.push(`- \`${name}\`: ${data[name].version}`);
    lines.push('');

    lines.push('## Steady state (median ms per document, lower is better)', '');
    lines.push('Ratios are relative to `js` (the original easy-template-x).', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name,
        ...names.map(n => {
            const median = data[n].scenarios[s.name].median;
            return `${fmt(median)}${n === 'js' ? '' : ratio(median, baseline?.scenarios[s.name].median)}`;
        })
    ]));

    lines.push('## Throughput (documents per second, higher is better)', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name, ...names.map(n => data[n].scenarios[s.name].opsPerSec.toFixed(1))
    ]));

    lines.push('## Tail latency (p95 ms)', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name, ...names.map(n => fmt(data[n].scenarios[s.name].p95))
    ]));

    const coldNames = names.filter(n => data[n].cold);
    lines.push('## Cold start (fresh Node.js process, ms)', '');
    lines.push('`startup` is module import + runtime initialization. The other rows are the first call of each scenario ' +
        'on a fresh engine (scenarios run in order, so later rows benefit from code already warmed by earlier ones).', '');
    table(['', ...coldNames.map(n => `\`${n}\``)], [
        ['startup', ...coldNames.map(n => fmt(data[n].cold.startupMs))],
        ...scenarios.map(s => [`first ${s.name}`, ...coldNames.map(n => fmt(data[n].cold.firstCallMs[s.name]))])
    ]);

    lines.push('## Footprint', '');
    table(['', ...names.map(n => `\`${n}\``)], [
        ['peak RSS (MB)', ...names.map(n => data[n].memory.peakRssMb.toFixed(0))],
        ['on disk (MB)', ...names.map(n => (data[n].bundle ? (data[n].bundle.bytes / 1024 ** 2).toFixed(1) : '-'))]
    ]);

    return lines.join('\n');
}

function fmt(ms) {
    return ms >= 100 ? ms.toFixed(0) : ms >= 10 ? ms.toFixed(1) : ms.toFixed(2);
}

function median(values) {
    const sorted = [...values].sort((a, b) => a - b);
    const mid = Math.floor(sorted.length / 2);
    return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

function log(message) {
    process.stderr.write(message + '\n');
}
