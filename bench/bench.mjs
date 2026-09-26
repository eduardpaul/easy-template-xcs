// Benchmark orchestrator: easy-template-x (JavaScript) vs Easy.Template.XCS
// compiled to WebAssembly, both running in Node.js. A native .NET run is
// included as a reference when the .NET SDK is available.
//
// Every engine runs in its own fresh process so JIT state, GC heaps and
// caches never leak from one engine into another.
//
// Each (engine, scenario) steady state measurement gets its own process with a
// timeout, so a hung engine is reported as such instead of stalling the run.
//
//   node bench.mjs [--engines js,wasm-interp-net10,wasm-aot-net11,...] [--scenarios a,b]
//                  [--warmup-ms 1000] [--measure-ms 3000] [--cold-runs 5]
//                  [--timeout-ms 60000] [--out results]

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
const nativeEngines = { 'dotnet-native-net10': 'net10.0', 'dotnet-native-net11': 'net11.0' };
const nativeDll = engine => path.join(benchDir, 'dotnet', 'bin', 'Release', nativeEngines[engine], 'Easy.Template.XCS.Bench.dll');
const isNative = engine => engine in nativeEngines;
const allEngines = [...engineNames, ...Object.keys(nativeEngines)];

const { values: args } = parseArgs({
    options: {
        engines: { type: 'string', default: allEngines.join(',') },
        scenarios: { type: 'string', default: '' },
        'warmup-ms': { type: 'string', default: '1000' },
        'measure-ms': { type: 'string', default: '3000' },
        'cold-runs': { type: 'string', default: '5' },
        'timeout-ms': { type: 'string', default: '60000' },
        out: { type: 'string', default: path.join(benchDir, 'results') }
    }
});

const scenarios = findScenarios(args.scenarios ? args.scenarios.split(',') : []);
const engines = args.engines.split(',').filter(isAvailable);
const loopArgs = ['--warmup-ms', args['warmup-ms'], '--measure-ms', args['measure-ms']];
const scenarioArg = ['--scenarios', scenarios.map(s => s.name).join(',')];

const timeoutMs = Number(args['timeout-ms']);
const results = { environment: environment(), settings: { ...args, gcParams: process.env.XCS_WASM_GC_PARAMS ?? null }, engines: {} };

for (const engine of engines) {
    const result = isNative(engine) ? runNative(engine) : runNodeSteady(engine);
    if (!isNative(engine))
        result.cold = runNodeCold(engine);
    results.engines[engine] = { ...result, bundle: bundleSize(engine) };
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
    return run(process.execPath, ['--expose-gc', path.join(benchDir, 'lib', 'run-engine.mjs'), '--engine', engine, ...extraArgs]);
}

function runNodeSteady(engine) {
    const result = { scenarios: {}, memory: { peakRssMb: 0 } };
    for (const scenario of scenarios) {
        log(`[${engine}] ${scenario.name}`);
        const { lines, timedOut } = runNode(engine, ['--mode', 'steady', '--scenarios', scenario.name, ...loopArgs]);
        if (timedOut) {
            log(`  timed out after ${timeoutMs} ms`);
            result.scenarios[scenario.name] = null;
            continue;
        }
        const run = lines[lines.length - 1];
        result.version = run.version;
        result.scenarios[scenario.name] = run.scenarios[scenario.name];
        result.memory.peakRssMb = Math.max(result.memory.peakRssMb, run.memory.peakRssMb);
    }
    return result;
}

function runNodeCold(engine) {
    log(`[${engine}] cold start x${args['cold-runs']}`);
    const runs = Array.from({ length: Number(args['cold-runs']) }, () => {
        const { lines } = runNode(engine, ['--mode', 'cold', ...scenarioArg]);
        return {
            startupMs: lines[0]?.startupMs,
            firstCallMs: Object.fromEntries(lines.slice(1).map(l => [l.scenario, l.firstCallMs]))
        };
    });
    return {
        startupMs: median(runs.map(r => r.startupMs)),
        firstCallMs: Object.fromEntries(scenarios.map(s => [s.name, median(runs.map(r => r.firstCallMs[s.name]))]))
    };
}

function runNative(engine) {
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
    log(`[${engine}] all scenarios`);
    const { lines, timedOut } = run(dotnetPath(), [nativeDll(engine), file, args['warmup-ms'], args['measure-ms']], timeoutMs * scenarios.length);
    if (timedOut)
        throw new Error(`${engine} timed out`);
    return lines[lines.length - 1];
}

// Runs a child process and parses every stdout line as JSON. A process that
// runs past the timeout is killed and reported as timed out, keeping the
// lines it printed so far.
function run(command, commandArgs, timeout = timeoutMs) {
    const child = spawnSync(command, commandArgs, { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, timeout, killSignal: 'SIGKILL' });
    const timedOut = child.error?.code === 'ETIMEDOUT';
    if (!timedOut && child.status !== 0)
        throw new Error(`${path.basename(command)} ${commandArgs.join(' ')} failed (${child.status}):\n${child.stderr}${child.stdout}`);
    const lines = child.stdout.split('\n').filter(l => l.startsWith('{')).map(l => JSON.parse(l));
    return { lines, timedOut };
}

