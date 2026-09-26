// Reproduces the wasm-aot hang (see README.md, "Known issue"): processes the
// image scenario repeatedly and prints a counter after every document.
// With .NET 10.0.12 the counter stops at 48 and the process spins at 100% CPU.
//
//   node repro-aot-hang.mjs [engine=wasm-aot] [iterations=200]

import { loadEngine } from './lib/engines.mjs';
import { readTemplate, scenarios } from './lib/scenarios.mjs';

const [engineName = 'wasm-aot', iterations = '200'] = process.argv.slice(2);
const engine = await loadEngine(engineName);
const scenario = scenarios.find(s => s.name === 'image');
const template = readTemplate(scenario.template);
const data = scenario.data();

for (let i = 1; i <= Number(iterations); i++) {
    await engine.process(template, data);
    console.log(i);
}
console.log(`${engine.version}: no hang`);
