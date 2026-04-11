using System.Text;
using NeuroCTF.Application.Services;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Infrastructure.Reporting;
using NeuroCTF.Modules.Utilities;

namespace NeuroCTF.Cli.Commands;

public sealed class CliRouter(
    IInputResolver inputResolver,
    IAnalysisOrchestrator analysisOrchestrator,
    IAnalysisSessionStore sessionStore,
    IModuleCatalog moduleCatalog,
    IModuleExecutionService moduleExecutionService,
    IReportWriter reportWriter,
    JsonReportWriter jsonReportWriter,
    AnalysisOptions options)
{
    public async Task<int> RouteAsync(string[] args, CancellationToken cancellationToken)
    {
        var command = args.Length == 0 ? "help" : args[0].ToLowerInvariant();
        var optionsMap = ParseOptions(args.Skip(1).ToArray());

        return command switch
        {
            "analyze" => await AnalyzeAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "decode" => await DecodeAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "exploit" => await ExploitAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "scan" => await ScanAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "bruteforce" => await BruteforceAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "recon" => await ReconAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "strings" => await StringsAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "xor" => await XorAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "base64" => await DecodeModuleAsync("base64", optionsMap, cancellationToken).ConfigureAwait(false),
            "hexdump" => await HexDumpAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "filetype" => await FileTypeAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "hash" => await HashAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "entropy" => await EntropyAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "extract" => await ExtractAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "export" => await ExportArtifactsAsync(optionsMap, cancellationToken).ConfigureAwait(false),
            "modules" => ModulesAsync(),
            "shell" => await LaunchShellAsync(cancellationToken).ConfigureAwait(false),
            _ => ShowHelp()
        };
    }

    public async Task<AnalysisReport> AnalyzeReportAsync(string? input, string? filePath, CancellationToken cancellationToken)
    {
        var payload = await inputResolver.ResolveAsync(input, filePath, cancellationToken).ConfigureAwait(false);
        return await analysisOrchestrator.AnalyzeAsync(payload, options, cancellationToken).ConfigureAwait(false);
    }

    public string RenderPipelineGraph(AnalysisReport report)
    {
        var lines = new List<string>();
        foreach (var candidate in report.TopCandidates.Take(10))
        {
            var nodes = candidate.History.Count == 0 ? ["input"] : candidate.History.Select(step => step.Module).Prepend("input").ToArray();
            lines.Add(string.Join(" -> ", nodes.Select(node => $"[{node}]")));
        }

        return string.Join(Environment.NewLine, lines.Distinct(StringComparer.Ordinal));
    }

    private async Task<int> AnalyzeAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        if (optionsMap.TryGetValue("replay", out var replayPath) && !string.IsNullOrWhiteSpace(replayPath))
        {
            var replay = await sessionStore.LoadAsync(replayPath, cancellationToken).ConfigureAwait(false);
            WriteReport(replay, optionsMap);
            return 0;
        }

        var report = await AnalyzeFromOptionsAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        WriteReport(report, optionsMap);

        if (optionsMap.TryGetValue("save", out var savePath))
        {
            var persisted = await sessionStore.SaveAsync(report, savePath, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Session saved: {persisted}");
        }

        return 0;
    }

    private async Task<int> StringsAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        foreach (var value in TextAnalysisUtilities.ExtractStrings(payload.BufferedData.Span))
        {
            Console.WriteLine(value);
        }

        return 0;
    }

    private async Task<int> DecodeAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var moduleName = optionsMap.TryGetValue("module", out var module) && !string.IsNullOrWhiteSpace(module)
            ? module
            : "base64";
        return await DecodeModuleAsync(moduleName, optionsMap, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> ExploitAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var moduleName = optionsMap.TryGetValue("module", out var module) && !string.IsNullOrWhiteSpace(module)
            ? module
            : "elf-sec";
        var result = await ExecuteNamedModuleAsync(moduleName, optionsMap, cancellationToken).ConfigureAwait(false);
        WriteInsights(result.Insights);
        return 0;
    }

    private async Task<int> ScanAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        await RunModuleSetAsync(CommandModuleSets.ScanModules, optionsMap, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> BruteforceAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var moduleName = optionsMap.TryGetValue("module", out var module) && !string.IsNullOrWhiteSpace(module)
            ? module
            : "xor";
        return await DecodeModuleAsync(moduleName, optionsMap, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> ReconAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        await RunModuleSetAsync(CommandModuleSets.ReconModules, optionsMap, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private async Task<int> XorAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);

        if (optionsMap.TryGetValue("key", out var key) && !string.IsNullOrWhiteSpace(key))
        {
            WriteBytes(ModuleSupport.Xor(payload.BufferedData.Span, Encoding.UTF8.GetBytes(key)), optionsMap);
            return 0;
        }

        if (optionsMap.TryGetValue("key-hex", out var keyHex) && !string.IsNullOrWhiteSpace(keyHex))
        {
            WriteBytes(ModuleSupport.Xor(payload.BufferedData.Span, Convert.FromHexString(keyHex)), optionsMap);
            return 0;
        }

        var resultSet = await ExecuteNamedModuleAsync("xor", optionsMap, cancellationToken, payload).ConfigureAwait(false);
        WriteVariants(resultSet.Variants.Take(options.MaxBruteforceResults), optionsMap);

        return 0;
    }

    private async Task<int> DecodeModuleAsync(string moduleName, Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var result = await ExecuteNamedModuleAsync(moduleName, optionsMap, cancellationToken).ConfigureAwait(false);
        WriteVariants(result.Variants, optionsMap);

        return 0;
    }

    private async Task<int> HexDumpAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        Console.Write(ByteFormatting.ToHexDump(payload.BufferedData.Span));
        return 0;
    }

    private async Task<int> FileTypeAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(payload.StreamAnalysis.FileType);
        return 0;
    }

    private async Task<int> HashAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"MD5    {Convert.ToHexString(System.Security.Cryptography.MD5.HashData(payload.BufferedData.Span))}");
        Console.WriteLine($"SHA1   {Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(payload.BufferedData.Span))}");
        Console.WriteLine($"SHA256 {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload.BufferedData.Span))}");
        return 0;
    }

    private async Task<int> EntropyAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(TextAnalysisUtilities.ComputeEntropy(payload.BufferedData.Span).ToString("0.000"));
        return 0;
    }

    private async Task<int> ExtractAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        var artifacts = await CollectArtifactsAsync(payload, cancellationToken).ConfigureAwait(false);
        foreach (var artifact in artifacts)
        {
            Console.WriteLine($"{artifact.FileType} offset=0x{artifact.Offset:x} length={artifact.Length} sha256={artifact.Sha256}");
        }

        if (optionsMap.TryGetValue("out", out var outputDirectory) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            ExportArtifacts(artifacts, outputDirectory);
        }

        return 0;
    }

    private async Task<int> ExportArtifactsAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        if (!optionsMap.TryGetValue("out", out var outputDirectory) || string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException("export requires --out <directory>.");
        }

        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        var artifacts = await CollectArtifactsAsync(payload, cancellationToken).ConfigureAwait(false);
        ExportArtifacts(artifacts, outputDirectory);
        Console.WriteLine($"Exported {artifacts.Count} artifacts to {Path.GetFullPath(outputDirectory)}");
        return 0;
    }

    private int ModulesAsync()
    {
        foreach (var descriptor in moduleCatalog.GetDescriptors())
        {
            Console.WriteLine($"{descriptor.Name} [{descriptor.Category}] ({descriptor.Origin})");
        }

        return 0;
    }

    private static int ShowHelp()
    {
        Console.WriteLine("NeuroCTF commands: analyze, decode, exploit, scan, extract, bruteforce, recon, strings, xor, base64, hexdump, filetype, hash, entropy, export, modules, shell");
        Console.WriteLine("Common options: --file <path> --input <text> --module <name> --json --save <session.json> --replay <session.json>");
        return 0;
    }

    private async Task<InputPayload> ResolvePayloadAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        optionsMap.TryGetValue("input", out var input);
        optionsMap.TryGetValue("file", out var filePath);
        return await inputResolver.ResolveAsync(input, filePath, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AnalysisReport> AnalyzeFromOptionsAsync(Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        var payload = await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        return await analysisOrchestrator.AnalyzeAsync(payload, options, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string?> ParseOptions(string[] args)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            string? value = null;
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++index];
            }

            dictionary[key] = value;
        }

        return dictionary;
    }

    private static void WriteBytes(byte[] bytes, Dictionary<string, string?> optionsMap)
    {
        if (optionsMap.ContainsKey("hex"))
        {
            Console.WriteLine(Convert.ToHexString(bytes));
            return;
        }

        Console.WriteLine(Encoding.UTF8.GetString(bytes));
    }

    private static void WriteInsights(IEnumerable<Insight> insights)
    {
        foreach (var insight in insights)
        {
            Console.WriteLine($"{insight.Title}: {insight.Value}");
        }
    }

    private void WriteReport(AnalysisReport report, Dictionary<string, string?> optionsMap)
    {
        Console.WriteLine(optionsMap.ContainsKey("json") ? jsonReportWriter.Format(report) : reportWriter.Format(report));
    }

    private static void WriteVariants(IEnumerable<DataVariant> variants, Dictionary<string, string?> optionsMap)
    {
        foreach (var variant in variants)
        {
            Console.WriteLine($"[{variant.Description}]");
            WriteBytes(variant.Data.ToArray(), optionsMap);
        }
    }

    private async Task<ModuleExecutionResult> ExecuteNamedModuleAsync(
        string moduleName,
        Dictionary<string, string?> optionsMap,
        CancellationToken cancellationToken,
        InputPayload? payloadOverride = null)
    {
        var payload = payloadOverride ?? await ResolvePayloadAsync(optionsMap, cancellationToken).ConfigureAwait(false);
        return await moduleExecutionService.ExecuteByNameAsync(moduleName, payload.BufferedData, payload.Source, [], options, 0, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunModuleSetAsync(IReadOnlyList<string> modules, Dictionary<string, string?> optionsMap, CancellationToken cancellationToken)
    {
        foreach (var moduleName in modules)
        {
            var result = await ExecuteNamedModuleAsync(moduleName, optionsMap, cancellationToken).ConfigureAwait(false);
            if (result.Insights.Count == 0 && result.Variants.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"[{moduleName}]");
            WriteInsights(result.Insights);
            WriteVariants(result.Variants, optionsMap);
        }
    }

    private async Task<IReadOnlyList<ExtractedArtifact>> CollectArtifactsAsync(InputPayload payload, CancellationToken cancellationToken)
    {
        var carved = await moduleExecutionService.ExecuteByNameAsync("extract", payload.BufferedData, payload.Source, [], options, 0, cancellationToken).ConfigureAwait(false);
        var archives = await moduleExecutionService.ExecuteByNameAsync("archive-traversal", payload.BufferedData, payload.Source, [], options, 0, cancellationToken).ConfigureAwait(false);
        return carved.Artifacts.Concat(archives.Artifacts)
            .DistinctBy(item => $"{item.Name}:{item.Sha256}")
            .ToArray();
    }

    private static void ExportArtifacts(IReadOnlyList<ExtractedArtifact> artifacts, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var artifact in artifacts.Where(item => item.Payload is not null))
        {
            var safeName = artifact.SuggestedPath ?? artifact.Name;
            safeName = safeName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var path = Path.Combine(outputDirectory, safeName);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, artifact.Payload!);
        }
    }

    private async Task<int> LaunchShellAsync(CancellationToken cancellationToken)
    {
        var shell = new Shell.InteractiveShell(this);
        await shell.RunAsync(cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
