namespace SortingVisualizerApp.Core;

public static class DataGenerator
{
    public static int[] Generate(int size, DistributionPreset preset, int? seed = null)
    {
        if (size < 1)
        {
            return Array.Empty<int>();
        }

        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var data = new int[size];

        switch (preset)
        {
            case DistributionPreset.Random:
                FillRandom(data, random);
                break;
            case DistributionPreset.NearlySorted:
                FillSorted(data);
                Perturb(data, random, Math.Max(1, size / 20));
                break;
            case DistributionPreset.Reversed:
                FillReverse(data);
                break;
            case DistributionPreset.FewUnique:
                FillWithLimitedUniques(data, random, Math.Min(8, Math.Max(2, size / 128 + 2)));
                break;
            case DistributionPreset.ManyDuplicates:
                FillWithLimitedUniques(data, random, Math.Min(Math.Max(4, size / 16), Math.Max(4, size / 4)));
                break;
            case DistributionPreset.Gaussian:
                FillGaussian(data, random);
                break;
            case DistributionPreset.Steps:
                FillSteps(data);
                break;
            default:
                FillRandom(data, random);
                break;
        }

        return data;
    }

    public static void Shuffle(Span<int> data, Random random)
    {
        for (var i = data.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (data[i], data[j]) = (data[j], data[i]);
        }
    }

    private static void FillRandom(Span<int> data, Random random)
    {
        var max = Math.Max(2, data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = random.Next(max);
        }
    }

    private static void FillSorted(Span<int> data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }
    }

    private static void FillReverse(Span<int> data)
    {
        var last = data.Length - 1;
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = last - i;
        }
    }

    private static void Perturb(Span<int> data, Random random, int swaps)
    {
        for (var i = 0; i < swaps; i++)
        {
            var a = random.Next(data.Length);
            var b = random.Next(data.Length);
            (data[a], data[b]) = (data[b], data[a]);
        }
    }

    private static void FillWithLimitedUniques(Span<int> data, Random random, int uniqueCount)
    {
        uniqueCount = Math.Clamp(uniqueCount, 2, Math.Max(2, data.Length));
        var levels = new int[uniqueCount];
        for (var i = 0; i < uniqueCount; i++)
        {
            levels[i] = (int)Math.Round(i * (Math.Max(1, data.Length - 1) / (double)Math.Max(1, uniqueCount - 1)));
        }

        for (var i = 0; i < data.Length; i++)
        {
            data[i] = levels[random.Next(uniqueCount)];
        }
    }

    private static void FillGaussian(Span<int> data, Random random)
    {
        var max = Math.Max(1, data.Length - 1);
        var mean = max * 0.5;
        var sigma = Math.Max(1.0, max / 6.0);

        for (var i = 0; i < data.Length; i++)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            var sample = (int)Math.Round(mean + sigma * z0);
            data[i] = Math.Clamp(sample, 0, max);
        }
    }

    private static void FillSteps(Span<int> data)
    {
        var steps = Math.Clamp(data.Length / 32, 4, 32);
        var stepSize = Math.Max(1, data.Length / steps);

        for (var i = 0; i < data.Length; i++)
        {
            var step = i / stepSize;
            var normalized = (int)Math.Round(step * (Math.Max(1, data.Length - 1) / (double)Math.Max(1, steps - 1)));
            data[i] = Math.Clamp(normalized, 0, Math.Max(1, data.Length - 1));
        }
    }
}
