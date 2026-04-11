using Microsoft.Extensions.Logging;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class ModuleExecutionService(
    IModuleCatalog moduleCatalog,
    IDetectionService detectionService,
    ILogger<ModuleExecutionService> logger) : IModuleExecutionService
{
    public async Task<ModuleExecutionResult> ExecuteAsync(
        IModule module,
        ReadOnlyMemory<byte> data,
        string source,
        IReadOnlyList<TransformationStep> history,
        AnalysisOptions options,
        int depth,
        CancellationToken cancellationToken)
    {
        var detection = detectionService.Detect(data);
        var context = new ModuleExecutionContext(depth, source, detection, options);
        if (!module.CanProcess(data, context))
        {
            return ModuleExecutionResult.Empty(module.Name);
        }

        try
        {
            return await module.ProcessAsync(new ModuleExecutionRequest(data, context, history), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Module {ModuleName} failed while processing source {Source}", module.Name, source);
            return ModuleExecutionResult.Empty(module.Name);
        }
    }

    public Task<ModuleExecutionResult> ExecuteByNameAsync(
        string moduleName,
        ReadOnlyMemory<byte> data,
        string source,
        IReadOnlyList<TransformationStep> history,
        AnalysisOptions options,
        int depth,
        CancellationToken cancellationToken)
    {
        var module = moduleCatalog.GetByName(moduleName)
            ?? throw new InvalidOperationException($"Module '{moduleName}' is unavailable.");
        return ExecuteAsync(module, data, source, history, options, depth, cancellationToken);
    }
}
