using System.Buffers.Binary;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class PcapInspectorModule : IModule
{
    public string Name => "pcap";

    public string Category => "network";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) =>
        data.Length >= 24 && data.Span[..4] is [0xD4, 0xC3, 0xB2, 0xA1] or [0xA1, 0xB2, 0xC3, 0xD4];

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var span = request.Data.Span;
        var littleEndian = span[0] == 0xD4;
        var offset = 24;
        var packets = 0;
        var http = 0;
        var dns = 0;
        while (offset + 16 <= span.Length && packets < 256)
        {
            var capturedLength = littleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4))
                : BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset + 8, 4));
            offset += 16;
            if (offset + capturedLength > span.Length)
            {
                break;
            }

            var payload = span.Slice(offset, (int)capturedLength);
            if (payload.IndexOf("HTTP"u8) >= 0 || payload.IndexOf("GET "u8) >= 0)
            {
                http++;
            }

            if (payload.IndexOf("dns"u8) >= 0 || payload.IndexOf(new byte[] { 0x00, 0x35 }) >= 0)
            {
                dns++;
            }

            packets++;
            offset += (int)capturedLength;
        }

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), [new Insight("pcap-packets", packets.ToString()), new Insight("http-packets", http.ToString()), new Insight("dns-indicators", dns.ToString())]));
    }
}
