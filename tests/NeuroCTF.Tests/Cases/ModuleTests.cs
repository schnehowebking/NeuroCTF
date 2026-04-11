using NeuroCTF.Application.Services;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class ModuleTests
{
    public static async Task RunAsync()
    {
        var options = new AnalysisOptions();
        var detection = new DetectionService();

        var base64 = new Base64TransformModule();
        var base64Input = System.Text.Encoding.ASCII.GetBytes("ZmxhZ3t0ZXN0fQ==");
        var base64Result = await base64.ProcessAsync(new ModuleExecutionRequest(base64Input, new ModuleExecutionContext(0, "test", detection.Detect(base64Input), options), []), CancellationToken.None);
        AssertEx.True(base64Result.Variants.Count > 0, "Base64 decoder should emit variants");
        AssertEx.Contains("flag{test}", System.Text.Encoding.UTF8.GetString(base64Result.Variants[0].Data.Span), "Base64 decoder should decode payload");

        var xor = new XorTransformModule();
        var xorInput = new byte[] { (byte)('f' ^ 0x42), (byte)('l' ^ 0x42), (byte)('a' ^ 0x42), (byte)('g' ^ 0x42) };
        var xorResult = await xor.ProcessAsync(new ModuleExecutionRequest(xorInput, new ModuleExecutionContext(0, "test", detection.Detect(xorInput), options), []), CancellationToken.None);
        AssertEx.True(xorResult.Variants.Any(), "XOR module should emit brute-force candidates");
    }
}
