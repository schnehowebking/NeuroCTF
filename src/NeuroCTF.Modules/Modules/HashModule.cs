using System.Security.Cryptography;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class HashModule : IModule
{
    public string Name => "hash";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Insight> insights =
        [
            new("md5", Convert.ToHexString(MD5.HashData(request.Data.Span))),
            new("sha1", Convert.ToHexString(SHA1.HashData(request.Data.Span))),
            new("sha256", Convert.ToHexString(SHA256.HashData(request.Data.Span)))
        ];

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
