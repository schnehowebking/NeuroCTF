using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class PatternOffsetModule : IModule
{
    public string Name => "pattern-offset";

    public string Category => "pwn";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length >= 3;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var needle = Encoding.ASCII.GetString(request.Data.Span).Trim();
        var pattern = PatternCreateModule.GeneratePattern(8192);
        var offset = pattern.IndexOf(needle, StringComparison.Ordinal);
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), [new Insight("pattern-offset", offset.ToString())]));
    }
}
