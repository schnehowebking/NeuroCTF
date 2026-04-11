using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Modules.Modules;

public sealed class StegoLsbModule : IModule
{
    public string Name => "stego-lsb";

    public string Category => "decode";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length >= 16;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var source = request.Data.Span;
        var output = new byte[source.Length / 8];
        for (var i = 0; i < output.Length; i++)
        {
            byte value = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (byte)((value << 1) | (source[(i * 8) + bit] & 0x01));
            }

            output[i] = value;
        }

        return Task.FromResult(new ModuleExecutionResult(
            Name,
            [new DataVariant(output, "Extracted LSB bitstream", new Dictionary<string, string>())],
            ModuleSupport.FindFlags(output),
            Array.Empty<ExtractedArtifact>(),
            ModuleSupport.BuildCommonInsights("stego-lsb", output)));
    }
}
