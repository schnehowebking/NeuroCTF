using NeuroCTF.Core.Models;

namespace NeuroCTF.Core.Abstractions;

public interface IModule
{
    string Name { get; }

    string Category { get; }

    bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context);

    Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken);
}

public interface IModuleCatalog
{
    IReadOnlyList<IModule> GetAll();

    IModule? GetByName(string name);

    IReadOnlyList<ModuleDescriptor> GetDescriptors();
}

public interface IScoringService
{
    CandidateScore Score(ReadOnlyMemory<byte> data, IReadOnlyList<TransformationStep> steps, IReadOnlyList<FlagHit> flags);
}

public interface IDetectionService
{
    DetectionProfile Detect(ReadOnlyMemory<byte> data);
}

public interface IPipelineEngine
{
    Task<AnalysisReport> ExecuteAsync(InputPayload payload, AnalysisOptions options, CancellationToken cancellationToken);
}

public interface IAnalysisOrchestrator
{
    Task<AnalysisReport> AnalyzeAsync(InputPayload payload, AnalysisOptions options, CancellationToken cancellationToken);
}

public interface IAnalysisSessionStore
{
    Task<string> SaveAsync(AnalysisReport report, string? requestedPath, CancellationToken cancellationToken);

    Task<AnalysisReport> LoadAsync(string path, CancellationToken cancellationToken);
}

public interface IReportWriter
{
    string Format(AnalysisReport report);
}

public interface IPluginLoader
{
    Task<PluginLoadResult> LoadModulesAsync(string pluginDirectory, CancellationToken cancellationToken);
}

public interface IInputResolver
{
    Task<InputPayload> ResolveAsync(string? input, string? filePath, CancellationToken cancellationToken);
}

public interface IModuleExecutionService
{
    Task<ModuleExecutionResult> ExecuteAsync(
        IModule module,
        ReadOnlyMemory<byte> data,
        string source,
        IReadOnlyList<TransformationStep> history,
        AnalysisOptions options,
        int depth,
        CancellationToken cancellationToken);

    Task<ModuleExecutionResult> ExecuteByNameAsync(
        string moduleName,
        ReadOnlyMemory<byte> data,
        string source,
        IReadOnlyList<TransformationStep> history,
        AnalysisOptions options,
        int depth,
        CancellationToken cancellationToken);
}
