using System.Text;

namespace NeuroCTF.Application.Utilities;

public static class ByteFormatting
{
    public static string ToHexDump(ReadOnlySpan<byte> data, int width = 16)
    {
        var builder = new StringBuilder();

        for (var offset = 0; offset < data.Length; offset += width)
        {
            var slice = data[offset..Math.Min(data.Length, offset + width)];
            builder.Append($"{offset:x8}  ");
            for (var index = 0; index < width; index++)
            {
                if (index < slice.Length)
                {
                    builder.Append($"{slice[index]:x2} ");
                }
                else
                {
                    builder.Append("   ");
                }
            }

            builder.Append(" |");
            foreach (var value in slice)
            {
                builder.Append(value is >= 32 and <= 126 ? (char)value : '.');
            }

            builder.AppendLine("|");
        }

        return builder.ToString();
    }
}
