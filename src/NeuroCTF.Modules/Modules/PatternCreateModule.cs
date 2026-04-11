using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class PatternCreateModule : IModule
{
    public string Name => "pattern-create";

    public string Category => "pwn";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => true;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var lengthText = Encoding.ASCII.GetString(request.Data.Span).Trim();
        var length = int.TryParse(lengthText, out var parsed) ? Math.Clamp(parsed, 1, 8192) : 256;
        var pattern = GeneratePattern(length);
        return Task.FromResult(new ModuleExecutionResult(Name, [new DataVariant(Encoding.ASCII.GetBytes(pattern), $"Cyclic pattern {length}", new Dictionary<string, string>())], Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), [new Insight("pattern-length", length.ToString())]));
    }

    public static string GeneratePattern(int length)
    {
        var chars = new StringBuilder(length);
        for (var a = 'A'; a <= 'Z' && chars.Length < length; a++)
        {
            for (var b = 'a'; b <= 'z' && chars.Length < length; b++)
            {
                for (var c = '0'; c <= '9' && chars.Length < length; c++)
                {
                    chars.Append(a).Append(b).Append(c);
                }
            }
        }

        return chars.Length > length ? chars.ToString(0, length) : chars.ToString();
    }
}
