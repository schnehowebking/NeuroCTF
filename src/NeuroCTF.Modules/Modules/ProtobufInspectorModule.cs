using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class ProtobufInspectorModule : IModule
{
    public string Name => "protobuf";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length > 2 && context.Detection.Entropy > 1.5d;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var insights = new List<Insight>();
        var span = request.Data.Span;
        var offset = 0;
        var count = 0;
        while (offset < span.Length && count < 16)
        {
            if (!TryReadVarint(span, ref offset, out var tag))
            {
                break;
            }

            var fieldNumber = (int)(tag >> 3);
            var wireType = (int)(tag & 0x07);
            insights.Add(new Insight("protobuf-field", $"field={fieldNumber} wire={wireType}"));
            count++;

            switch (wireType)
            {
                case 0:
                    _ = TryReadVarint(span, ref offset, out _);
                    break;
                case 1:
                    offset = Math.Min(span.Length, offset + 8);
                    break;
                case 2:
                    if (TryReadVarint(span, ref offset, out var length))
                    {
                        offset = Math.Min(span.Length, offset + (int)length);
                    }
                    break;
                case 5:
                    offset = Math.Min(span.Length, offset + 4);
                    break;
                default:
                    offset = span.Length;
                    break;
            }
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }

    private static bool TryReadVarint(ReadOnlySpan<byte> span, ref int offset, out ulong value)
    {
        value = 0;
        var shift = 0;
        while (offset < span.Length && shift < 64)
        {
            var current = span[offset++];
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                return true;
            }

            shift += 7;
        }

        return false;
    }
}
