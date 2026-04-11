using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.SamplePlugin;

public sealed class Rot13Module : IModule
{
    public string Name => "rot13";

    public string Category => "plugin";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        context.Detection.PrintableRatio >= 0.8d;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var input = Encoding.UTF8.GetString(request.Data.Span);
        var chars = input.Select(Rot13).ToArray();
        var output = Encoding.UTF8.GetBytes(chars);
        return Task.FromResult(new ModuleExecutionResult(
            Name,
            [new DataVariant(output, "ROT13 transform", new Dictionary<string, string>())],
            Array.Empty<FlagHit>(),
            Array.Empty<ExtractedArtifact>(),
            [new Insight("plugin", "rot13")]));
    }

    private static char Rot13(char value) => value switch
    {
        >= 'a' and <= 'z' => (char)('a' + ((value - 'a' + 13) % 26)),
        >= 'A' and <= 'Z' => (char)('A' + ((value - 'A' + 13) % 26)),
        _ => value
    };
}
