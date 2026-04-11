using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;

var modules = new IModule[]
{
    new Base64TransformModule(),
    new Base32TransformModule(),
    new XorTransformModule(),
    new JwtInspectModule(),
    new ArchiveTraversalModule(),
    new FlagFinderModule(),
    new StringsModule()
};

var detection = new DetectionService();
var scoring = new ScoringService();
var catalog = new ModuleCatalog(modules);
var executor = new ModuleExecutionService(catalog, detection, NullLogger<ModuleExecutionService>.Instance);
var pipeline = new PipelineEngine(catalog, detection, scoring, executor, NullLogger<PipelineEngine>.Instance);

var payloads = new Dictionary<string, byte[]>
{
    ["base64"] = System.Text.Encoding.ASCII.GetBytes("ZmxhZ3tiZW5jaG1hcmt9"),
    ["jwt"] = System.Text.Encoding.ASCII.GetBytes("eyJhbGciOiJub25lIn0.eyJ1c2VyIjoiY3RmIiwiZmxhZyI6ImZsYWd7YmVuY2htYXJrfSJ9."),
    ["xor"] = Enumerable.Range(0, 256).Select(i => (byte)(i ^ 0x23)).ToArray()
};

foreach (var payload in payloads)
{
    var stopwatch = Stopwatch.StartNew();
    for (var i = 0; i < 50; i++)
    {
        var input = new InputPayload(payload.Value, payload.Key, false, new StreamAnalysis(payload.Value.Length, 0, Array.Empty<FlagHit>(), Array.Empty<string>(), Array.Empty<ExtractedArtifact>(), "unknown"));
        _ = await pipeline.ExecuteAsync(input, new AnalysisOptions(), CancellationToken.None).ConfigureAwait(false);
    }

    stopwatch.Stop();
    Console.WriteLine($"{payload.Key}: {stopwatch.ElapsedMilliseconds} ms / 50 runs");
}
