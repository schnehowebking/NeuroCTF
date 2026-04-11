namespace NeuroCTF.Core.Options;

public sealed class NeuroCtfOptions
{
    public int MaxDepth { get; set; } = 4;
    public int MaxCandidates { get; set; } = 64;
    public int MaxBufferedBytes { get; set; } = 8 * 1024 * 1024;
    public int MaxArtifacts { get; set; } = 16;
    public int MaxParallelModules { get; set; } = Environment.ProcessorCount;
    public int MaxBruteforceResults { get; set; } = 12;
    public int ArchiveRecursionDepth { get; set; } = 2;
    public double MinimumCandidateScore { get; set; } = 0.15d;
    public string PluginDirectory { get; set; } = "plugins";
    public string SessionsDirectory { get; set; } = "sessions";
    public List<string> CommonMultiByteKeys { get; set; } = new()
    {
        "key",
        "ctf",
        "flag",
        "neuro",
        "secret",
        "xor"
    };
}
