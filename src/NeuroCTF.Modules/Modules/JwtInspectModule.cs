using System.Text;
using System.Text.Json;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class JwtInspectModule : IModule
{
    public string Name => "jwt";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.UTF8.GetString(data.Span);
        return text.Count(ch => ch == '.') is 2;
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var parts = text.Split('.');
        if (parts.Length != 3)
        {
            return Task.FromResult(ModuleExecutionResult.Empty(Name));
        }

        var variants = new List<DataVariant>();
        var insights = new List<Insight>();
        foreach (var (part, index) in parts.Take(2).Select((part, index) => (part, index)))
        {
            try
            {
                var decoded = DecodeBase64Url(part);
                variants.Add(new DataVariant(decoded, index == 0 ? "JWT header" : "JWT payload", new Dictionary<string, string>()));
                var json = JsonDocument.Parse(decoded);
                foreach (var property in json.RootElement.EnumerateObject())
                {
                    insights.Add(new Insight(index == 0 ? "jwt-header" : "jwt-payload", $"{property.Name}={property.Value}"));
                }
            }
            catch
            {
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, variants, variants.SelectMany(v => ModuleSupport.FindFlags(v.Data.Span)).ToArray(), Array.Empty<ExtractedArtifact>(), insights));
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var normalized = input.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        return Convert.FromBase64String(normalized);
    }
}
