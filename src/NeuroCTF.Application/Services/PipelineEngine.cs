using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class PipelineEngine(
    IModuleCatalog moduleCatalog,
    IDetectionService detectionService,
    IScoringService scoringService,
    IModuleExecutionService moduleExecutionService,
    ILogger<PipelineEngine> logger) : IPipelineEngine
{
    public async Task<AnalysisReport> ExecuteAsync(InputPayload payload, AnalysisOptions options, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting pipeline for {Source} with max depth {MaxDepth}", payload.Source, options.MaxDepth);
        var initialDetection = detectionService.Detect(payload.BufferedData);
        var initialFlags = CollectFlags(payload.BufferedData);
        var initialCandidate = CreateCandidate(payload.BufferedData, [], initialDetection, initialFlags, 0);
        var visited = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        visited.TryAdd(initialCandidate.Id, 0);

        var queue = new PriorityQueue<AnalysisCandidate, double>();
        queue.Enqueue(initialCandidate, -initialCandidate.Score.Value);

        var candidates = new List<AnalysisCandidate> { initialCandidate };
        var artifacts = new List<ExtractedArtifact>(payload.StreamAnalysis.Artifacts);
        var flags = new List<FlagHit>(payload.StreamAnalysis.Flags.Concat(initialFlags));
        var insights = new List<Insight>
        {
            new("Buffered bytes", payload.BufferedData.Length.ToString()),
            new("Input truncated", payload.IsTruncated.ToString())
        };

        while (queue.Count > 0 && candidates.Count < options.MaxCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = queue.Dequeue();
            if (candidate.Depth >= options.MaxDepth)
            {
                continue;
            }

            var context = new ModuleExecutionContext(candidate.Depth, payload.Source, candidate.Detection, options);
            var eligibleModules = moduleCatalog.GetAll().Where(module => module.CanProcess(candidate.Data, context)).ToArray();
            var results = new ConcurrentBag<ModuleExecutionResult>();
            await Parallel.ForEachAsync(
                eligibleModules,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Math.Max(1, options.MaxParallelModules)
                },
                async (module, token) =>
                {
                    var result = await moduleExecutionService.ExecuteAsync(
                        module,
                        candidate.Data,
                        payload.Source,
                        candidate.History,
                        options,
                        candidate.Depth,
                        token).ConfigureAwait(false);
                    results.Add(result);
                }).ConfigureAwait(false);

            foreach (var result in results)
            {
                flags.AddRange(result.Flags);
                artifacts.AddRange(result.Artifacts);
                insights.AddRange(result.Insights);

                foreach (var variant in result.Variants)
                {
                    var nextHistory = candidate.History.Concat(
                    [
                        new TransformationStep(result.ModuleName, variant.Description, variant.Metadata)
                    ]).ToArray();
                    var variantFlags = CollectFlags(variant.Data).Concat(result.Flags).DistinctBy(hit => $"{hit.Pattern}:{hit.Value}").ToArray();
                    var nextDetection = detectionService.Detect(variant.Data);
                    var nextCandidate = CreateCandidate(variant.Data, nextHistory, nextDetection, variantFlags, candidate.Depth + 1);

                    if (nextCandidate.Score.Value < options.MinimumCandidateScore)
                    {
                        continue;
                    }

                    if (!visited.TryAdd(nextCandidate.Id, 0))
                    {
                        continue;
                    }

                    candidates.Add(nextCandidate);
                    queue.Enqueue(nextCandidate, -nextCandidate.Score.Value);
                }
            }
        }

        var hashes = CreateHashes(payload.BufferedData);
        var orderedCandidates = candidates
            .DistinctBy(item => item.Id)
            .OrderByDescending(item => item.Score.Value)
            .ThenBy(item => item.Depth)
            .ToArray();

        return new AnalysisReport(
            payload.Source,
            DateTimeOffset.UtcNow,
            initialDetection,
            orderedCandidates,
            flags.DistinctBy(hit => $"{hit.Pattern}:{hit.Value}").OrderByDescending(hit => hit.Confidence).ToArray(),
            artifacts.DistinctBy(artifact => $"{artifact.Offset}:{artifact.Sha256}").Take(options.MaxArtifacts).ToArray(),
            hashes,
            insights.DistinctBy(insight => $"{insight.Title}:{insight.Value}").ToArray(),
            payload.StreamAnalysis);
    }

    private AnalysisCandidate CreateCandidate(
        ReadOnlyMemory<byte> data,
        IReadOnlyList<TransformationStep> history,
        DetectionProfile detection,
        IReadOnlyList<FlagHit> flags,
        int depth)
    {
        var id = Convert.ToHexString(SHA256.HashData(data.Span));
        var score = scoringService.Score(data, history, flags);
        var storedData = data.ToArray();
        return new AnalysisCandidate(
            id,
            storedData,
            history,
            score,
            depth,
            TextAnalysisUtilities.GetAsciiPreview(storedData),
            detection,
            flags);
    }

    private static IReadOnlyList<HashEntry> CreateHashes(ReadOnlyMemory<byte> data)
    {
        return
        [
            new HashEntry("MD5", Convert.ToHexString(MD5.HashData(data.Span))),
            new HashEntry("SHA1", Convert.ToHexString(SHA1.HashData(data.Span))),
            new HashEntry("SHA256", Convert.ToHexString(SHA256.HashData(data.Span)))
        ];
    }

    private static IReadOnlyList<FlagHit> CollectFlags(ReadOnlyMemory<byte> data)
    {
        var utf8 = System.Text.Encoding.UTF8.GetString(data.Span);
        var normalized = TextAnalysisUtilities.NormalizeObfuscation(utf8);
        return TextAnalysisUtilities.FindFlagPatterns(utf8)
            .Concat(TextAnalysisUtilities.FindFlagPatterns(normalized))
            .Select(match => new FlagHit(match.Value, "regex", match.Partial, match.Partial ? 0.55d : 0.95d))
            .DistinctBy(hit => hit.Value)
            .ToArray();
    }
}
