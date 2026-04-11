using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class ScoringService : IScoringService
{
    public CandidateScore Score(ReadOnlyMemory<byte> data, IReadOnlyList<TransformationStep> steps, IReadOnlyList<FlagHit> flags)
    {
        var printable = TextAnalysisUtilities.ComputePrintableRatio(data.Span);
        var entropy = TextAnalysisUtilities.ComputeEntropy(data.Span);
        var preview = Encoding.UTF8.GetString(data.Span[..Math.Min(2048, data.Length)]);
        var english = TextAnalysisUtilities.ComputeEnglishLikelihood(preview);
        var baselineEntropy = steps.Count == 0 ? entropy : Math.Max(0d, entropy - 0.5d);
        var entropyDelta = baselineEntropy - entropy;
        var containsFlag = flags.Any(hit => !hit.IsPartial) || TextAnalysisUtilities.FindFlagPatterns(preview).Any();
        var score = (printable * 0.35d) + (english * 0.35d) + (containsFlag ? 0.4d : 0d) + (Math.Clamp(4.5d - entropy, 0d, 4.5d) / 4.5d * 0.15d) + (Math.Clamp(entropyDelta + 1.5d, 0d, 2d) / 2d * 0.15d);
        return new CandidateScore(Math.Min(1d, score), printable, english, entropy, entropyDelta, containsFlag);
    }
}
