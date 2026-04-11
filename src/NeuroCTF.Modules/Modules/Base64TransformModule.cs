using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class Base64TransformModule : IModule
{
    public string Name => "base64";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.LooksLikeBase64 || Encoding.ASCII.GetString(data.Span).Trim().Contains('=');

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var input = Encoding.ASCII.GetString(request.Data.Span).Trim();
        var normalized = input.Replace('-', '+').Replace('_', '/').Replace("\r", string.Empty).Replace("\n", string.Empty);
        var variants = new List<DataVariant>();

        foreach (var candidate in new[] { normalized, normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=') }.Distinct())
        {
            try
            {
                var decoded = Convert.FromBase64String(candidate);
                variants.Add(new DataVariant(decoded, "Decoded Base64 payload", new Dictionary<string, string> { ["variant"] = candidate == normalized ? "raw" : "padded" }));
            }
            catch (FormatException)
            {
            }
        }

        var unique = variants.DistinctBy(item => Convert.ToHexString(item.Data.Span)).ToArray();
        return Task.FromResult(new ModuleExecutionResult(Name, unique, unique.SelectMany(item => ModuleSupport.FindFlags(item.Data.Span)).ToArray(), Array.Empty<ExtractedArtifact>(), unique.SelectMany(item => ModuleSupport.BuildCommonInsights("base64", item.Data.Span)).ToArray()));
    }
}
