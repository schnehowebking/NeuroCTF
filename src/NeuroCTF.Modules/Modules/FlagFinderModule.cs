using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class FlagFinderModule : IModule
{
    public string Name => "flag-finder";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var normalized = TextAnalysisUtilities.NormalizeObfuscation(text);
        var hits = TextAnalysisUtilities.FindFlagPatterns(text)
            .Concat(TextAnalysisUtilities.FindFlagPatterns(normalized))
            .Select(match => new FlagHit(match.Value, "flag-finder", match.Partial, match.Partial ? 0.55d : 0.98d))
            .DistinctBy(hit => hit.Value)
            .ToArray();

        return Task.FromResult(new ModuleExecutionResult(
            Name,
            Array.Empty<DataVariant>(),
            hits,
            Array.Empty<ExtractedArtifact>(),
            hits.Select(hit => new Insight("flag-candidate", hit.Value)).ToArray()));
    }
}
