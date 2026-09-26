#!/usr/bin/env bash
# Builds everything the benchmark needs:
#   dist/interp  Easy.Template.XCS as WebAssembly, Mono interpreter
#   dist/aot     Easy.Template.XCS as WebAssembly, AOT compiled (needs the wasm-tools workload)
#   dotnet/bin   native .NET reference harness
#
# Prerequisites: .NET 10 SDK and `dotnet workload install wasm-tools`.
# Set SKIP_AOT=1 to skip the (slow) AOT build.
set -euo pipefail

bench_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
wasm_project="$bench_dir/../src/Easy.Template.XCS.Wasm/Easy.Template.XCS.Wasm.csproj"

export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

echo "==> WebAssembly (interpreter)"
dotnet publish "$wasm_project" -c Release -o "$bench_dir/dist/interp"

if [[ "${SKIP_AOT:-0}" != "1" ]]; then
    echo "==> WebAssembly (AOT) - this takes a few minutes"
    dotnet publish "$wasm_project" -c Release -o "$bench_dir/dist/aot" -p:RunAOTCompilation=true
fi

echo "==> Native .NET reference"
dotnet build "$bench_dir/dotnet/Easy.Template.XCS.Bench.csproj" -c Release

npm --prefix "$bench_dir" install --no-audit --no-fund
