using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class UrlDecodeTransformModule : IModule
{
    public string Name => "url";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.LooksLikeUrlEncoded;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var input = Encoding.UTF8.GetString(request.Data.Span);
        var decodedText = Uri.UnescapeDataString(input.Replace('+', ' '));
        var decoded = Encoding.UTF8.GetBytes(decodedText);
        return Task.FromResult(new ModuleExecutionResult(
            Name,
            [new DataVariant(decoded, "URL-decoded payload", new Dictionary<string, string>())],
            ModuleSupport.FindFlags(decoded),
            Array.Empty<ExtractedArtifact>(),
            ModuleSupport.BuildCommonInsights("url", decoded)));
    }
}
