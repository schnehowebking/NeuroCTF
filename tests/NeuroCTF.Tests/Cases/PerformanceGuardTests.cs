using NeuroCTF.Application.Services;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class PerformanceGuardTests
{
    public static async Task RunAsync()
    {
        var module = new XorTransformModule();
        var options = new AnalysisOptions
        {
            MaxBruteforceResults = 5
        };
        var detection = new DetectionService();
        var input = System.Text.Encoding.ASCII.GetBytes("flag");
        var result = await module.ProcessAsync(
            new ModuleExecutionRequest(input, new ModuleExecutionContext(0, "test", detection.Detect(input), options), []),
            CancellationToken.None);

        AssertEx.LessThanOrEqual(result.Variants.Count, 5, "XOR brute force should keep only bounded top candidates");
    }
}
