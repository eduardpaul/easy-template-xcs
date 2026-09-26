// Native .NET reference for the Node.js benchmark. Reads the scenarios exported
// by ../bench.mjs (template path + JSON data), runs them with the same
// warmup / measure loop as lib/run-engine.mjs and prints the results as JSON.
//
// Every iteration parses the JSON data, like the WebAssembly export does.
//
//   dotnet run -c Release -- <scenarios.json> [warmupMs] [measureMs] [minIterations] [maxIterations]

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Easy.Template.XCS;

var scenariosFile = args[0];
var warmupMs = args.Length > 1 ? double.Parse(args[1]) : 1000;
var measureMs = args.Length > 2 ? double.Parse(args[2]) : 3000;
var minIterations = args.Length > 3 ? int.Parse(args[3]) : 10;
var maxIterations = args.Length > 4 ? int.Parse(args[4]) : 5000;

var scenarios = JsonNode.Parse(File.ReadAllText(scenariosFile))!.AsArray();
var handler = new TemplateHandler();
var results = new JsonObject();

foreach (var scenario in scenarios)
{
    var name = (string)scenario!["name"]!;
    var template = File.ReadAllBytes((string)scenario["templatePath"]!);
    var json = (string)scenario["json"]!;

    await TimeLoop(warmupMs, Math.Min(5, minIterations));
    GC.Collect();
    var samples = await TimeLoop(measureMs, minIterations);
    results[name] = Summarize(samples);

    async Task<List<double>> TimeLoop(double budgetMs, int min)
    {
        var list = new List<double>();
        var loop = Stopwatch.StartNew();
        while (list.Count < maxIterations && (list.Count < min || loop.Elapsed.TotalMilliseconds < budgetMs))
        {
            var begin = Stopwatch.GetTimestamp();
            await handler.ProcessAsync(template, JsonNode.Parse(json));
            list.Add(Stopwatch.GetElapsedTime(begin).TotalMilliseconds);
        }
        return list;
    }
}

var output = new JsonObject
{
    ["engine"] = $"dotnet-native-net{Environment.Version.Major}",
    ["version"] = $"Easy.Template.XCS {TemplateHandler.Version.Split('+')[0]} (native .NET {Environment.Version})",
    ["scenarios"] = results,
    ["memory"] = new JsonObject
    {
        ["peakRssMb"] = Process.GetCurrentProcess().PeakWorkingSet64 / 1024.0 / 1024.0
    }
};
Console.WriteLine(output.ToJsonString());

static JsonObject Summarize(List<double> samples)
{
    samples.Sort();
    var n = samples.Count;
    var mean = samples.Average();
    var variance = samples.Sum(x => (x - mean) * (x - mean)) / Math.Max(1, n - 1);
    return new JsonObject
    {
        ["n"] = n,
        ["mean"] = mean,
        ["median"] = Quantile(0.5),
        ["p95"] = Quantile(0.95),
        ["min"] = samples[0],
        ["max"] = samples[n - 1],
        ["stddev"] = Math.Sqrt(variance),
        ["opsPerSec"] = 1000 / mean
    };

    double Quantile(double q)
    {
        var pos = (n - 1) * q;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return samples[lo] + (samples[hi] - samples[lo]) * (pos - lo);
    }
}
