# Benchmark results

- Date: 2026-09-26T08:49:26.416Z
- Node.js v22.22.2 (V8 12.4.254.21-node.39), .NET SDK 11.0.100-rc.1.26425.128
- Linux 6.18.44-fc-v37 x64, Intel(R) Xeon(R) Processor @ 2.10GHz (4 cores), 16 GB RAM
- Warmup 1000 ms, measure 3000 ms per scenario (one fresh process each), cold start = median of 5 fresh processes

Engines:

- `js`: easy-template-x 7.2.8
- `wasm-interp-net10`: Easy.Template.XCS 1.0.0 on .NET 10.0.12, wasm Mono interpreter
- `wasm-aot-net10`: Easy.Template.XCS 1.0.0 on .NET 10.0.12, wasm Mono AOT
- `wasm-interp-net11`: Easy.Template.XCS 1.0.0 on .NET 11.0.0-rc.1.26425.128, wasm Mono interpreter
- `wasm-aot-net11`: Easy.Template.XCS 1.0.0 on .NET 11.0.0-rc.1.26425.128, wasm Mono AOT
- `wasm-coreclr-net11`: Easy.Template.XCS 1.0.0 on .NET 11.0.0-rc.1.26425.128, wasm CoreCLR interpreter
- `dotnet-native-net10`: Easy.Template.XCS 1.0.0 (native .NET 10.0.12)
- `dotnet-native-net11`: Easy.Template.XCS 1.0.0 (native .NET 11.0.0)

## Steady state (median ms per document, lower is better)

Ratios are relative to `js` (the original easy-template-x).

| Scenario | `js` | `wasm-interp-net10` | `wasm-aot-net10` | `wasm-interp-net11` | `wasm-aot-net11` | `wasm-coreclr-net11` | `dotnet-native-net10` | `dotnet-native-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| simple | 1.33 | 11.0 (8.3x) | 2.47 (1.9x) | 12.1 (9.1x) | 2.78 (2.1x) | 74.4 (56.1x) | 2.05 (1.5x) | 2.06 (1.6x) |
| table-rows-100 | 8.81 | 102 (11.6x) | 26.5 (3.0x) | 111 (12.6x) | 27.8 (3.2x) | 855 (97.1x) | 6.49 (0.7x) | 6.48 (0.7x) |
| table-rows-1000 | 78.6 | 890 (11.3x) | 283 (3.6x) | 871 (11.1x) | 258 (3.3x) | 8082 (102.9x) | 49.6 (0.6x) | 51.2 (0.7x) |
| nested-loops | 28.6 | 366 (12.8x) | 92.6 (3.2x) | 342 (12.0x) | 89.7 (3.1x) | 2999 (105.0x) | 18.4 (0.6x) | 18.5 (0.6x) |
| real-life-he | 28.9 | 320 (11.1x) | 93.6 (3.2x) | 332 (11.5x) | 93.1 (3.2x) | 2999 (103.6x) | 20.4 (0.7x) | 19.8 (0.7x) |
| image | 6.39 | 19.7 (3.1x) | hung ⚠ | 22.7 (3.5x) | 6.13 (1.0x) | 275 (43.0x) | 1.64 (0.3x) | 1.62 (0.3x) |

## Throughput (documents per second, higher is better)

| Scenario | `js` | `wasm-interp-net10` | `wasm-aot-net10` | `wasm-interp-net11` | `wasm-aot-net11` | `wasm-coreclr-net11` | `dotnet-native-net10` | `dotnet-native-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| simple | 592.2 | 79.5 | 375.4 | 72.6 | 344.3 | 13.4 | 447.2 | 465.9 |
| table-rows-100 | 112.1 | 9.6 | 36.5 | 8.9 | 34.9 | 1.2 | 123.2 | 131.6 |
| table-rows-1000 | 12.4 | 1.1 | 3.5 | 1.2 | 3.9 | 0.1 | 19.7 | 18.6 |
| nested-loops | 33.1 | 2.8 | 10.8 | 2.9 | 11.0 | 0.3 | 52.5 | 51.1 |
| real-life-he | 33.5 | 3.1 | 10.4 | 2.9 | 10.4 | 0.3 | 47.7 | 49.1 |
| image | 139.9 | 46.7 | hung ⚠ | 41.8 | 159.6 | 3.6 | 556.5 | 548.9 |

## Tail latency (p95 ms)

| Scenario | `js` | `wasm-interp-net10` | `wasm-aot-net10` | `wasm-interp-net11` | `wasm-aot-net11` | `wasm-coreclr-net11` | `dotnet-native-net10` | `dotnet-native-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| simple | 4.48 | 20.1 | 3.83 | 22.4 | 3.67 | 80.9 | 3.69 | 2.83 |
| table-rows-100 | 11.3 | 122 | 33.1 | 134 | 36.4 | 911 | 18.3 | 14.1 |
| table-rows-1000 | 96.1 | 966 | 308 | 953 | 274 | 8278 | 60.7 | 74.8 |
| nested-loops | 40.9 | 420 | 109 | 414 | 107 | 3236 | 25.1 | 26.6 |
| real-life-he | 38.2 | 360 | 114 | 396 | 118 | 3088 | 28.7 | 25.8 |
| image | 11.5 | 34.3 | hung ⚠ | 32.6 | 8.21 | 309 | 2.85 | 2.20 |

## .NET 11 vs .NET 10 (speedup of the median, higher is better)

`1.25x` means .NET 11 generates the document 25% faster than .NET 10 (`net10 median / net11 median`).

| Scenario | `wasm-interp` | `wasm-aot` | `dotnet-native` |
| --- | ---: | ---: | ---: |
| simple | 0.91x | 0.89x | 1.00x |
| table-rows-100 | 0.92x | 0.96x | 1.00x |
| table-rows-1000 | 1.02x | 1.09x | 0.97x |
| nested-loops | 1.07x | 1.03x | 0.99x |
| real-life-he | 0.96x | 1.01x | 1.03x |
| image | 0.87x | hung ⚠ | 1.01x |

## Cold start (fresh Node.js process, ms)

`startup` is module import + runtime initialization. The other rows are the first call of each scenario on a fresh engine (scenarios run in order, so later rows benefit from code already warmed by earlier ones).

|  | `js` | `wasm-interp-net10` | `wasm-aot-net10` | `wasm-interp-net11` | `wasm-aot-net11` | `wasm-coreclr-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| startup | 40.1 | 192 | 287 | 185 | 290 | 320 |
| first simple | 22.2 | 276 | 164 | 277 | 165 | 460 |
| first table-rows-100 | 45.7 | 352 | 72.6 | 338 | 70.4 | 918 |
| first table-rows-1000 | 136 | 1104 | 338 | 1161 | 319 | 8266 |
| first nested-loops | 56.4 | 424 | 99.6 | 411 | 107 | 2922 |
| first real-life-he | 42.5 | 492 | 160 | 461 | 173 | 3360 |
| first image | 22.8 | 129 | 67.3 | 129 | 73.5 | 412 |

## Footprint

|  | `js` | `wasm-interp-net10` | `wasm-aot-net10` | `wasm-interp-net11` | `wasm-aot-net11` | `wasm-coreclr-net11` | `dotnet-native-net10` | `dotnet-native-net11` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| peak RSS (MB) | 205 | 215 | 243 | 218 | 251 | 280 | 214 | 203 |
| on disk (MB) | 2.9 | 10.4 | 41.3 | 10.6 | 42.9 | 12.3 | - | - |

hung ⚠: the engine did not finish within the 400000 ms timeout (see bench/README.md).
