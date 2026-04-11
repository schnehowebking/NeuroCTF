using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NeuroCTF.Application.Utilities;

public static partial class TextAnalysisUtilities
{
    private static readonly string[] CommonWords =
    {
        "the", "and", "you", "that", "have", "flag", "ctf", "secret", "this", "with", "for"
    };

    private static readonly IReadOnlyDictionary<string, double> CommonTrigrams = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["the"] = 1.0, ["and"] = 0.9, ["ing"] = 0.85, ["ion"] = 0.8, ["ent"] = 0.78,
        ["her"] = 0.76, ["for"] = 0.74, ["tha"] = 0.72, ["nth"] = 0.70, ["int"] = 0.69,
        ["ere"] = 0.68, ["tio"] = 0.66, ["ter"] = 0.64, ["est"] = 0.63, ["ers"] = 0.61,
        ["ati"] = 0.60, ["hat"] = 0.58, ["ate"] = 0.56, ["all"] = 0.54, ["eth"] = 0.52
    };

    [GeneratedRegex(@"flag\{[^}\r\n]{1,256}\}|ctf\{[^}\r\n]{1,256}\}", RegexOptions.IgnoreCase)]
    private static partial Regex FullFlagRegex();

    [GeneratedRegex(@"(?:flag|ctf)\{[^}\r\n]{0,256}$", RegexOptions.IgnoreCase)]
    private static partial Regex PartialFlagRegex();

    public static string GetAsciiPreview(ReadOnlySpan<byte> data, int maxLength = 120)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var value in data[..Math.Min(maxLength, data.Length)])
        {
            builder.Append(value is >= 32 and <= 126 ? (char)value : '.');
        }

        return builder.ToString();
    }

    public static double ComputeEntropy(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0d;
        }

        Span<int> frequencies = stackalloc int[256];
        foreach (var value in data)
        {
            frequencies[value]++;
        }

        var entropy = 0d;
        var length = (double)data.Length;
        foreach (var count in frequencies)
        {
            if (count == 0)
            {
                continue;
            }

            var probability = count / length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }

    public static double ComputePrintableRatio(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0d;
        }

        var printable = 0;
        foreach (var value in data)
        {
            if (value is 9 or 10 or 13 or >= 32 and <= 126)
            {
                printable++;
            }
        }

        return printable / (double)data.Length;
    }

    public static double ComputeEnglishLikelihood(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0d;
        }

        var normalized = value.ToLowerInvariant();
        var hits = CommonWords.Count(normalized.Contains);
        var alpha = normalized.Count(char.IsLetter);
        var spaces = normalized.Count(char.IsWhiteSpace);
        var punctuation = normalized.Count(ch => ",.;:_-!?".Contains(ch));
        var trigram = ComputeTrigramScore(normalized);
        return Math.Min(1d, (hits * 0.10d) + (alpha / (double)Math.Max(1, normalized.Length)) * 0.45d + (spaces / (double)Math.Max(1, normalized.Length)) * 0.10d + (punctuation / (double)Math.Max(1, normalized.Length)) * 0.05d + (trigram * 0.30d));
    }

    public static double ComputeTrigramScore(string text)
    {
        if (text.Length < 3)
        {
            return 0d;
        }

        double score = 0;
        var windows = 0;
        for (var index = 0; index <= text.Length - 3; index++)
        {
            var trigram = text.Substring(index, 3);
            if (!trigram.All(char.IsLetter))
            {
                continue;
            }

            windows++;
            if (CommonTrigrams.TryGetValue(trigram, out var weight))
            {
                score += weight;
            }
        }

        return windows == 0 ? 0d : Math.Min(1d, score / windows);
    }

    public static IEnumerable<(string Value, bool Partial)> FindFlagPatterns(string text)
    {
        foreach (Match match in FullFlagRegex().Matches(text))
        {
            yield return (match.Value, false);
        }

        foreach (Match match in PartialFlagRegex().Matches(text))
        {
            yield return (match.Value, true);
        }
    }

    public static string NormalizeObfuscation(string text)
    {
        return text
            .Replace('0', 'o')
            .Replace('1', 'l')
            .Replace('3', 'e')
            .Replace('4', 'a')
            .Replace('5', 's')
            .Replace('@', 'a')
            .Replace('$', 's');
    }

    public static string ToHex(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(data);

    public static string ComputeSha256(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data));

    public static byte[] TryGunzip(ReadOnlySpan<byte> data)
    {
        using var input = new MemoryStream(data.ToArray());
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    public static IReadOnlyList<string> ExtractStrings(ReadOnlySpan<byte> data, int minLength = 4)
    {
        var buffer = data.ToArray();
        return ExtractAsciiStrings(buffer, minLength)
            .Concat(ExtractUtf16Strings(buffer, minLength))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> ExtractAsciiStrings(byte[] data, int minLength)
    {
        var builder = new StringBuilder();
        foreach (var value in data)
        {
            if (value is >= 32 and <= 126)
            {
                builder.Append((char)value);
                continue;
            }

            if (builder.Length >= minLength)
            {
                yield return builder.ToString();
            }

            builder.Clear();
        }

        if (builder.Length >= minLength)
        {
            yield return builder.ToString();
        }
    }

    private static IEnumerable<string> ExtractUtf16Strings(byte[] data, int minLength)
    {
        if (data.Length < minLength * 2)
        {
            yield break;
        }

        var builder = new StringBuilder();
        for (var index = 0; index + 1 < data.Length; index += 2)
        {
            var value = BitConverter.ToUInt16(data, index);
            if (value is >= 32 and <= 126)
            {
                builder.Append((char)value);
                continue;
            }

            if (builder.Length >= minLength)
            {
                yield return builder.ToString();
            }

            builder.Clear();
        }

        if (builder.Length >= minLength)
        {
            yield return builder.ToString();
        }
    }
}
