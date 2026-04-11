using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class PeElfInspectorModule : IModule
{
    public string Name => "bin-inspect";

    public string Category => "reverse";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length >= 4;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var span = request.Data.Span;
        var insights = new List<Insight>();

        if (span.Length >= 2 && span[0] == 'M' && span[1] == 'Z')
        {
            insights.Add(new Insight("binary-format", "PE"));
            if (span.Length >= 0x40)
            {
                var peOffset = BitConverter.ToInt32(span.Slice(0x3C, 4));
                if (peOffset > 0 && peOffset + 6 < span.Length && span[peOffset] == 'P' && span[peOffset + 1] == 'E')
                {
                    var numberOfSections = BitConverter.ToUInt16(span.Slice(peOffset + 6, 2));
                    insights.Add(new Insight("pe-sections", numberOfSections.ToString()));
                }
            }
        }
        else if (span[0] == 0x7F && span[1] == (byte)'E' && span[2] == (byte)'L' && span[3] == (byte)'F')
        {
            insights.Add(new Insight("binary-format", "ELF"));
            insights.Add(new Insight("elf-class", span[4] == 2 ? "64-bit" : "32-bit"));
            insights.Add(new Insight("elf-endianness", span[5] == 1 ? "little" : "big"));
            if (span.Length >= 0x3C)
            {
                var sectionCount = span[4] == 2
                    ? BitConverter.ToUInt16(span.Slice(0x3C, 2))
                    : BitConverter.ToUInt16(span.Slice(0x30, 2));
                insights.Add(new Insight("elf-sections", sectionCount.ToString()));
            }
        }
        else
        {
            var ascii = Encoding.ASCII.GetString(span[..Math.Min(span.Length, 32)]);
            insights.Add(new Insight("binary-signature-preview", ascii));
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