function isAvailable(engine) {
    if (!allEngines.includes(engine))
        throw new Error(`Unknown engine '${engine}'. Expected one of: ${allEngines.join(', ')}`);
    const missing =
        engine.startsWith('wasm-') && !existsSync(path.join(wasmBundleDir(engine), 'dotnet.js')) ? 'WebAssembly bundle not built' :
        isNative(engine) && !existsSync(nativeDll(engine)) ? 'native harness not built' :
        isNative(engine) && !dotnetPath() ? '.NET SDK not found' :
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
        return { bytes: dirSize(wasmBundleDir(engine)), what: '_framework (runtime + assemblies)' };
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
    const hung = 'hung ⚠';
    const stat = (name, scenario, key, format) => {
        const s = data[name].scenarios[scenario];
        return s ? format(s[key]) : hung;
    };

    lines.push('# Benchmark results', '');
    lines.push(`- Date: ${env.date}`, `- Node.js ${env.node} (V8 ${env.v8})${env.dotnetSdk ? `, .NET SDK ${env.dotnetSdk}` : ''}`,
        `- ${env.os}, ${env.cpu}, ${env.memoryGb} GB RAM`,
        `- Warmup ${settings['warmup-ms']} ms, measure ${settings['measure-ms']} ms per scenario (one fresh process each), cold start = median of ${settings['cold-runs']} fresh processes`,
        ...(settings.gcParams ? [`- WebAssembly engines run with \`MONO_GC_PARAMS=${settings.gcParams}\``] : []),
        '');
    lines.push('Engines:', '');
    for (const name of names)
        lines.push(`- \`${name}\`: ${data[name].version}`);
    lines.push('');

    lines.push('## Steady state (median ms per document, lower is better)', '');
    lines.push('Ratios are relative to `js` (the original easy-template-x).', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name,
        ...names.map(n => stat(n, s.name, 'median', median =>
            `${fmt(median)}${n === 'js' ? '' : ratio(median, baseline?.scenarios[s.name]?.median)}`))
    ]));

    lines.push('## Throughput (documents per second, higher is better)', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name, ...names.map(n => stat(n, s.name, 'opsPerSec', x => x.toFixed(1)))
    ]));

    lines.push('## Tail latency (p95 ms)', '');
    table(['Scenario', ...names.map(n => `\`${n}\``)], scenarios.map(s => [
        s.name, ...names.map(n => stat(n, s.name, 'p95', fmt))
    ]));

    // .NET 11 vs .NET 10, for every engine measured on both
    const pairs = names.filter(n => n.endsWith('-net11') && data[n.replace(/-net11$/, '-net10')])
        .map(n => [n.replace(/-net11$/, '-net10'), n]);
    if (pairs.length) {
        lines.push('## .NET 11 vs .NET 10 (speedup of the median, higher is better)', '');
        lines.push('`1.25x` means .NET 11 generates the document 25% faster than .NET 10 (`net10 median / net11 median`).', '');
        table(['Scenario', ...pairs.map(([, n11]) => `\`${n11.replace(/-net11$/, '')}\``)], scenarios.map(s => [
            s.name, ...pairs.map(([n10, n11]) => {
                const a = data[n10].scenarios[s.name];
                const b = data[n11].scenarios[s.name];
                return a && b ? `${(a.median / b.median).toFixed(2)}x` : hung;
            })
        ]));
    }

    const coldNames = names.filter(n => data[n].cold);
    lines.push('## Cold start (fresh Node.js process, ms)', '');
    lines.push('`startup` is module import + runtime initialization. The other rows are the first call of each scenario ' +
        'on a fresh engine (scenarios run in order, so later rows benefit from code already warmed by earlier ones).', '');
    table(['', ...coldNames.map(n => `\`${n}\``)], [
        ['startup', ...coldNames.map(n => fmt(data[n].cold.startupMs))],
        ...scenarios.map(s => [`first ${s.name}`, ...coldNames.map(n => {
            const ms = data[n].cold.firstCallMs[s.name];
            return ms === undefined ? hung : fmt(ms);
        })])
    ]);

    lines.push('## Footprint', '');
    table(['', ...names.map(n => `\`${n}\``)], [
        ['peak RSS (MB)', ...names.map(n => data[n].memory.peakRssMb.toFixed(0))],
        ['on disk (MB)', ...names.map(n => (data[n].bundle ? (data[n].bundle.bytes / 1024 ** 2).toFixed(1) : '-'))]
    ]);

    if (Object.values(data).some(e => Object.values(e.scenarios).includes(null)))
        lines.push(`${hung}: the engine did not finish within the ${settings['timeout-ms']} ms timeout (see bench/README.md).`, '');

    return lines.join('\n');
}

function fmt(ms) {
    return ms >= 100 ? ms.toFixed(0) : ms >= 10 ? ms.toFixed(1) : ms.toFixed(2);
}

function median(values) {
    const sorted = values.filter(v => v !== undefined).sort((a, b) => a - b);
    if (sorted.length === 0)
        return undefined;
    const mid = Math.floor(sorted.length / 2);
    return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

function log(message) {
    process.stderr.write(message + '\n');
}
