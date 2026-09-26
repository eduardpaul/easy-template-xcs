// Runs the benchmark scenarios for a single engine in the current process and
// prints the results as JSON on stdout. Spawned by bench.mjs, one fresh
// process per engine (and per cold start sample).
//
//   node lib/run-engine.mjs --engine <name> [--mode steady|cold] [--scenarios a,b]
//                           [--warmup-ms 1000] [--measure-ms 3000]

import { parseArgs } from 'node:util';
import { loadEngine } from './engines.mjs';
import { findScenarios, readTemplate } from './scenarios.mjs';
import { summarize } from './stats.mjs';

const { values: args } = parseArgs({
    options: {
        engine: { type: 'string' },
        mode: { type: 'string', default: 'steady' },
        scenarios: { type: 'string', default: '' },
        'warmup-ms': { type: 'string', default: '1000' },
        'measure-ms': { type: 'string', default: '3000' },
        'min-iterations': { type: 'string', default: '10' },
        'max-iterations': { type: 'string', default: '5000' }
    }
});

const scenarios = findScenarios(args.scenarios ? args.scenarios.split(',') : []);

// startup: module import + runtime initialization
const startupBegin = performance.now();
const engine = await loadEngine(args.engine);
const startupMs = performance.now() - startupBegin;

if (args.mode === 'cold') {
    // one call per scenario on a fresh engine: what a CLI / serverless
    // invocation would pay
    const firstCallMs = {};
    for (const scenario of scenarios) {
        const template = readTemplate(scenario.template);
        const begin = performance.now();
        await engine.process(template, scenario.data());
        firstCallMs[scenario.name] = performance.now() - begin;
    }
    print({ engine: engine.name, version: engine.version, startupMs, firstCallMs });
} else {
    const warmupMs = Number(args['warmup-ms']);
    const measureMs = Number(args['measure-ms']);
    const minIterations = Number(args['min-iterations']);
    const maxIterations = Number(args['max-iterations']);

    const results = {};
    for (const scenario of scenarios) {
        const template = readTemplate(scenario.template);
        const data = scenario.data();

        await timeLoop(engine, template, data, warmupMs, Math.min(5, minIterations), maxIterations);
        globalThis.gc?.();
        const samples = await timeLoop(engine, template, data, measureMs, minIterations, maxIterations);
        results[scenario.name] = summarize(samples);
    }

    const usage = process.resourceUsage();
    print({
        engine: engine.name,
        version: engine.version,
        startupMs,
        scenarios: results,
        memory: {
            peakRssMb: usage.maxRSS / 1024,
            ...Object.fromEntries(Object.entries(process.memoryUsage()).map(([k, v]) => [`${k}Mb`, v / 1024 / 1024]))
        }
    });
}

// Run until both the time budget and the minimum number of iterations are
// reached (or the iteration cap is hit). The data object is shared between
// iterations: neither engine mutates it.
async function timeLoop(engine, template, data, budgetMs, minIterations, maxIterations) {
    const samples = [];
    const loopBegin = performance.now();
    while (samples.length < maxIterations &&
        (samples.length < minIterations || performance.now() - loopBegin < budgetMs)) {
        const begin = performance.now();
        await engine.process(template, data);
        samples.push(performance.now() - begin);
    }
    return samples;
}

function print(result) {
    process.stdout.write(JSON.stringify(result) + '\n');
}
