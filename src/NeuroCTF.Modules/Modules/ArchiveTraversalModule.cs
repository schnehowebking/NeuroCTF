using System.IO.Compression;
using System.Security.Cryptography;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class ArchiveTraversalModule : IModule
{
    public string Name => "archive-traversal";

    public string Category => "forensics";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        IsZip(data.Span) || ModuleSupport.LooksGzip(data.Span);

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var variants = new List<DataVariant>();
        var artifacts = new List<ExtractedArtifact>();
        var flags = new List<FlagHit>();
        Traverse(request.Data.ToArray(), "root", 0, request.Context.Options.ArchiveRecursionDepth, variants, artifacts, flags);
        return Task.FromResult(new ModuleExecutionResult(Name, variants, flags, artifacts, artifacts.Select(a => new Insight("archive-entry", a.Name)).ToArray()));
    }

    private static void Traverse(
        byte[] data,
        string name,
        int depth,
        int maxDepth,
        ICollection<DataVariant> variants,
        ICollection<ExtractedArtifact> artifacts,
        ICollection<FlagHit> flags)
    {
        if (depth > maxDepth)
        {
            return;
        }

        if (ModuleSupport.LooksGzip(data))
        {
            try
            {
                var decompressed = ModuleSupport.Gunzip(data);
                variants.Add(new DataVariant(decompressed, $"Archive entry {name} (gzip)", new Dictionary<string, string> { ["depth"] = depth.ToString() }));
                artifacts.Add(new ExtractedArtifact($"{name}.gunzip", "gzip-entry", 0, decompressed.Length, Convert.ToHexString(SHA256.HashData(decompressed)), name, decompressed));
                foreach (var flag in ModuleSupport.FindFlags(decompressed))
                {
                    flags.Add(flag);
                }
            }
            catch
            {
            }

            return;
        }

        if (!IsZip(data))
        {
            return;
        }

        using var memory = new MemoryStream(data, writable: false);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            using var entryStream = entry.Open();
            using var entryMemory = new MemoryStream();
            entryStream.CopyTo(entryMemory);
            var bytes = entryMemory.ToArray();
            variants.Add(new DataVariant(bytes, $"Archive entry {entry.FullName}", new Dictionary<string, string> { ["depth"] = depth.ToString() }));
            artifacts.Add(new ExtractedArtifact(entry.FullName, "zip-entry", 0, bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), entry.FullName, bytes));
            foreach (var flag in ModuleSupport.FindFlags(bytes))
            {
                flags.Add(flag);
            }

            if (depth < maxDepth && (IsZip(bytes) || ModuleSupport.LooksGzip(bytes)))
            {
                Traverse(bytes, entry.FullName, depth + 1, maxDepth, variants, artifacts, flags);
            }
        }
    }

    private static bool IsZip(ReadOnlySpan<byte> data) =>
        data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04;
}
