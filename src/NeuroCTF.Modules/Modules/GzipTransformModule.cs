using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class GzipTransformModule : IModule
{
    public string Name => "gzip";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.LooksCompressed || ModuleSupport.LooksGzip(data.Span);

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var decoded = ModuleSupport.Gunzip(request.Data.Span);
            return Task.FromResult(new ModuleExecutionResult(
                Name,
                [new DataVariant(decoded, "GZip decompressed payload", new Dictionary<string, string>())],
                ModuleSupport.FindFlags(decoded),
                Array.Empty<ExtractedArtifact>(),
                ModuleSupport.BuildCommonInsights("gzip", decoded)));
        }
        catch
        {
            return Task.FromResult(ModuleExecutionResult.Empty(Name));
        }
    }
}
