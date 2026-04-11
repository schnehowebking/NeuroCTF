using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class ElfSecurityCheckModule : IModule
{
    public string Name => "elf-sec";

    public string Category => "pwn";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length >= 4 && data.Span[0] == 0x7F && data.Span[1] == (byte)'E' && data.Span[2] == (byte)'L' && data.Span[3] == (byte)'F';

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = System.Text.Encoding.ASCII.GetString(request.Data.Span[..Math.Min(request.Data.Length, 16384)]);
        var insights = new List<Insight>
        {
            new("nx", text.Contains("GNU_STACK", StringComparison.OrdinalIgnoreCase) ? "likely" : "unknown"),
            new("pie", text.Contains("DYN", StringComparison.OrdinalIgnoreCase) ? "likely" : "unknown"),
            new("relro", text.Contains("GNU_RELRO", StringComparison.OrdinalIgnoreCase) ? "partial/likely" : "unknown")
        };
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
