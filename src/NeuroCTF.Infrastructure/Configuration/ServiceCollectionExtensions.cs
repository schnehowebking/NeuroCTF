using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NeuroCTF.Application.Services;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;
using NeuroCTF.Core.Options;
using NeuroCTF.Infrastructure.Input;
using NeuroCTF.Infrastructure.Plugins;
using NeuroCTF.Infrastructure.Reporting;
using NeuroCTF.Infrastructure.Storage;

namespace NeuroCTF.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNeuroCtfInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NeuroCtfOptions>(configuration.GetSection("NeuroCTF"));
        services.AddSingleton(sp => ToAnalysisOptions(sp.GetRequiredService<IOptions<NeuroCtfOptions>>().Value));

        services.AddSingleton<IDetectionService, DetectionService>();
        services.AddSingleton<IScoringService, ScoringService>();
        services.AddSingleton<IModuleExecutionService, ModuleExecutionService>();
        services.AddSingleton<IPipelineEngine, PipelineEngine>();
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        services.AddSingleton<IPluginLoader, ReflectionPluginLoader>();
        services.AddSingleton<IAnalysisSessionStore, FileAnalysisSessionStore>();
        services.AddSingleton<IInputResolver, InputResolver>();
        services.AddSingleton<IReportWriter, HumanReadableReportWriter>();
        services.AddSingleton<JsonReportWriter>();
        services.AddSingleton<StreamingInspector>();
        return services;
    }

    private static AnalysisOptions ToAnalysisOptions(NeuroCtfOptions options) => new()
    {
        MaxDepth = options.MaxDepth,
        MaxCandidates = options.MaxCandidates,
        MaxBufferedBytes = options.MaxBufferedBytes,
        MaxArtifacts = options.MaxArtifacts,
        MaxParallelModules = options.MaxParallelModules,
        MaxBruteforceResults = options.MaxBruteforceResults,
        ArchiveRecursionDepth = options.ArchiveRecursionDepth,
        MinimumCandidateScore = options.MinimumCandidateScore,
        PluginDirectory = options.PluginDirectory,
        SessionsDirectory = options.SessionsDirectory,
        CommonMultiByteKeys = options.CommonMultiByteKeys
    };
}
