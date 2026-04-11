using System.Text.Json;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Reporting;

public sealed class JsonReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Format(AnalysisReport report) => JsonSerializer.Serialize(report, SerializerOptions);
}
