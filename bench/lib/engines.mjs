// Engine adapters. Every engine exposes the same shape:
//   { name, version, process(template: Buffer, data: object): Promise<Uint8Array> }
// so the benchmark runner can treat them uniformly.

import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const benchDir = path.resolve(import.meta.dirname, '..');

export const engineNames = ['js', 'wasm-interp', 'wasm-aot'];

export function wasmBundleDir(variant) {
    return path.join(benchDir, 'dist', variant, 'wwwroot', '_framework');
}

export async function loadEngine(name) {
    switch (name) {
        case 'js':
            return loadJs();
        case 'wasm-interp':
            return loadWasm('interp');
        case 'wasm-aot':
            return loadWasm('aot');
        default:
            throw new Error(`Unknown engine '${name}'. Expected one of: ${engineNames.join(', ')}`);
    }
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
async function loadWasm(variant) {
    const dir = wasmBundleDir(variant);
    const entry = path.join(dir, 'dotnet.js');
    if (!existsSync(entry))
        throw new Error(`WebAssembly bundle not found at ${dir}. Run 'npm run build' first.`);

    const { dotnet } = await import(pathToFileURL(entry).href);
    const runtime = await dotnet.create();
    const exports = await runtime.getAssemblyExports('Easy.Template.XCS.Wasm');
    const api = exports.Easy.Template.XCS.Wasm.Exports;

    return {
        name: `wasm-${variant}`,
        version: `Easy.Template.XCS ${api.Version().split('+')[0]} (${variant === 'aot' ? 'wasm AOT' : 'wasm interpreter'})`,
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
