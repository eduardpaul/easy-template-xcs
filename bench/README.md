# Node.js benchmark: easy-template-x vs Easy.Template.XCS (WebAssembly)

Compares the original JavaScript library, [easy-template-x](https://github.com/alonrbar/easy-template-x) 7.2.8 (the feature set this port follows), with Easy.Template.XCS compiled to WebAssembly and running in the same Node.js runtime.

Engine names end with the .NET version they were built with (`-net10`, `-net11`):

| Engine | What it is |
| --- | --- |
| `js` | `easy-template-x` from npm |
| `wasm-interp-*` | `src/Easy.Template.XCS.Wasm` published for `browser-wasm` on the Mono runtime, with IL run by the Mono interpreter (the default .NET wasm mode) |
| `wasm-aot-*` | the same project on Mono, published with `RunAOTCompilation=true` so IL is compiled ahead of time to wasm |
| `wasm-coreclr-net11` | the same project on **CoreCLR** for WebAssembly (`UseMonoRuntime=false`), new in .NET 11. It is interpreter only: there is no AOT/ReadyToRun option for browser-wasm yet |
| `dotnet-native-*` | reference only: the library on regular .NET (CoreCLR JIT), outside Node.js, fed the same JSON |

## Results

Full tables are in [results/results.md](results/results.md). Raw numbers are in [results/results.json](results/results.json).

Setup: 4-core Intel Xeon @ 2.10GHz, Linux x64, Node.js 22.22, .NET 10.0.12 and .NET 11.0.0-rc.1 (SDK 11.0.100-rc.1).

Median time per generated document (lower is better), with the ratio to `js`:

| Scenario | `js` | `wasm-interp-net10` | `wasm-interp-net11` | `wasm-aot-net10` | `wasm-aot-net11` | `wasm-coreclr-net11` | `dotnet-native-net10` | `dotnet-native-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| simple | 1.33 ms | 11.0 (8.3x) | 12.1 (9.1x) | 2.47 (1.9x) | 2.78 (2.1x) | 74.4 (56x) | 2.05 (1.5x) | 2.06 (1.6x) |
| table-rows-100 | 8.81 ms | 102 (11.6x) | 111 (12.6x) | 26.5 (3.0x) | 27.8 (3.2x) | 855 (97x) | 6.49 (0.7x) | 6.48 (0.7x) |
| table-rows-1000 | 78.6 ms | 890 (11.3x) | 871 (11.1x) | 283 (3.6x) | 258 (3.3x) | 8082 (103x) | 49.6 (0.6x) | 51.2 (0.7x) |
| nested-loops | 28.6 ms | 366 (12.8x) | 342 (12.0x) | 92.6 (3.2x) | 89.7 (3.1x) | 2999 (105x) | 18.4 (0.6x) | 18.5 (0.6x) |
| real-life-he | 28.9 ms | 320 (11.1x) | 332 (11.5x) | 93.6 (3.2x) | 93.1 (3.2x) | 2999 (104x) | 20.4 (0.7x) | 19.8 (0.7x) |
| image | 6.39 ms | 19.7 (3.1x) | 22.7 (3.5x) | hangs, see below | 6.13 (1.0x) | 275 (43x) | 1.64 (0.3x) | 1.62 (0.3x) |

### .NET 11 vs .NET 10

Speedup of the median, computed as `net10 / net11` (higher is better):

| Scenario | Mono interpreter | Mono AOT | native |
| --- | ---: | ---: | ---: |
| simple | 0.91x | 0.89x | 1.00x |
| table-rows-100 | 0.92x | 0.96x | 1.00x |
| table-rows-1000 | 1.02x | 1.09x | 0.97x |
| nested-loops | 1.07x | 1.03x | 0.99x |
| real-life-he | 0.96x | 1.01x | 1.03x |
| image | 0.87x | (net10 hangs) | 1.01x |

**.NET 11 (rc.1) brings no measurable speedup for this library, in WebAssembly or native.** Every ratio is between 0.87x and 1.09x. That is inside the run-to-run noise on this machine: the same .NET 10 builds measured 1 to 13% apart between two sessions (nested-loops on the interpreter took 325 ms in one run and 366 ms in the other). Startup (185 vs 192 ms for the interpreter, 290 vs 287 ms for AOT) and bundle size (10.6 vs 10.4 MB, 42.9 vs 41.3 MB) did not change either.

What .NET 11 does change:

- **The AOT image scenario no longer hangs**, so AOT can be measured there. At 6.1 ms it matches the JS library (1.0x). The underlying runtime bug is *not* fixed: the png repro still hangs, see below.
- **CoreCLR on WebAssembly is new and 6 to 12x slower than Mono's interpreter** (about 9x on the larger documents, 43 to 105x slower than the JS library). It is a straight IL interpreter in this preview (Mono has the jiterpreter and AOT), and it takes longer to start (320 ms). Correctness is fine: it produces the same documents as every other engine. It is not an option for performance yet.

Cold start in a fresh Node.js process:

| | `js` | `wasm-interp-net10` | `wasm-interp-net11` | `wasm-aot-net10` | `wasm-aot-net11` | `wasm-coreclr-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| import + runtime init | 40 ms | 192 ms | 185 ms | 287 ms | 290 ms | 320 ms |
| first `simple` document | 22 ms | 276 ms | 277 ms | 164 ms | 165 ms | 460 ms |
| on disk | 2.9 MB | 10.4 MB | 10.6 MB | 41.3 MB | 42.9 MB | 12.3 MB |

### Takeaways

- **In Node.js the original JavaScript library is the fastest option.** This holds on .NET 10 and .NET 11 alike. The WebAssembly build is 11 to 13x slower with the Mono interpreter (3.1 to 3.5x on the image scenario) and about 3.2x slower when AOT-compiled. It also takes longer to start (up to 0.3 s) and ships 3.5 to 15x more bytes.
- **The port is not what makes it slow.** On native .NET the same code is 1.4 to 3.9x *faster* than `easy-template-x`; only the trivial `simple` document is slower. The cost comes from running .NET on WebAssembly (no tiered JIT, interpreter or LLVM-AOT code, and a conservative GC stack scan), not from the template engine.
- **The Mono interpreter build is the one to use today** if you need the .NET library from JavaScript: it works for every scenario, it is 10 MB, and it starts faster than AOT. AOT is 3.5 to 4x faster in steady state but has the hang described below, on both .NET versions. Upgrading from .NET 10 to .NET 11 makes no performance difference either way.
- **Binary data is relatively cheap.** The image scenario has the smallest gap even though the image goes through base64 and JSON. That is because hashing, zip and deflate run as compiled native code in every runtime.
- All engines produce equivalent documents for every scenario (`npm run verify`: same text, paragraphs, table rows, page breaks, images and media parts).

### Known issue: Mono AOT hangs on repeated image documents

The Mono AOT builds reliably hang after a number of image documents. On .NET 10 that is call 49 of the `image` scenario, or call 18 with the larger `panda2.png`. On .NET 11 rc.1 the `image` scenario survives 1,000 calls, but `panda2.png` still hangs on call 15. The interpreter builds, CoreCLR and native .NET are not affected. What is known:

- The process spins at 100% CPU in the Mono runtime function `interp_mark_stack` (found on .NET 10 with `node --prof` and a build using `-p:WasmEmitSymbolMap=true`). That function scans the *interpreter's* stack during a garbage collection. Even an AOT build runs some methods in the interpreter: generic instantiations over value types that were not pre-compiled, such as `OpenXmlSimpleValue<ShapeTypeValues>` from the image markup, reach it through `gsharedvt` wrappers.
- The hang is triggered by the GC. On .NET 10, `MONO_GC_PARAMS=nursery-size=1m` hangs on the first call, and a 64 MB nursery only delays it (it still hangs during a full benchmark run).
- What did *not* help: disabling the jiterpreter, `Switch.System.Reflection.ForceInterpretedInvoke`, or adding explicit references to the value-type instantiations. Running SHA-256, base64 and zip on their own does not hang either.

This looks like a bug in the Mono wasm runtime (present in 10.0.12 and 11.0.0-rc.1) rather than in the library. To reproduce, run `npm run build`, then `node repro-aot-hang.mjs wasm-aot-net10` or `node repro-aot-hang.mjs wasm-aot-net11`: the counter stops at 18 or 15. `node repro-aot-hang.mjs wasm-interp-net11` runs to completion. The benchmark gives every measurement its own process and a timeout (`--timeout-ms`, default 60 s), so a hang shows up as `hung ⚠` in the report instead of stalling the run.

## Running it

Prerequisites:

- Node.js 20+.
- The .NET 10 SDK, or the .NET 11 SDK to also build the `-net11` engines.
- The wasm workloads:
  - `dotnet workload install wasm-tools`.
  - With SDK 11, also `dotnet workload install wasm-tools-net10` for the net10 builds.

```sh
cd bench
npm run build     # wasm (interpreter, AOT, CoreCLR per .NET version, ~12 min with SDK 11), native reference, npm install
npm run verify    # check that all engines produce equivalent documents
npm run bench     # full run, writes results/results.{md,json}
```

Build options:

- `SKIP_AOT=1 npm run build` skips the slow AOT builds.
- `TFMS=net11 npm run build` builds only one .NET version.

Engines that are not built are skipped. The results above were measured with `--timeout-ms 400000`: `wasm-coreclr-net11` needs ~8 s per `table-rows-1000` document. Useful options:

```sh
node bench.mjs --engines js,wasm-interp-net10,wasm-interp-net11 --scenarios table-rows-100,image \
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
| `../src/Easy.Template.XCS.Wasm/` | the WebAssembly project (net10.0 and net11.0): `Exports.Process(byte[] template, string json) -> byte[]`, `Exports.Version()` and `Exports.Runtime()` via `[JSExport]` |
| `lib/engines.mjs` | loads each engine behind one `process(template, data)` interface (shows how to call the wasm build from JS) |
| `lib/scenarios.mjs` | scenario definitions and data generators |
| `lib/run-engine.mjs` | measures one engine in the current process |
| `bench.mjs` | orchestrates processes, writes the report |
| `verify.mjs` | output equivalence check |
| `repro-aot-hang.mjs` | reproduces the Mono AOT hang |
| `dotnet/` | native .NET reference harness |

Using the wasm build from your own code:

```js
import { dotnet } from './dist/net10/interp/wwwroot/_framework/dotnet.js';

const runtime = await dotnet.create();
const { Exports } = (await runtime.getAssemblyExports('Easy.Template.XCS.Wasm')).Easy.Template.XCS.Wasm;
const docx = Exports.Process(templateBytes, JSON.stringify(data)); // Uint8Array
```
