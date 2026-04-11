using System.Text;
using System.Text.RegularExpressions;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed partial class ParameterDiscoveryModule : IModule
{
    public string Name => "param-discovery";

    public string Category => "web";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.UTF8.GetString(data.Span[..Math.Min(data.Length, 2048)]);
        return text.Contains('?') || text.Contains('=') || text.Contains('&');
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var insights = ParamRegex().Matches(text)
            .Select(match => new Insight("parameter", match.Groups[1].Value))
            .DistinctBy(insight => insight.Value)
            .Take(32)
            .ToArray();

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }

    [GeneratedRegex(@"(?:\?|&|^)([A-Za-z0-9_\-\.]+)=", RegexOptions.IgnoreCase)]
    private static partial Regex ParamRegex();
}
