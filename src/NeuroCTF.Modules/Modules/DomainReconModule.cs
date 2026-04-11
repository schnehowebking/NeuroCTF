using System.Text;
using System.Text.RegularExpressions;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed partial class DomainReconModule : IModule
{
    public string Name => "domain-recon";

    public string Category => "osint";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.UTF8.GetString(data.Span[..Math.Min(data.Length, 2048)]);
        return DomainRegex().IsMatch(text);
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var insights = DomainRegex().Matches(text)
            .Select(match =>
            {
                var parts = match.Value.Split('.');
                return new[]
                {
                    new Insight("domain", match.Value),
                    new Insight("tld", parts[^1]),
                    new Insight("labels", parts.Length.ToString())
                };
            })
            .SelectMany(x => x)
            .Take(24)
            .ToArray();

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }

    [GeneratedRegex(@"\b(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,24}\b", RegexOptions.IgnoreCase)]
    private static partial Regex DomainRegex();
}
