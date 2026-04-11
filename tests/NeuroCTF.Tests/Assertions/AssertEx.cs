namespace NeuroCTF.Tests.Assertions;

public static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}. Expected={expected} Actual={actual}");
        }
    }

    public static void Contains(string expectedSubstring, string actual, string message)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{message}. Missing substring '{expectedSubstring}'.");
        }
    }

    public static void LessThanOrEqual(int actual, int maximum, string message)
    {
        if (actual > maximum)
        {
            throw new InvalidOperationException($"{message}. Actual={actual} Maximum={maximum}");
        }
    }
}
