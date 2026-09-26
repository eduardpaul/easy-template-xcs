#!/usr/bin/env bash
# Builds everything the benchmark needs, for .NET 10 and (with SDK 11+) .NET 11:
#   dist/<tfm>/interp   Easy.Template.XCS as WebAssembly, Mono interpreter
#   dist/<tfm>/aot      Easy.Template.XCS as WebAssembly, Mono AOT (needs the wasm workloads)
#   dist/net11/coreclr  Easy.Template.XCS as WebAssembly, CoreCLR (.NET 11+)
#   dotnet/bin          native .NET reference harness
#
# Prerequisites: .NET SDK 10 or 11 and the wasm workloads:
#   dotnet workload install wasm-tools            (SDK 10: net10; SDK 11: net11)
#   dotnet workload install wasm-tools-net10      (SDK 11 only, for the net10 builds)
# Set SKIP_AOT=1 to skip the (slow) AOT builds, TFMS="net11" to build a subset.
set -euo pipefail

bench_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
wasm_project="$bench_dir/../src/Easy.Template.XCS.Wasm/Easy.Template.XCS.Wasm.csproj"

export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

sdk_major="$(cd "$bench_dir" && dotnet --version | cut -d. -f1)"
default_tfms="net10"
if (( sdk_major >= 11 )); then default_tfms="net10 net11"; fi

for tfm in ${TFMS:-$default_tfms}; do
    echo "==> $tfm WebAssembly (Mono interpreter)"
    dotnet publish "$wasm_project" -c Release -f "$tfm.0" -o "$bench_dir/dist/$tfm/interp"

    if [[ "${SKIP_AOT:-0}" != "1" ]]; then
        echo "==> $tfm WebAssembly (Mono AOT) - this takes a few minutes"
        dotnet publish "$wasm_project" -c Release -f "$tfm.0" -o "$bench_dir/dist/$tfm/aot" -p:RunAOTCompilation=true
    fi

    if [[ "$tfm" != "net10" ]]; then
        echo "==> $tfm WebAssembly (CoreCLR)"
        dotnet publish "$wasm_project" -c Release -f "$tfm.0" -o "$bench_dir/dist/$tfm/coreclr" -p:UseMonoRuntime=false
    fi
done

echo "==> Native .NET reference"
dotnet build "$bench_dir/dotnet/Easy.Template.XCS.Bench.csproj" -c Release

npm --prefix "$bench_dir" install --no-audit --no-fund
