using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class DetectionService : IDetectionService
{
    public DetectionProfile Detect(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        var printable = TextAnalysisUtilities.ComputePrintableRatio(span);
        var entropy = TextAnalysisUtilities.ComputeEntropy(span);
        var preview = Encoding.ASCII.GetString(span[..Math.Min(span.Length, 1024)]);
        var normalized = preview.Trim();
        var hexChars = normalized.Count(Uri.IsHexDigit);
        var looksHex = normalized.Length > 6 && hexChars >= normalized.Length * 0.85d;
        var b64Chars = normalized.Count(ch => char.IsLetterOrDigit(ch) || ch is '+' or '/' or '=' or '-' or '_');
        var looksBase64 = normalized.Length > 8 && b64Chars >= normalized.Length * 0.9d && normalized.Length % 4 is 0 or 2 or 3;
        var looksUrlEncoded = normalized.Contains('%') || normalized.Contains('+');
        var looksCompressed = span.Length > 2 && ((span[0] == 0x1F && span[1] == 0x8B) || (span[0] == 0x78 && span[1] is 0x01 or 0x9C or 0xDA));
        var looksBinary = printable < 0.60d || span.Contains((byte)0);

        return new DetectionProfile(
            LooksLikeBase64: looksBase64,
            LooksLikeHex: looksHex,
            LooksLikeUrlEncoded: looksUrlEncoded,
            LooksCompressed: looksCompressed,
            LooksBinary: looksBinary,
            Entropy: entropy,
            PrintableRatio: printable,
            SuggestedTextEncoding: span.Contains((byte)0) ? Encoding.Unicode.WebName : Encoding.UTF8.WebName,
            Hints: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["preview"] = TextAnalysisUtilities.GetAsciiPreview(span),
                ["size"] = span.Length.ToString(),
                ["hex-ratio"] = normalized.Length == 0 ? "0" : (hexChars / (double)normalized.Length).ToString("0.00"),
                ["base64-ratio"] = normalized.Length == 0 ? "0" : (b64Chars / (double)normalized.Length).ToString("0.00")
            });
    }
}
