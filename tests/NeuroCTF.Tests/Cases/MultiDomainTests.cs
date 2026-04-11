using System.Text;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class MultiDomainTests
{
    public static async Task RunAsync()
    {
        var detection = new DetectionService();
        var options = new AnalysisOptions();

        var urlRecon = new UrlReconModule();
        var urlInput = Encoding.UTF8.GetBytes("https://ctf.example.com/challenge?id=42");
        var urlResult = await urlRecon.ProcessAsync(new ModuleExecutionRequest(urlInput, new ModuleExecutionContext(0, "test", detection.Detect(urlInput), options), []), CancellationToken.None);
        AssertEx.True(urlResult.Insights.Any(i => i.Title == "host" && i.Value == "ctf.example.com"), "URL recon should parse host");

        var patternOffset = new PatternOffsetModule();
        var offsetInput = Encoding.ASCII.GetBytes("Aa0Aa1");
        var offsetResult = await patternOffset.ProcessAsync(new ModuleExecutionRequest(offsetInput, new ModuleExecutionContext(0, "test", detection.Detect(offsetInput), options), []), CancellationToken.None);
        AssertEx.True(offsetResult.Insights.Any(i => i.Title == "pattern-offset" && i.Value == "0"), "Pattern offset helper should locate exact match");

        var elfSec = new ElfSecurityCheckModule();
        var elfBytes = Encoding.ASCII.GetBytes("\x7FELFGNU_STACKGNU_RELRODYN");
        var elfResult = await elfSec.ProcessAsync(new ModuleExecutionRequest(elfBytes, new ModuleExecutionContext(0, "test", detection.Detect(elfBytes), options), []), CancellationToken.None);
        AssertEx.True(elfResult.Insights.Any(i => i.Title == "relro"), "ELF security module should emit checks");

        var pcap = new PcapInspectorModule();
        var pcapBytes = BuildMinimalPcap(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: test\r\n\r\n"));
        var pcapResult = await pcap.ProcessAsync(new ModuleExecutionRequest(pcapBytes, new ModuleExecutionContext(0, "test", detection.Detect(pcapBytes), options), []), CancellationToken.None);
        AssertEx.True(pcapResult.Insights.Any(i => i.Title == "http-packets"), "PCAP inspector should report HTTP indicators");
    }

    private static byte[] BuildMinimalPcap(byte[] payload)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0xD4, 0xC3, 0xB2, 0xA1, 0x02, 0x00, 0x04, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0, 1, 0, 0, 0 });
        ms.Write(BitConverter.GetBytes(0u));
        ms.Write(BitConverter.GetBytes(0u));
        ms.Write(BitConverter.GetBytes((uint)payload.Length));
        ms.Write(BitConverter.GetBytes((uint)payload.Length));
        ms.Write(payload);
        return ms.ToArray();
    }
}
