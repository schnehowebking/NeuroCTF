using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class StringsModule : IModule
{
    public string Name => "strings";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var strings = TextAnalysisUtilities.ExtractStrings(request.Data.Span).Take(32).ToArray();
        var insights = strings.Select(value => new Insight("string", value)).ToArray();
        var flags = strings
            .SelectMany(TextAnalysisUtilities.FindFlagPatterns)
            .Select(match => new FlagHit(match.Value, "strings", match.Partial, match.Partial ? 0.55d : 0.90d))
            .ToArray();
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), flags, Array.Empty<ExtractedArtifact>(), insights));
    }
}
