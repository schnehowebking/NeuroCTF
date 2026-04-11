using Microsoft.Extensions.Logging.Abstractions;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;

var modules = new IModule[]
{
    new Base64TransformModule(),
    new Base32TransformModule(),
    new HexTransformModule(),
    new UrlDecodeTransformModule(),
    new CaesarTransformModule(),
    new XorTransformModule(),
    new GzipTransformModule(),
    new JwtInspectModule(),
    new ProtobufInspectorModule(),
    new Asn1InspectorModule(),
    new StegoLsbModule(),
    new ArchiveTraversalModule(),
    new BinaryCarverModule(),
    new FlagFinderModule(),
    new StringsModule()
};

var detection = new DetectionService();
var scoring = new ScoringService();
var catalog = new ModuleCatalog(modules);
var executor = new ModuleExecutionService(catalog, detection, NullLogger<ModuleExecutionService>.Instance);
var pipeline = new PipelineEngine(catalog, detection, scoring, executor, NullLogger<PipelineEngine>.Instance);

var random = new Random(1337);
for (var iteration = 0; iteration < 200; iteration++)
{
    var length = random.Next(1, 4096);
    var buffer = new byte[length];
    random.NextBytes(buffer);

    try
    {
        var payload = new InputPayload(buffer, $"fuzz-{iteration}", false, new StreamAnalysis(buffer.Length, 0, Array.Empty<FlagHit>(), Array.Empty<string>(), Array.Empty<ExtractedArtifact>(), "unknown"));
        _ = await pipeline.ExecuteAsync(payload, new AnalysisOptions(), CancellationToken.None).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Fuzz failure at iteration {iteration}: {ex}");
        return 1;
    }
}

Console.WriteLine("Fuzz run completed without unhandled exceptions.");
return 0;
