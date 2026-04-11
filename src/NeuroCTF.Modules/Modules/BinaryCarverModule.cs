using System.Security.Cryptography;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class BinaryCarverModule : IModule
{
    private static readonly byte[] PngStart = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] PngEnd = [0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
    private static readonly byte[] ZipStart = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] ZipEnd = [0x50, 0x4B, 0x05, 0x06];
    private static readonly byte[] ElfStart = [0x7F, 0x45, 0x4C, 0x46];

    public string Name => "extract";

    public string Category => "forensics";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 4;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var artifacts = new List<ExtractedArtifact>();
        CarveWithEndMarker(request.Data.Span, PngStart, PngEnd, "png", artifacts);
        CarveWithEndMarker(request.Data.Span, ZipStart, ZipEnd, "zip", artifacts, 22);
        CarveToEnd(request.Data.Span, ElfStart, "elf", artifacts);
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), artifacts.ToArray(), artifacts.Select(item => new Insight("artifact", $"{item.FileType}@0x{item.Offset:x}")).ToArray()));
    }

    private static void CarveWithEndMarker(ReadOnlySpan<byte> data, byte[] start, byte[] end, string fileType, ICollection<ExtractedArtifact> artifacts, int trailerLength = 0)
    {
        var searchOffset = 0;
        while (searchOffset < data.Length)
        {
            var startIndex = data[searchOffset..].IndexOf(start);
            if (startIndex < 0)
            {
                break;
            }

            startIndex += searchOffset;
            var endIndex = data[(startIndex + start.Length)..].IndexOf(end);
            if (endIndex < 0)
            {
                break;
            }

            endIndex += startIndex + start.Length + end.Length + trailerLength;
            endIndex = Math.Min(endIndex, data.Length);
            var carved = data[startIndex..endIndex];
            artifacts.Add(new ExtractedArtifact($"{fileType}-{startIndex:x8}", fileType, startIndex, carved.Length, Convert.ToHexString(SHA256.HashData(carved)), $"{fileType}-{startIndex:x8}.bin", carved.ToArray()));
            searchOffset = endIndex;
        }
    }

    private static void CarveToEnd(ReadOnlySpan<byte> data, byte[] start, string fileType, ICollection<ExtractedArtifact> artifacts)
    {
        var startIndex = data.IndexOf(start);
        if (startIndex < 0)
        {
            return;
        }

        var carved = data[startIndex..];
        artifacts.Add(new ExtractedArtifact($"{fileType}-{startIndex:x8}", fileType, startIndex, carved.Length, Convert.ToHexString(SHA256.HashData(carved)), $"{fileType}-{startIndex:x8}.bin", carved.ToArray()));
    }
}
