using System.Security.Cryptography;
using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Input;

public sealed class StreamingInspector
{
    private static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["PNG"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
        ["ZIP"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
        ["ELF"] = new byte[] { 0x7F, 0x45, 0x4C, 0x46 },
        ["GZIP"] = new byte[] { 0x1F, 0x8B }
    };

    public async Task<(byte[] Buffer, bool Truncated, StreamAnalysis StreamAnalysis)> ReadAsync(string path, int maxBufferedBytes, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await ReadAsync(stream, maxBufferedBytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(byte[] Buffer, bool Truncated, StreamAnalysis StreamAnalysis)> ReadAsync(Stream stream, int maxBufferedBytes, CancellationToken cancellationToken)
    {
        using var bufferStream = new MemoryStream();
        var buffer = new byte[8192];
        long totalBytes = 0;
        var entropySamples = new List<double>();
        var stringSet = new HashSet<string>(StringComparer.Ordinal);
        var flags = new List<FlagHit>();
        var artifacts = new List<ExtractedArtifact>();
        var fileType = "unknown";
        var chunkIndex = 0;
        var truncated = false;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            var chunk = buffer.AsSpan(0, read);
            ProcessChunk(chunk, chunkIndex * buffer.Length, entropySamples, stringSet, flags, artifacts, ref fileType);

            if (bufferStream.Length < maxBufferedBytes)
            {
                var writable = Math.Min(read, maxBufferedBytes - (int)bufferStream.Length);
                if (writable > 0)
                {
                    await bufferStream.WriteAsync(buffer.AsMemory(0, writable), cancellationToken).ConfigureAwait(false);
                }

                if (writable < read)
                {
                    truncated = true;
                }
            }
            else
            {
                truncated = true;
            }

            chunkIndex++;
        }

        return (
            bufferStream.ToArray(),
            truncated,
            new StreamAnalysis(
                totalBytes,
                entropySamples.Count == 0 ? 0d : entropySamples.Average(),
                flags.DistinctBy(flag => flag.Value).ToArray(),
                stringSet.Take(64).ToArray(),
                artifacts.DistinctBy(item => $"{item.FileType}:{item.Offset}").ToArray(),
                fileType));
    }

    public StreamAnalysis InspectBuffer(ReadOnlyMemory<byte> data)
    {
        var strings = TextAnalysisUtilities.ExtractStrings(data.Span).Take(64).ToArray();
        var flags = strings.SelectMany(value => TextAnalysisUtilities.FindFlagPatterns(value))
            .Select(match => new FlagHit(match.Value, "buffer", match.Partial, match.Partial ? 0.55d : 0.9d))
            .DistinctBy(item => item.Value)
            .ToArray();
        var bytes = data.ToArray();
        var fileType = DetectFileType(bytes) ?? "unknown";
        var artifacts = FindChunkArtifacts(bytes, 0).ToArray();

        return new StreamAnalysis(
            data.Length,
            TextAnalysisUtilities.ComputeEntropy(data.Span),
            flags,
            strings,
            artifacts,
            fileType);
    }

    private static void ProcessChunk(
        ReadOnlySpan<byte> chunk,
        int baseOffset,
        ICollection<double> entropySamples,
        ISet<string> strings,
        ICollection<FlagHit> flags,
        ICollection<ExtractedArtifact> artifacts,
        ref string fileType)
    {
        entropySamples.Add(TextAnalysisUtilities.ComputeEntropy(chunk));
        if (fileType == "unknown")
        {
            fileType = DetectFileType(chunk) ?? "unknown";
        }

        foreach (var value in TextAnalysisUtilities.ExtractStrings(chunk))
        {
            strings.Add(value);
            foreach (var flag in TextAnalysisUtilities.FindFlagPatterns(value))
            {
                flags.Add(new FlagHit(flag.Value, "stream", flag.Partial, flag.Partial ? 0.55d : 0.90d));
            }
        }

        foreach (var artifact in FindChunkArtifacts(chunk, baseOffset))
        {
            artifacts.Add(artifact);
        }
    }

    private static string? DetectFileType(ReadOnlySpan<byte> data)
    {
        foreach (var signature in Signatures)
        {
            if (data.StartsWith(signature.Value))
            {
                return signature.Key;
            }
        }

        return null;
    }

    private static IReadOnlyList<ExtractedArtifact> FindChunkArtifacts(ReadOnlySpan<byte> data, int baseOffset)
    {
        var artifacts = new List<ExtractedArtifact>();
        foreach (var signature in Signatures)
        {
            var index = data.IndexOf(signature.Value);
            if (index >= 0)
            {
                var artifactData = data[index..Math.Min(data.Length, index + 128)].ToArray();
                artifacts.Add(new ExtractedArtifact(
                    $"{signature.Key}-{baseOffset + index:x8}",
                    signature.Key,
                    baseOffset + index,
                    artifactData.Length,
                    Convert.ToHexString(SHA256.HashData(artifactData))));
            }
        }

        return artifacts;
    }
}
