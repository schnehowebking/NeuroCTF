using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class HttpRequestReplayModule : IModule
{
    public string Name => "http-replay";

    public string Category => "web";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.UTF8.GetString(data.Span[..Math.Min(data.Length, 1024)]);
        return text.Contains("HTTP/1.1", StringComparison.OrdinalIgnoreCase) || text.StartsWith("GET ", StringComparison.OrdinalIgnoreCase) || text.StartsWith("POST ", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var firstLine = text.Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var insights = new List<Insight> { new("http-request", firstLine) };
        foreach (var line in text.Split('\n').Where(line => line.Contains(':')).Take(8))
        {
            insights.Add(new Insight("http-header", line.Trim()));
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
