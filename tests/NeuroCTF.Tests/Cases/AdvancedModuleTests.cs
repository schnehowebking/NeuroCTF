using System.IO.Compression;
using System.Text;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Models;
using NeuroCTF.Modules.Modules;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class AdvancedModuleTests
{
    public static async Task RunAsync()
    {
        var detection = new DetectionService();
        var options = new AnalysisOptions();

        var base32 = new Base32TransformModule();
        var base32Input = Encoding.ASCII.GetBytes("MZXW6===");
        var base32Result = await base32.ProcessAsync(new ModuleExecutionRequest(base32Input, new ModuleExecutionContext(0, "test", detection.Detect(base32Input), options), []), CancellationToken.None);
        AssertEx.Contains("foo", Encoding.UTF8.GetString(base32Result.Variants[0].Data.Span), "Base32 decoder should decode payload");

        var jwt = new JwtInspectModule();
        var jwtInput = Encoding.ASCII.GetBytes("eyJhbGciOiJub25lIn0.eyJmbGFnIjoiZmxhZ3tqd3R9In0.");
        var jwtResult = await jwt.ProcessAsync(new ModuleExecutionRequest(jwtInput, new ModuleExecutionContext(0, "test", detection.Detect(jwtInput), options), []), CancellationToken.None);
        AssertEx.True(jwtResult.Insights.Any(insight => insight.Value.Contains("flag", StringComparison.OrdinalIgnoreCase)), "JWT inspector should surface payload fields");

        var archiveModule = new ArchiveTraversalModule();
        using var zipMemory = new MemoryStream();
        using (var archive = new ZipArchive(zipMemory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("flag.txt");
            await using var entryStream = entry.Open();
            var content = Encoding.UTF8.GetBytes("flag{zip}");
            await entryStream.WriteAsync(content);
        }

        var archiveBytes = zipMemory.ToArray();
        var archiveResult = await archiveModule.ProcessAsync(new ModuleExecutionRequest(archiveBytes, new ModuleExecutionContext(0, "test", detection.Detect(archiveBytes), options), []), CancellationToken.None);
        AssertEx.True(archiveResult.Artifacts.Any(artifact => artifact.Name == "flag.txt"), "Archive traversal should surface embedded entries");
    }
}
