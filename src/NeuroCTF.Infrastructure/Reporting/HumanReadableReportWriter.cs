using System.Text;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Reporting;

public sealed class HumanReadableReportWriter : IReportWriter
{
    public string Format(AnalysisReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Source: {report.Source}");
        builder.AppendLine($"Timestamp (UTC): {report.TimestampUtc:O}");
        builder.AppendLine($"File type: {report.StreamAnalysis.FileType}");
        builder.AppendLine($"Entropy: {report.InitialDetection.Entropy:0.000}");
        builder.AppendLine($"Printable ratio: {report.InitialDetection.PrintableRatio:0.000}");
        builder.AppendLine();
        builder.AppendLine("Top candidates:");
        foreach (var candidate in report.TopCandidates.Take(5))
        {
            builder.AppendLine($"  Score={candidate.Score.Value:0.000} Depth={candidate.Depth} Pipeline={candidate.Pipeline}");
            builder.AppendLine($"  Preview={candidate.Preview}");
        }

        if (report.Flags.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Flags:");
            foreach (var flag in report.Flags.Take(10))
            {
                builder.AppendLine($"  {flag.Value} (confidence {flag.Confidence:0.00})");
            }
        }

        if (report.Artifacts.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Artifacts:");
            foreach (var artifact in report.Artifacts.Take(10))
            {
                builder.AppendLine($"  {artifact.FileType} offset=0x{artifact.Offset:x} length={artifact.Length} sha256={artifact.Sha256}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Pipeline visualization:");
        foreach (var candidate in report.TopCandidates.Take(5))
        {
            builder.AppendLine($"  {candidate.Pipeline}");
        }

        return builder.ToString();
    }
}
