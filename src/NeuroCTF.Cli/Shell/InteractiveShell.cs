using NeuroCTF.Cli.Commands;

namespace NeuroCTF.Cli.Shell;

public sealed class InteractiveShell(CliRouter router)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        string? loadedFile = null;
        string? inlineInput = null;
        NeuroCTF.Core.Models.AnalysisReport? lastReport = null;
        var history = new List<string>();
        Console.WriteLine("NeuroCTF interactive shell. Commands: load <path>, set-input <text>, analyze, decode <module>, pipeline, graph, state, history, run <command>, exit");

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("neuroctf> ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            history.Add(line);
            var parts = Split(line);
            var verb = parts[0].ToLowerInvariant();
            if (verb is "exit" or "quit")
            {
                break;
            }

            if (verb == "load" && parts.Length > 1)
            {
                loadedFile = parts[1];
                inlineInput = null;
                Console.WriteLine($"Loaded {loadedFile}");
                continue;
            }

            if (verb == "set-input" && parts.Length > 1)
            {
                inlineInput = string.Join(' ', parts[1..]);
                loadedFile = null;
                Console.WriteLine("Inline input updated");
                continue;
            }

            if (verb == "analyze")
            {
                lastReport = await router.AnalyzeReportAsync(inlineInput, loadedFile, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(router.RenderPipelineGraph(lastReport));
                continue;
            }

            if (verb == "pipeline")
            {
                lastReport ??= await router.AnalyzeReportAsync(inlineInput, loadedFile, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(router.RenderPipelineGraph(lastReport));
                continue;
            }

            if (verb == "graph")
            {
                if (lastReport is null)
                {
                    lastReport = await router.AnalyzeReportAsync(inlineInput, loadedFile, cancellationToken).ConfigureAwait(false);
                }

                Console.WriteLine(router.RenderPipelineGraph(lastReport));
                continue;
            }

            if (verb == "state")
            {
                Console.WriteLine($"loaded-file: {loadedFile ?? "<none>"}");
                Console.WriteLine($"inline-input: {(inlineInput is null ? "<none>" : "<set>")}");
                Console.WriteLine($"last-report: {(lastReport is null ? "<none>" : lastReport.Source)}");
                continue;
            }

            if (verb == "history")
            {
                for (var index = 0; index < history.Count; index++)
                {
                    Console.WriteLine($"{index + 1}: {history[index]}");
                }
                continue;
            }

            if (verb == "decode" && parts.Length > 1)
            {
                await router.RouteAsync(BuildCommand(parts[1], loadedFile, inlineInput), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (verb == "run" && parts.Length > 1)
            {
                await router.RouteAsync(parts[1..], cancellationToken).ConfigureAwait(false);
                continue;
            }

            await router.RouteAsync(parts, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string[] BuildCommand(string command, string? file, string? input)
    {
        if (!string.IsNullOrWhiteSpace(file))
        {
            return [command, "--file", file];
        }

        if (!string.IsNullOrWhiteSpace(input))
        {
            return [command, "--input", input];
        }

        return [command];
    }

    private static string[] Split(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
