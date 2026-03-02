namespace SortingVisualizerApp.Core;

public static class StringDataGenerator
{
    public static StringItem[] Generate(
        int count,
        int length,
        StringAlphabetSet alphabet,
        StringDistributionPreset distribution,
        int? seed = null)
    {
        count = Math.Clamp(count, 1, 5000);
        length = Math.Clamp(length, 2, 64);

        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var symbols = ResolveAlphabet(alphabet);
        var items = new StringItem[count];

        switch (distribution)
        {
            case StringDistributionPreset.CommonPrefix:
                FillCommonPrefix(items, random, symbols, length);
                break;
            case StringDistributionPreset.ManyDuplicates:
                FillManyDuplicates(items, random, symbols, length);
                break;
            default:
                FillRandom(items, random, symbols, length);
                break;
        }

        return items;
    }

    private static void FillRandom(StringItem[] items, Random random, string alphabet, int length)
    {
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new StringItem(i, RandomString(random, alphabet, length), i);
        }
    }

    private static void FillCommonPrefix(StringItem[] items, Random random, string alphabet, int length)
    {
        var prefixLength = Math.Clamp(length / 2, 1, Math.Max(1, length - 1));
        var prefix = RandomString(random, alphabet, prefixLength);

        for (var i = 0; i < items.Length; i++)
        {
            var suffixLength = Math.Max(1, length - prefixLength);
            var suffix = RandomString(random, alphabet, suffixLength);
            items[i] = new StringItem(i, prefix + suffix, i);
        }
    }

    private static void FillManyDuplicates(StringItem[] items, Random random, string alphabet, int length)
    {
        var unique = Math.Clamp(items.Length / 12, 2, 64);
        var pool = new string[unique];
        for (var i = 0; i < unique; i++)
        {
            pool[i] = RandomString(random, alphabet, length);
        }

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new StringItem(i, pool[random.Next(pool.Length)], i);
        }
    }

    private static string ResolveAlphabet(StringAlphabetSet alphabet)
    {
        return alphabet switch
        {
            StringAlphabetSet.Digits => "0123456789",
            StringAlphabetSet.Lowercase => "abcdefghijklmnopqrstuvwxyz",
            StringAlphabetSet.Uppercase => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            _ => "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        };
    }

    private static string RandomString(Random random, string alphabet, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(chars);
    }
}
