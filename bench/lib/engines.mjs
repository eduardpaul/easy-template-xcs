// Engine adapters. Every engine exposes the same shape:
//   { name, version, process(template: Buffer, data: object): Promise<Uint8Array> }
// so the benchmark runner can treat them uniformly.

import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const benchDir = path.resolve(import.meta.dirname, '..');

// WebAssembly builds of Easy.Template.XCS, per .NET version (see build.sh):
//   interp   Mono, IL interpreter (the default .NET wasm runtime)
//   aot      Mono, IL compiled ahead of time to wasm
//   coreclr  CoreCLR on WebAssembly (.NET 11+, interpreter)
export const wasmEngines = {
    'wasm-interp-net10': { tfm: 'net10', variant: 'interp' },
    'wasm-aot-net10': { tfm: 'net10', variant: 'aot' },
    'wasm-interp-net11': { tfm: 'net11', variant: 'interp' },
    'wasm-aot-net11': { tfm: 'net11', variant: 'aot' },
    'wasm-coreclr-net11': { tfm: 'net11', variant: 'coreclr' }
};

export const engineNames = ['js', ...Object.keys(wasmEngines)];

const variantLabels = { interp: 'Mono interpreter', aot: 'Mono AOT', coreclr: 'CoreCLR interpreter' };

export function wasmBundleDir(engine) {
    const { tfm, variant } = wasmEngines[engine];
    return path.join(benchDir, 'dist', tfm, variant, 'wwwroot', '_framework');
}

export async function loadEngine(name) {
    if (name === 'js')
        return loadJs();
    if (wasmEngines[name])
        return loadWasm(name);
    throw new Error(`Unknown engine '${name}'. Expected one of: ${engineNames.join(', ')}`);
}

// The original JavaScript library.
async function loadJs() {
    const { TemplateHandler } = await import('easy-template-x');
    const pkg = JSON.parse(readFileSync(path.join(benchDir, 'node_modules', 'easy-template-x', 'package.json'), 'utf8'));
    const handler = new TemplateHandler();
    return {
        name: 'js',
        version: `easy-template-x ${pkg.version}`,
        process: (template, data) => handler.process(template, data)
    };
}

// Easy.Template.XCS compiled to WebAssembly (see src/Easy.Template.XCS.Wasm).
async function loadWasm(name) {
    const { variant } = wasmEngines[name];
    const dir = wasmBundleDir(name);
    const entry = path.join(dir, 'dotnet.js');
    if (!existsSync(entry))
        throw new Error(`WebAssembly bundle not found at ${dir}. Run 'npm run build' first.`);

    const { dotnet } = await import(pathToFileURL(entry).href);
    // XCS_WASM_GC_PARAMS is passed to the Mono GC as MONO_GC_PARAMS
    // (for instance 'nursery-size=64m', see bench/README.md)
    const gcParams = process.env.XCS_WASM_GC_PARAMS;
    const runtime = await (gcParams ? dotnet.withEnvironmentVariable('MONO_GC_PARAMS', gcParams) : dotnet).create();
    const exports = await runtime.getAssemblyExports('Easy.Template.XCS.Wasm');
    const api = exports.Easy.Template.XCS.Wasm.Exports;

    return {
        name,
        version: `Easy.Template.XCS ${api.Version().split('+')[0]} on ${api.Runtime()}, wasm ${variantLabels[variant]}`,
        // Data crosses the JS/.NET boundary as JSON; binary values (images)
        // are sent as base64 strings. That cost is part of the measurement.
        process: async (template, data) => api.Process(template, JSON.stringify(data, binaryAsBase64))
    };
}

function binaryAsBase64(key, value) {
    // Look at the raw value: Buffer.prototype.toJSON has already run on `value`.
    const raw = this[key];
    if (raw instanceof Uint8Array)
        return Buffer.from(raw.buffer, raw.byteOffset, raw.byteLength).toString('base64');
    return value;
}
