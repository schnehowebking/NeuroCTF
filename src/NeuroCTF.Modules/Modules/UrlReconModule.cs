using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class UrlReconModule : IModule
{
    public string Name => "url-recon";

    public string Category => "osint";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.UTF8.GetString(data.Span[..Math.Min(data.Length, 2048)]);
        return Uri.TryCreate(text.Trim(), UriKind.Absolute, out _);
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span).Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(ModuleExecutionResult.Empty(Name));
        }

        IReadOnlyList<Insight> insights =
        [
            new("scheme", uri.Scheme),
            new("host", uri.Host),
            new("port", uri.Port.ToString()),
            new("path", uri.AbsolutePath),
            new("query", uri.Query),
            new("segment-count", uri.Segments.Length.ToString())
        ];

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
