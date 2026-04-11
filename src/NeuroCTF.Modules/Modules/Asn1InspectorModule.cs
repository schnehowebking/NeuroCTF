using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class Asn1InspectorModule : IModule
{
    public string Name => "asn1";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length > 4 && data.Span[0] is 0x30 or 0x31 or 0x02;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        Parse(request.Data.Span, 0, 0, insights, 12);
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }

    private static int Parse(ReadOnlySpan<byte> data, int offset, int depth, ICollection<Insight> insights, int budget)
    {
        while (offset + 2 <= data.Length && budget-- > 0)
        {
            var tag = data[offset++];
            var length = (int)data[offset++];
            if ((length & 0x80) != 0)
            {
                var count = length & 0x7F;
                if (offset + count > data.Length)
                {
                    return data.Length;
                }

                length = 0;
                for (var i = 0; i < count; i++)
                {
                    length = (length << 8) | data[offset++];
                }
            }

            if (offset + length > data.Length)
            {
                return data.Length;
            }

            insights.Add(new Insight("asn1-node", $"depth={depth} tag=0x{tag:x2} len={length}"));
            if ((tag & 0x20) != 0)
            {
                Parse(data[offset..(offset + length)], 0, depth + 1, insights, budget);
            }

            offset += length;
        }

        return offset;
    }
}
