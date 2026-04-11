using NeuroCTF.Cli.Commands;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Infrastructure.Reporting;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class CliRouterTests
{
    public static async Task RunAsync()
    {
        var report = new AnalysisReport(
            "test",
            DateTimeOffset.UtcNow,
            new DetectionProfile(false, false, false, false, false, 0, 1, "utf-8", new Dictionary<string, string>()),
            [
                new AnalysisCandidate(
                    "1",
                    Array.Empty<byte>(),
                    [new TransformationStep("base64", "decode"), new TransformationStep("xor", "bruteforce")],
                    new CandidateScore(1, 1, 1, 0, 0, true),
                    2,
                    "flag{test}",
                    new DetectionProfile(false, false, false, false, false, 0, 1, "utf-8", new Dictionary<string, string>()),
                    Array.Empty<FlagHit>())
            ],
            Array.Empty<FlagHit>(),
            Array.Empty<ExtractedArtifact>(),
            Array.Empty<HashEntry>(),
            Array.Empty<Insight>(),
            new StreamAnalysis(0, 0, Array.Empty<FlagHit>(), Array.Empty<string>(), Array.Empty<ExtractedArtifact>(), "unknown"));

        var router = new CliRouter(
            new StubInputResolver(),
            new StubAnalysisOrchestrator(report),
            new StubSessionStore(report),
            new NeuroCTF.Application.Services.ModuleCatalog(Array.Empty<IModule>()),
            new StubModuleExecutionService(),
            new HumanReadableReportWriter(),
            new JsonReportWriter(),
            new AnalysisOptions());

        var graph = router.RenderPipelineGraph(report);
        AssertEx.Contains("[input] -> [base64] -> [xor]", graph, "Pipeline graph should render ordered module nodes");

        var analyzed = await router.AnalyzeReportAsync("ZmxhZw==", null, CancellationToken.None);
        AssertEx.Equal("test", analyzed.Source, "AnalyzeReportAsync should delegate to the orchestrator");
    }

    private sealed class StubInputResolver : IInputResolver
    {
        public Task<InputPayload> ResolveAsync(string? input, string? filePath, CancellationToken cancellationToken)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input ?? string.Empty);
            return Task.FromResult(new InputPayload(bytes, "stub", false, new StreamAnalysis(bytes.Length, 0, Array.Empty<FlagHit>(), Array.Empty<string>(), Array.Empty<ExtractedArtifact>(), "unknown")));
        }
    }

    private sealed class StubAnalysisOrchestrator(AnalysisReport report) : IAnalysisOrchestrator
    {
        public Task<AnalysisReport> AnalyzeAsync(InputPayload payload, AnalysisOptions options, CancellationToken cancellationToken) => Task.FromResult(report);
    }

    private sealed class StubSessionStore(AnalysisReport report) : IAnalysisSessionStore
    {
        public Task<string> SaveAsync(AnalysisReport report, string? requestedPath, CancellationToken cancellationToken) => Task.FromResult(requestedPath ?? "session.json");

        public Task<AnalysisReport> LoadAsync(string path, CancellationToken cancellationToken) => Task.FromResult(report);
    }

    private sealed class StubModuleExecutionService : IModuleExecutionService
    {
        public Task<ModuleExecutionResult> ExecuteAsync(IModule module, ReadOnlyMemory<byte> data, string source, IReadOnlyList<TransformationStep> history, AnalysisOptions options, int depth, CancellationToken cancellationToken) =>
            Task.FromResult(ModuleExecutionResult.Empty(module.Name));

        public Task<ModuleExecutionResult> ExecuteByNameAsync(string moduleName, ReadOnlyMemory<byte> data, string source, IReadOnlyList<TransformationStep> history, AnalysisOptions options, int depth, CancellationToken cancellationToken) =>
            Task.FromResult(ModuleExecutionResult.Empty(moduleName));
    }
}
