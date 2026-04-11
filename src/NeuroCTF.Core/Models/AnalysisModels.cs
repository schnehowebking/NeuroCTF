using NeuroCTF.Core.Abstractions;

namespace NeuroCTF.Core.Models;

public sealed record AnalysisOptions
{
    public int MaxDepth { get; init; } = 4;
    public int MaxCandidates { get; init; } = 64;
    public int MaxBufferedBytes { get; init; } = 8 * 1024 * 1024;
    public int MaxArtifacts { get; init; } = 16;
    public int MaxParallelModules { get; init; } = Environment.ProcessorCount;
    public int MaxBruteforceResults { get; init; } = 12;
    public int ArchiveRecursionDepth { get; init; } = 2;
    public double MinimumCandidateScore { get; init; } = 0.15d;
    public string PluginDirectory { get; init; } = "plugins";
    public string SessionsDirectory { get; init; } = "sessions";
    public IReadOnlyList<string> CommonMultiByteKeys { get; init; } = new[]
    {
        "key",
        "ctf",
        "flag",
        "neuro",
        "secret",
        "xor"
    };
}

public sealed record ModuleDescriptor(
    string Name,
    string Category,
    string Origin,
    string? AssemblyPath = null);

public sealed record PluginLoadResult(
    IReadOnlyList<IModule> Modules,
    IReadOnlyList<ModuleDescriptor> Descriptors);

public sealed record ModuleExecutionContext(int Depth, string Source, DetectionProfile Detection, AnalysisOptions Options);

public sealed record ModuleExecutionRequest(
    ReadOnlyMemory<byte> Data,
    ModuleExecutionContext Context,
    IReadOnlyList<TransformationStep> History);

public sealed record ModuleExecutionResult(
    string ModuleName,
    IReadOnlyList<DataVariant> Variants,
    IReadOnlyList<FlagHit> Flags,
    IReadOnlyList<ExtractedArtifact> Artifacts,
    IReadOnlyList<Insight> Insights)
{
    public static ModuleExecutionResult Empty(string moduleName) =>
        new(moduleName, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), Array.Empty<Insight>());
}

public sealed record DataVariant(
    ReadOnlyMemory<byte> Data,
    string Description,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record TransformationStep(
    string Module,
    string Description,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record CandidateScore(
    double Value,
    double PrintableRatio,
    double EnglishLikelihood,
    double Entropy,
    double EntropyDelta,
    bool ContainsLikelyFlag);

public sealed record AnalysisCandidate(
    string Id,
    ReadOnlyMemory<byte> Data,
    IReadOnlyList<TransformationStep> History,
    CandidateScore Score,
    int Depth,
    string Preview,
    DetectionProfile Detection,
    IReadOnlyList<FlagHit> Flags)
{
    public string Pipeline => History.Count == 0
        ? "[input]"
        : string.Join(" -> ", History.Select(step => $"[{step.Module}]"));
}

public sealed record FlagHit(
    string Value,
    string Pattern,
    bool IsPartial,
    double Confidence);

public sealed record ExtractedArtifact(
    string Name,
    string FileType,
    int Offset,
    int Length,
    string Sha256,
    string? SuggestedPath = null,
    byte[]? Payload = null);

public sealed record Insight(
    string Title,
    string Value,
    string Severity = "info");

public sealed record DetectionProfile(
    bool LooksLikeBase64,
    bool LooksLikeHex,
    bool LooksLikeUrlEncoded,
    bool LooksCompressed,
    bool LooksBinary,
    double Entropy,
    double PrintableRatio,
    string SuggestedTextEncoding,
    IReadOnlyDictionary<string, string> Hints);

public sealed record StreamAnalysis(
    long TotalBytes,
    double AverageEntropy,
    IReadOnlyList<FlagHit> Flags,
    IReadOnlyList<string> Strings,
    IReadOnlyList<ExtractedArtifact> Artifacts,
    string FileType);

public sealed record InputPayload(
    ReadOnlyMemory<byte> BufferedData,
    string Source,
    bool IsTruncated,
    StreamAnalysis StreamAnalysis);

public sealed record AnalysisReport(
    string Source,
    DateTimeOffset TimestampUtc,
    DetectionProfile InitialDetection,
    IReadOnlyList<AnalysisCandidate> Candidates,
    IReadOnlyList<FlagHit> Flags,
    IReadOnlyList<ExtractedArtifact> Artifacts,
    IReadOnlyList<HashEntry> Hashes,
    IReadOnlyList<Insight> Insights,
    StreamAnalysis StreamAnalysis)
{
    public IReadOnlyList<AnalysisCandidate> TopCandidates => Candidates.Take(10).ToArray();
}

public sealed record HashEntry(string Algorithm, string Value);

public sealed class AnalysisSession
{
    public required AnalysisReport Report { get; init; }

    public required string FileVersion { get; init; }
}
