using System.Text.Json;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Storage;

public sealed class FileAnalysisSessionStore(AnalysisOptions options) : IAnalysisSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task<string> SaveAsync(AnalysisReport report, string? requestedPath, CancellationToken cancellationToken)
    {
        var path = requestedPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            Directory.CreateDirectory(options.SessionsDirectory);
            path = Path.Combine(options.SessionsDirectory, $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Path.GetFileName(report.Source)}.session.json");
        }
        else
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        var session = new AnalysisSession
        {
            FileVersion = "1",
            Report = report
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(session, SerializerOptions), cancellationToken).ConfigureAwait(false);
        return Path.GetFullPath(path);
    }

    public async Task<AnalysisReport> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var session = JsonSerializer.Deserialize<AnalysisSession>(content, SerializerOptions)
            ?? throw new InvalidOperationException("Session file is invalid.");
        return session.Report;
    }
}
