using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class DisassemblyLiteModule : IModule
{
    private static readonly Dictionary<byte, string> Opcodes = new()
    {
        [0x55] = "push rbp",
        [0x48] = "rex.w",
        [0x89] = "mov",
        [0x8B] = "mov",
        [0xE8] = "call",
        [0xE9] = "jmp",
        [0x74] = "jz",
        [0x75] = "jnz",
        [0xC3] = "ret",
        [0x90] = "nop"
    };

    public string Name => "disasm-lite";

    public string Category => "reverse";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        var span = request.Data.Span[..Math.Min(request.Data.Length, 64)];
        for (var i = 0; i < span.Length && insights.Count < 16; i++)
        {
            if (Opcodes.TryGetValue(span[i], out var instruction))
            {
                insights.Add(new Insight("instruction", $"0x{i:x4}: {instruction}"));
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
