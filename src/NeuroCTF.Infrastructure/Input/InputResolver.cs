using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Input;

public sealed class InputResolver(StreamingInspector inspector, AnalysisOptions options) : IInputResolver
{
    public async Task<InputPayload> ResolveAsync(string? input, string? filePath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fullPath = Path.GetFullPath(filePath);
            var fileInspection = await inspector.ReadAsync(fullPath, options.MaxBufferedBytes, cancellationToken).ConfigureAwait(false);
            return new InputPayload(fileInspection.Buffer, fullPath, fileInspection.Truncated, fileInspection.StreamAnalysis);
        }

        if (!string.IsNullOrEmpty(input))
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return new InputPayload(bytes, "inline", false, inspector.InspectBuffer(bytes));
        }

        using var stdin = Console.OpenStandardInput();
        var streamInspection = await inspector.ReadAsync(stdin, options.MaxBufferedBytes, cancellationToken).ConfigureAwait(false);
        return new InputPayload(streamInspection.Buffer, "stdin", streamInspection.Truncated, streamInspection.StreamAnalysis);
    }
}
