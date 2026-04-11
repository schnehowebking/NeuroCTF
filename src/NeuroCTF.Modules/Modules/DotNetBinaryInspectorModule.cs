using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class DotNetBinaryInspectorModule : IModule
{
    public string Name => "dotnet-inspect";

    public string Category => "reverse";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.ASCII.GetString(data.Span[..Math.Min(data.Length, 4096)]);
        return text.Contains("BSJB", StringComparison.Ordinal) || text.Contains("mscoree.dll", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.ASCII.GetString(request.Data.Span[..Math.Min(request.Data.Length, 8192)]);
        var insights = new List<Insight>
        {
            new("dotnet-binary", "true")
        };

        foreach (var token in new[] { "mscoree.dll", "System.", "Microsoft.", "BSJB", "<Module>" })
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                insights.Add(new Insight("dotnet-indicator", token));
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
