using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class Base32TransformModule : IModule
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string Name => "base32";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context)
    {
        var text = Encoding.ASCII.GetString(data.Span).Trim().Replace("=", string.Empty, StringComparison.Ordinal);
        return text.Length >= 8 && text.All(ch => Alphabet.Contains(char.ToUpperInvariant(ch)));
    }

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.ASCII.GetString(request.Data.Span).Trim().TrimEnd('=').ToUpperInvariant();
        try
        {
            var decoded = Decode(text);
            return Task.FromResult(new ModuleExecutionResult(
                Name,
                [new DataVariant(decoded, "Decoded Base32 payload", new Dictionary<string, string>())],
                ModuleSupport.FindFlags(decoded),
                Array.Empty<ExtractedArtifact>(),
                ModuleSupport.BuildCommonInsights("base32", decoded)));
        }
        catch
        {
            return Task.FromResult(ModuleExecutionResult.Empty(Name));
        }
    }

    private static byte[] Decode(string input)
    {
        var output = new List<byte>(input.Length * 5 / 8);
        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var ch in input)
        {
            var index = Alphabet.IndexOf(ch);
            if (index < 0)
            {
                continue;
            }

            bitBuffer = (bitBuffer << 5) | index;
            bitsInBuffer += 5;
            while (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                output.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
