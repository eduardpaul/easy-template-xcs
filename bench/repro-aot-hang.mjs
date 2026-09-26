// Reproduces the Mono wasm AOT hang (see README.md, "Known issue"): replaces
// the image placeholder with a 354 KB png over and over and prints a counter
// after every document. The counter stops (process spinning at 100% CPU) at
// 18 with .NET 10.0.12 and at 15 with .NET 11.0 rc.1.
//
//   node repro-aot-hang.mjs [engine=wasm-aot-net10] [iterations=200]

import { readFileSync } from 'node:fs';
import path from 'node:path';
import { loadEngine } from './lib/engines.mjs';
import { readTemplate } from './lib/scenarios.mjs';

const [engineName = 'wasm-aot-net10', iterations = '200'] = process.argv.slice(2);
const engine = await loadEngine(engineName);
const template = readTemplate('image - placeholder.docx');
const png = readFileSync(path.join(import.meta.dirname, '..', 'src', 'Easy.Template.XCS.Test', 'Fixtures', 'Res', 'panda2.png'));
const data = { 'My Tag 2': { _type: 'image', source: png, format: 'image/png' } };

for (let i = 1; i <= Number(iterations); i++) {
    await engine.process(template, data);
    console.log(i);
}
console.log(`${engine.version}: no hang`);
