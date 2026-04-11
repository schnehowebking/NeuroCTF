using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class FileTypeModule : IModule
{
    private static readonly IReadOnlyDictionary<string, byte[]> MagicBytes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
        ["zip"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
        ["elf"] = new byte[] { 0x7F, 0x45, 0x4C, 0x46 },
        ["gif"] = new byte[] { 0x47, 0x49, 0x46, 0x38 },
        ["jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
        ["gzip"] = new byte[] { 0x1F, 0x8B }
    };

    public string Name => "filetype";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var hits = MagicBytes
            .Where(entry => request.Data.Span.StartsWith(entry.Value))
            .Select(entry => new Insight("filetype", entry.Key))
            .ToArray();
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), hits));
    }
}
