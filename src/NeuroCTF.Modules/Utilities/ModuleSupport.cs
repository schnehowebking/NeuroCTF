using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NeuroCTF.Application.Utilities;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Modules.Utilities;

public static class ModuleSupport
{
    public static IReadOnlyList<FlagHit> FindFlags(ReadOnlySpan<byte> data)
    {
        var utf8 = Encoding.UTF8.GetString(data);
        return TextAnalysisUtilities.FindFlagPatterns(utf8)
            .Concat(TextAnalysisUtilities.FindFlagPatterns(TextAnalysisUtilities.NormalizeObfuscation(utf8)))
            .Select(match => new FlagHit(match.Value, "module", match.Partial, match.Partial ? 0.55d : 0.92d))
            .DistinctBy(hit => hit.Value)
            .ToArray();
    }

    public static IReadOnlyList<Insight> BuildCommonInsights(string title, ReadOnlySpan<byte> data)
    {
        return
        [
            new Insight($"{title} entropy", TextAnalysisUtilities.ComputeEntropy(data).ToString("0.000")),
            new Insight($"{title} printable-ratio", TextAnalysisUtilities.ComputePrintableRatio(data).ToString("0.000")),
            new Insight($"{title} sha256", Convert.ToHexString(SHA256.HashData(data)))
        ];
    }

    public static byte[] Xor(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key)
    {
        var output = new byte[input.Length];
        for (var index = 0; index < input.Length; index++)
        {
            output[index] = (byte)(input[index] ^ key[index % key.Length]);
        }

        return output;
    }

    public static bool LooksGzip(ReadOnlySpan<byte> data) =>
        data.Length > 2 && data[0] == 0x1F && data[1] == 0x8B;

    public static byte[] Gunzip(ReadOnlySpan<byte> data)
    {
        using var input = new MemoryStream(data.ToArray());
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
