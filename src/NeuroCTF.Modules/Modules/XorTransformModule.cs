using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class XorTransformModule : IModule
{
    public string Name => "xor";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, request.Context.Options.MaxBruteforceResults);
        var topCandidates = new PriorityQueue<ScoredVariant, double>();

        for (var key = 0; key <= 255; key++)
        {
            var decoded = ModuleSupport.Xor(request.Data.Span, [(byte)key]);
            AddCandidate(
                topCandidates,
                new ScoredVariant(
                    decoded,
                    $"XOR single-byte key 0x{key:x2}",
                    new Dictionary<string, string> { ["key"] = $"0x{key:x2}" },
                    ScoreCandidate(decoded)),
                limit);
        }

        foreach (var multiKey in request.Context.Options.CommonMultiByteKeys.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var keyBytes = Encoding.UTF8.GetBytes(multiKey);
            var decoded = ModuleSupport.Xor(request.Data.Span, keyBytes);
            AddCandidate(
                topCandidates,
                new ScoredVariant(
                    decoded,
                    $"XOR multi-byte key '{multiKey}'",
                    new Dictionary<string, string> { ["key"] = multiKey },
                    ScoreCandidate(decoded)),
                limit);
        }

        var selected = topCandidates.UnorderedItems
            .Select(item => item.Element)
            .OrderByDescending(item => item.Score)
            .Select(item => new DataVariant(item.Data, item.Description, item.Metadata))
            .ToArray();

        return Task.FromResult(new ModuleExecutionResult(Name, selected, selected.SelectMany(item => ModuleSupport.FindFlags(item.Data.Span)).ToArray(), Array.Empty<ExtractedArtifact>(), selected.SelectMany(item => ModuleSupport.BuildCommonInsights("xor", item.Data.Span)).ToArray()));
    }

    private static double ScoreCandidate(byte[] decoded)
    {
        var preview = Encoding.UTF8.GetString(decoded, 0, Math.Min(decoded.Length, 1024));
        var score = TextAnalysisUtilities.ComputeEnglishLikelihood(preview);
        if (ModuleSupport.FindFlags(decoded).Any())
        {
            score += 1d;
        }

        return score;
    }

    private static void AddCandidate(PriorityQueue<ScoredVariant, double> queue, ScoredVariant candidate, int limit)
    {
        queue.Enqueue(candidate, candidate.Score);
        if (queue.Count > limit)
        {
            queue.Dequeue();
        }
    }

    private sealed record ScoredVariant(byte[] Data, string Description, Dictionary<string, string> Metadata, double Score);
}
