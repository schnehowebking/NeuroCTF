using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class CaesarTransformModule : IModule
{
    public string Name => "caesar";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.PrintableRatio >= 0.7d && !context.Detection.LooksBinary;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var input = Encoding.UTF8.GetString(request.Data.Span);
        var results = new List<(string Text, int Shift, double Score)>();
        for (var shift = 1; shift <= 25; shift++)
        {
            var decoded = Shift(input, shift);
            var score = TextAnalysisUtilities.ComputeEnglishLikelihood(decoded);
            results.Add((decoded, shift, score));
        }

        var variants = results
            .OrderByDescending(item => item.Score)
            .Take(request.Context.Options.MaxBruteforceResults)
            .Select(item => new DataVariant(Encoding.UTF8.GetBytes(item.Text), $"Caesar shift {item.Shift}", new Dictionary<string, string> { ["shift"] = item.Shift.ToString(), ["english"] = item.Score.ToString("0.000") }))
            .ToArray();

        return Task.FromResult(new ModuleExecutionResult(Name, variants, variants.SelectMany(item => ModuleSupport.FindFlags(item.Data.Span)).ToArray(), Array.Empty<ExtractedArtifact>(), variants.SelectMany(item => ModuleSupport.BuildCommonInsights("caesar", item.Data.Span)).ToArray()));
    }

    private static string Shift(string input, int shift)
    {
        var chars = input.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = chars[index] switch
            {
                >= 'a' and <= 'z' => (char)('a' + (chars[index] - 'a' - shift + 26) % 26),
                >= 'A' and <= 'Z' => (char)('A' + (chars[index] - 'A' - shift + 26) % 26),
                _ => chars[index]
            };
        }

        return new string(chars);
    }
}
