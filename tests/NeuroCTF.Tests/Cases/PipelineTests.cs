using Microsoft.Extensions.Logging.Abstractions;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class PipelineTests
{
    public static async Task RunAsync()
    {
        var modules = new IModule[]
        {
            new Base64TransformModule(),
            new FlagFinderModule(),
            new StringsModule()
        };

        var detection = new DetectionService();
        var scoring = new ScoringService();
        var catalog = new ModuleCatalog(modules);
        var moduleExecution = new ModuleExecutionService(catalog, detection, NullLogger<ModuleExecutionService>.Instance);
        var pipeline = new PipelineEngine(catalog, detection, scoring, moduleExecution, NullLogger<PipelineEngine>.Instance);

        var input = System.Text.Encoding.ASCII.GetBytes("ZmxhZ3twaXBlbGluZX0=");
        var payload = new InputPayload(input, "test", false, new StreamAnalysis(input.Length, 0, Array.Empty<FlagHit>(), Array.Empty<string>(), Array.Empty<ExtractedArtifact>(), "unknown"));
        var report = await pipeline.ExecuteAsync(payload, new AnalysisOptions(), CancellationToken.None);

        AssertEx.True(report.Flags.Any(hit => hit.Value.Contains("flag{pipeline}", StringComparison.OrdinalIgnoreCase)), "Pipeline should discover decoded flag");
        AssertEx.True(report.Candidates.Any(candidate => candidate.History.Any(step => step.Module == "base64")), "Pipeline should record base64 transformation");
    }
}
