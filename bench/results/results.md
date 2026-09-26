# Benchmark results

- Date: 2026-09-26T08:01:12.583Z
- Node.js v22.22.2 (V8 12.4.254.21-node.39), .NET SDK 10.0.401
- Linux 6.18.44-fc-v37 x64, Intel(R) Xeon(R) Processor @ 2.10GHz (4 cores), 16 GB RAM
- Warmup 1000 ms, measure 3000 ms per scenario (one fresh process each), cold start = median of 5 fresh processes

Engines:

- `js`: easy-template-x 7.2.8
- `wasm-interp`: Easy.Template.XCS 1.0.0 (wasm interpreter)
- `wasm-aot`: Easy.Template.XCS 1.0.0 (wasm AOT)
- `dotnet-native`: Easy.Template.XCS 1.0.0 (native .NET 10.0.12)

## Steady state (median ms per document, lower is better)

Ratios are relative to `js` (the original easy-template-x).

| Scenario | `js` | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: | ---: |
| simple | 1.33 | 10.0 (7.5x) | 2.68 (2.0x) | 1.95 (1.5x) |
| table-rows-100 | 8.62 | 98.3 (11.4x) | 27.7 (3.2x) | 5.70 (0.7x) |
| table-rows-1000 | 75.9 | 868 (11.4x) | 269 (3.6x) | 46.3 (0.6x) |
| nested-loops | 27.7 | 325 (11.7x) | 93.4 (3.4x) | 17.8 (0.6x) |
| real-life-he | 28.5 | 335 (11.8x) | 93.8 (3.3x) | 18.5 (0.6x) |
| image | 5.88 | 19.5 (3.3x) | hung ⚠ | 1.59 (0.3x) |

## Throughput (documents per second, higher is better)

| Scenario | `js` | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: | ---: |
| simple | 576.8 | 85.3 | 359.5 | 500.3 |
| table-rows-100 | 113.4 | 10.0 | 34.2 | 138.7 |
| table-rows-1000 | 12.7 | 1.2 | 3.7 | 20.8 |
| nested-loops | 34.9 | 3.1 | 10.4 | 54.9 |
| real-life-he | 33.0 | 2.6 | 10.2 | 53.8 |
| image | 150.7 | 47.3 | hung ⚠ | 575.7 |

## Tail latency (p95 ms)

| Scenario | `js` | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: | ---: |
| simple | 4.59 | 20.2 | 3.59 | 2.57 |
| table-rows-100 | 11.6 | 117 | 40.4 | 15.6 |
| table-rows-1000 | 103 | 906 | 294 | 62.0 |
| nested-loops | 36.5 | 369 | 121 | 22.5 |
| real-life-he | 41.4 | 518 | 124 | 22.4 |
| image | 10.9 | 32.8 | hung ⚠ | 2.78 |

## Cold start (fresh Node.js process, ms)

`startup` is module import + runtime initialization. The other rows are the first call of each scenario on a fresh engine (scenarios run in order, so later rows benefit from code already warmed by earlier ones).

|  | `js` | `wasm-interp` | `wasm-aot` |
| --- | ---: | ---: | ---: |
| startup | 41.7 | 178 | 303 |
| first simple | 23.5 | 303 | 166 |
| first table-rows-100 | 40.7 | 332 | 74.9 |
| first table-rows-1000 | 135 | 1156 | 343 |
| first nested-loops | 59.5 | 405 | 111 |
| first real-life-he | 41.2 | 461 | 176 |
| first image | 25.2 | 123 | 70.0 |

## Footprint

|  | `js` | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: | ---: |
| peak RSS (MB) | 210 | 212 | 246 | 215 |
| on disk (MB) | 2.9 | 10.4 | 41.3 | - |

hung ⚠: the engine did not finish within the 60000 ms timeout (see bench/README.md).
