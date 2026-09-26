# Node.js benchmark: easy-template-x vs Easy.Template.XCS (WebAssembly)

Compares the original JavaScript library, [easy-template-x](https://github.com/alonrbar/easy-template-x) 7.2.8 (the feature set this port follows), with Easy.Template.XCS compiled to WebAssembly and running in the same Node.js runtime.

| Engine | What it is |
| --- | --- |
| `js` | `easy-template-x` from npm |
| `wasm-interp` | `src/Easy.Template.XCS.Wasm` published for `browser-wasm`, IL run by the Mono interpreter (the default .NET wasm mode) |
| `wasm-aot` | the same project published with `RunAOTCompilation=true` (IL compiled ahead of time to wasm) |
| `dotnet-native` | reference only: the library on regular .NET (CoreCLR), outside Node.js, fed the same JSON |

## Results

Full tables are in [results/results.md](results/results.md). Raw numbers are in [results/results.json](results/results.json). Machine: 4-core Intel Xeon @ 2.10GHz, Linux x64, Node.js 22.22, .NET SDK 10.0.401 (runtime 10.0.12).

Median time per generated document (lower is better), with the ratio to `js`:

| Scenario | `js` | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: | ---: |
| simple | 1.33 ms | 10.0 ms (7.5x) | 2.68 ms (2.0x) | 1.95 ms (1.5x) |
| table-rows-100 | 8.62 ms | 98.3 ms (11.4x) | 27.7 ms (3.2x) | 5.70 ms (0.7x) |
| table-rows-1000 | 75.9 ms | 868 ms (11.4x) | 269 ms (3.6x) | 46.3 ms (0.6x) |
| nested-loops | 27.7 ms | 325 ms (11.7x) | 93.4 ms (3.4x) | 17.8 ms (0.6x) |
| real-life-he | 28.5 ms | 335 ms (11.8x) | 93.8 ms (3.3x) | 18.5 ms (0.6x) |
| image | 5.88 ms | 19.5 ms (3.3x) | hangs, see below | 1.59 ms (0.3x) |

Cold start in a fresh Node.js process:

| | `js` | `wasm-interp` | `wasm-aot` |
| --- | ---: | ---: | ---: |
| import + runtime init | 42 ms | 178 ms | 303 ms |
| first `simple` document | 24 ms | 303 ms | 166 ms |
| on disk | 2.9 MB | 10.4 MB | 41.3 MB |

### Takeaways

- **In Node.js the original JavaScript library is the fastest option.** The WebAssembly build is about 11x slower with the interpreter (3.3x on the image scenario) and about 3.3x slower when AOT-compiled. It also takes longer to start (up to 0.3 s) and ships 3.5 to 14x more bytes.
- **The port is not what makes it slow.** On native .NET the same code is 1.5 to 3.7x *faster* than `easy-template-x`; only the trivial `simple` document is slower. The cost comes from running .NET on Mono-on-WebAssembly (no tiered JIT, interpreter or LLVM-AOT code, and a conservative GC stack scan), not from the template engine.
- **The interpreter build is the one to use today** if you need the .NET library from JavaScript: it works for every scenario, it is 10 MB, and it starts faster than AOT. AOT is ~3.5x faster in steady state but has the hang described below.
- **Binary data is relatively cheap.** The image scenario has the smallest gap (3.3x with the interpreter) even though the image goes through base64 and JSON. That is because hashing, zip and deflate run as compiled native code in both runtimes.
- All engines produce equivalent documents for every scenario (`npm run verify`: same text, paragraphs, table rows, page breaks, images and media parts).

### Known issue: `wasm-aot` hangs on repeated image documents

The AOT build reliably hangs after a few dozen image documents: on call 49 of the `image` scenario, and on call 18 with the larger `panda2.png`. The interpreter build and native .NET are not affected. What is known:

- The process spins at 100% CPU in the Mono runtime function `interp_mark_stack` (found with `node --prof` and a build using `-p:WasmEmitSymbolMap=true`). That function scans the *interpreter's* stack during a garbage collection. Even an AOT build runs some methods in the interpreter: generic instantiations over value types that were not pre-compiled, such as `OpenXmlSimpleValue<ShapeTypeValues>` from the image markup, reach it through `gsharedvt` wrappers.
- The hang is triggered by the GC: `MONO_GC_PARAMS=nursery-size=1m` hangs on the first call, and a 64 MB nursery only delays it (it still hangs during a full benchmark run).
- What did *not* help: disabling the jiterpreter, `Switch.System.Reflection.ForceInterpretedInvoke`, or adding explicit references to the value-type instantiations. Running SHA-256, base64 and zip on their own does not hang either.

This looks like a bug in the .NET 10 (10.0.12) Mono wasm runtime rather than in the library. To reproduce, run `npm run build`, then `node repro-aot-hang.mjs`: the counter stops at 48. `node repro-aot-hang.mjs wasm-interp` runs to completion. The benchmark gives every measurement its own process and a timeout (`--timeout-ms`, default 60 s), so a hang shows up as `hung ⚠` in the report instead of stalling the run.

## Running it

Prerequisites: Node.js 20+, the .NET 10 SDK and the wasm workload (`dotnet workload install wasm-tools`).

```sh
cd bench
npm run build     # wasm (interpreter + AOT, ~6 min), native reference, npm install
npm run verify    # check that all engines produce equivalent documents
npm run bench     # full run, writes results/results.{md,json}
```

`SKIP_AOT=1 npm run build` skips the slow AOT build; engines that are not built are skipped. Useful options:

```sh
node bench.mjs --engines js,wasm-interp --scenarios table-rows-100,image \
               --warmup-ms 1000 --measure-ms 3000 --cold-runs 5 --timeout-ms 60000
```

`XCS_WASM_GC_PARAMS` is passed to the WebAssembly engines as `MONO_GC_PARAMS`.

## Methodology

- **Same input everywhere.** Scenarios (`lib/scenarios.mjs`) use the original easy-template-x fixture documents from `src/Easy.Template.XCS.Test/Fixtures`, with seeded pseudo-random data. They are:
  - `simple`: one text tag.
  - `table-rows-100` and `table-rows-1000`: a table row loop.
  - `nested-loops`: 30 x 30 nested paragraph loops.
  - `real-life-he`: the Hebrew report card, 20 students x 5 groups, with raw xml page breaks.
  - `image`: image placeholder replacement with a 32 KB jpeg.
- **Fair boundary costs.** The `js` engine gets a plain JS object and a `Buffer`. The wasm engines get `JSON.stringify(data)` (with binaries as base64) and return a `Uint8Array`. Serialization and marshalling happen inside the timed region, because a JS caller of the wasm build pays them. `dotnet-native` also parses the JSON on every iteration.
- **Isolation.** Every steady-state measurement (engine x scenario) and every cold-start sample runs in a fresh process, so JIT state and GC heaps never carry over.
- **Steady state.** Each scenario warms up for 1 s (at least 5 iterations), then measures for 3 s (at least 10 iterations). The report shows median, p95 and throughput.
- **Cold start** is the median of 5 fresh processes. It covers module import plus runtime initialization, then the first call of each scenario in order.
- The handler instance is reused across iterations in all engines.

## Layout

| Path | |
| --- | --- |
| `../src/Easy.Template.XCS.Wasm/` | the WebAssembly project: `Exports.Process(byte[] template, string json) -> byte[]` and `Exports.Version()` via `[JSExport]` |
| `lib/engines.mjs` | loads each engine behind one `process(template, data)` interface (shows how to call the wasm build from JS) |
| `lib/scenarios.mjs` | scenario definitions and data generators |
| `lib/run-engine.mjs` | measures one engine in the current process |
| `bench.mjs` | orchestrates processes, writes the report |
| `verify.mjs` | output equivalence check |
| `repro-aot-hang.mjs` | reproduces the `wasm-aot` hang |
| `dotnet/` | native .NET reference harness |

Using the wasm build from your own code:

```js
import { dotnet } from './dist/interp/wwwroot/_framework/dotnet.js';

const runtime = await dotnet.create();
const { Exports } = (await runtime.getAssemblyExports('Easy.Template.XCS.Wasm')).Easy.Template.XCS.Wasm;
const docx = Exports.Process(templateBytes, JSON.stringify(data)); // Uint8Array
```
