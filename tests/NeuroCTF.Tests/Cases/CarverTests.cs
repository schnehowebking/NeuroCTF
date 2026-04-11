using NeuroCTF.Application.Services;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class CarverTests
{
    public static async Task RunAsync()
    {
        var module = new BinaryCarverModule();
        var bytes = new byte[]
        {
            0x00, 0x89, 0x50, 0x4E, 0x47, 0x11, 0x22, 0x33,
            0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };
        var detection = new DetectionService();
        var result = await module.ProcessAsync(new ModuleExecutionRequest(bytes, new ModuleExecutionContext(0, "test", detection.Detect(bytes), new AnalysisOptions()), []), CancellationToken.None);
        AssertEx.True(result.Artifacts.Any(artifact => artifact.FileType == "png"), "Carver should identify embedded PNG");
    }
}
