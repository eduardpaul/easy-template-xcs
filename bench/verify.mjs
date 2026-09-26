// Runs every scenario once on every engine and checks that the generated
// documents are equivalent (same text, same structure, no leftover tags).
//
//   node verify.mjs [--engines js,wasm-interp-net10,...] [--out <dir>]
//
// Without --engines, every engine that has been built is checked.

import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { engineNames, loadEngine, wasmBundleDir } from './lib/engines.mjs';
import { inspectDocx } from './lib/docx.mjs';
import { readTemplate, scenarios } from './lib/scenarios.mjs';

const { values: args } = parseArgs({
    options: {
        engines: { type: 'string' },
        out: { type: 'string' }
    }
});

const engines = [];
for (const name of (args.engines ?? engineNames.join(',')).split(',')) {
    if (!args.engines && name.startsWith('wasm-') && !existsSync(path.join(wasmBundleDir(name), 'dotnet.js'))) {
        console.log(`skip ${name} (not built)`);
        continue;
    }
    engines.push(await loadEngine(name));
}

let failures = 0;
for (const scenario of scenarios) {
    const template = readTemplate(scenario.template);
    const results = [];
    for (const engine of engines) {
        const doc = await engine.process(template, scenario.data());
        if (args.out) {
            mkdirSync(args.out, { recursive: true });
            writeFileSync(path.join(args.out, `${scenario.name}.${engine.name}.docx`), doc);
        }
        results.push({ engine: engine.name, ...(await inspectDocx(doc)) });
    }

    const [reference, ...others] = results;
    const problems = [];
    for (const result of results) {
        if (/[{}]/.test(result.text))
            problems.push(`${result.engine}: leftover tag delimiters in output`);
    }
    for (const other of others) {
        for (const key of ['text', 'paragraphs', 'tableRows', 'pageBreaks', 'images', 'media']) {
            if (other[key] !== reference[key])
                problems.push(`${other.engine} differs from ${reference.engine} in ${key}` +
                    (key === 'text' ? '' : ` (${other[key]} vs ${reference[key]})`));
        }
    }

    const summary = `${reference.paragraphs} paragraphs, ${reference.tableRows} table rows, ` +
        `${reference.images} images, ${reference.text.length} chars`;
    if (problems.length) {
        failures++;
        console.log(`FAIL ${scenario.name} (${summary})`);
        for (const problem of problems)
            console.log(`     - ${problem}`);
    } else {
        console.log(`ok   ${scenario.name} (${summary})`);
    }
}

console.log(failures ? `\n${failures} scenario(s) failed` : `\nAll engines produced equivalent documents: ${engines.map(e => e.version).join(' | ')}`);
process.exit(failures ? 1 : 0);
