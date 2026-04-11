using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class RopGadgetLiteModule : IModule
{
    public string Name => "rop-lite";

    public string Category => "pwn";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 1;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        var span = request.Data.Span;
        for (var i = 0; i < span.Length - 1 && insights.Count < 16; i++)
        {
            if (span[i] == 0x5F && span[i + 1] == 0xC3)
            {
                insights.Add(new Insight("rop-gadget", $"0x{i:x}: pop rdi; ret"));
            }
            else if (span[i] == 0xC3)
            {
                insights.Add(new Insight("rop-gadget", $"0x{i:x}: ret"));
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
