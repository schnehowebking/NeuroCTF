using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class HexTransformModule : IModule
{
    public string Name => "hex";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.LooksLikeHex;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var input = Encoding.ASCII.GetString(request.Data.Span);
        var normalized = new string(input.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length < 2 || normalized.Length % 2 != 0)
        {
            return Task.FromResult(ModuleExecutionResult.Empty(Name));
        }

        var decoded = Convert.FromHexString(normalized);
        return Task.FromResult(new ModuleExecutionResult(
            Name,
            [new DataVariant(decoded, "Decoded hexadecimal payload", new Dictionary<string, string> { ["length"] = decoded.Length.ToString() })],
            ModuleSupport.FindFlags(decoded),
            Array.Empty<ExtractedArtifact>(),
            ModuleSupport.BuildCommonInsights("hex", decoded)));
    }
}
