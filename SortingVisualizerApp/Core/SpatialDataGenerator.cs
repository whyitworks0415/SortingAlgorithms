namespace SortingVisualizerApp.Core;

public static class SpatialDataGenerator
{
    public static SpatialPoint[] Generate(int count, SpatialDistributionPreset preset, int? seed = null)
    {
        count = Math.Clamp(count, 1, 200000);
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var points = new SpatialPoint[count];

        switch (preset)
        {
            case SpatialDistributionPreset.Gaussian:
                FillGaussian(points, random);
                break;
            case SpatialDistributionPreset.Clusters:
                FillClusters(points, random);
                break;
            default:
                FillUniform(points, random);
                break;
        }

        return points;
    }

    private static void FillUniform(SpatialPoint[] points, Random random)
    {
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new SpatialPoint(i, (float)random.NextDouble(), (float)random.NextDouble());
        }
    }

    private static void FillGaussian(SpatialPoint[] points, Random random)
    {
        for (var i = 0; i < points.Length; i++)
        {
            var x = Clamp01(0.5f + 0.18f * NextGaussian(random));
            var y = Clamp01(0.5f + 0.18f * NextGaussian(random));
            points[i] = new SpatialPoint(i, x, y);
        }
    }

    private static void FillClusters(SpatialPoint[] points, Random random)
    {
        var clusters = Math.Clamp(points.Length / 4000, 3, 12);
        var centers = new (float X, float Y)[clusters];
        for (var i = 0; i < clusters; i++)
        {
            centers[i] = ((float)random.NextDouble(), (float)random.NextDouble());
        }

        for (var i = 0; i < points.Length; i++)
        {
            var c = centers[random.Next(clusters)];
            var x = Clamp01(c.X + 0.06f * NextGaussian(random));
            var y = Clamp01(c.Y + 0.06f * NextGaussian(random));
            points[i] = new SpatialPoint(i, x, y);
        }
    }

    private static float NextGaussian(Random random)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0.0f, 1.0f);
    }
}
