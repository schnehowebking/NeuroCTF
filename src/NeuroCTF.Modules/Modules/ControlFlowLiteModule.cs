using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class ControlFlowLiteModule : IModule
{
    public string Name => "cfg-lite";

    public string Category => "reverse";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var edges = new List<Insight>();
        var span = request.Data.Span[..Math.Min(request.Data.Length, 128)];
        for (var i = 0; i < span.Length - 1 && edges.Count < 12; i++)
        {
            if (span[i] is 0xE8 or 0xE9 or 0x74 or 0x75)
            {
                edges.Add(new Insight("cfg-edge", $"0x{i:x4} -> control-transfer"));
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), edges));
    }
}
