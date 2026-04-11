using System.Text;
using System.Text.RegularExpressions;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Modules;

public sealed partial class ArchivePasswordHeuristicModule : IModule
{
    public string Name => "archive-passwords";

    public string Category => "inspect";

    public bool CanProcess(ReadOnlyMemory<byte> data, ModuleExecutionContext context) => data.Length > 0;

    public Task<ModuleExecutionResult> ProcessAsync(ModuleExecutionRequest request, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(request.Data.Span);
        var insights = PasswordRegex().Matches(text)
            .Select(match => new Insight("archive-password-candidate", match.Value))
            .DistinctBy(item => item.Value)
            .ToArray();
        return Task.FromResult(new ModuleExecutionResult(Name, Array.Empty<DataVariant>(), Array.Empty<FlagHit>(), Array.Empty<ExtractedArtifact>(), insights));
    }

    [GeneratedRegex(@"(?:(?:pass|password|pwd|zip)\s*[:=]\s*[A-Za-z0-9_!@#$%^&*.\-]{3,64})", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();
}
