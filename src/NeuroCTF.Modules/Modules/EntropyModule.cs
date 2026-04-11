using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed class EntropyModule : IModule
{
    public string Name => "entropy";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Insight> insights =
        [
            new("entropy", TextAnalysisUtilities.ComputeEntropy(request.Data.Span).ToString("0.000")),
            new("printable-ratio", TextAnalysisUtilities.ComputePrintableRatio(request.Data.Span).ToString("0.000"))
        ];

        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }
}
