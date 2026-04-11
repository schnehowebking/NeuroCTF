using Microsoft.Extensions.Logging;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class AnalysisOrchestrator(
    IPipelineEngine pipelineEngine,
    ILogger<AnalysisOrchestrator> logger) : IAnalysisOrchestrator
{
    public async Task<AnalysisReport> AnalyzeAsync(InputPayload payload, AnalysisOptions options, CancellationToken cancellationToken)
    {
        logger.LogInformation("Analyzing {Source} with {BufferedBytes} buffered bytes", payload.Source, payload.BufferedData.Length);
        return await pipelineEngine.ExecuteAsync(payload, options, cancellationToken).ConfigureAwait(false);
    }
}
