using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class QuadTreeOrderSortAlgorithm : ISpatialSortAlgorithm
{
    private const int MaxDepth = 16;
    private readonly record struct Region(float X0, float Y0, float X1, float Y1);

    public IEnumerable<SortEvent> Execute(SpatialPoint[] data, SpatialSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(SpatialPoint[] points)
    {
        long step = 0;
        var n = points.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var source = points.ToArray();
        var rootIndices = Enumerable.Range(0, n).ToList();
        var ordered = new List<int>(n);
        var keyBySourceIndex = new uint[n];

        IEnumerable<SortEvent> BuildOrder(List<int> indices, Region region, int depth, uint prefixKey)
        {
            if (indices.Count == 0)
            {
                yield break;
            }

            if (indices.Count == 1 || depth >= MaxDepth)
            {
                for (var i = 0; i < indices.Count; i++)
                {
                    var sourceIndex = indices[i];
                    var key = (prefixKey << 2) | (uint)Math.Clamp(i, 0, 3);
                    keyBySourceIndex[sourceIndex] = key;
                    ordered.Add(sourceIndex);
                    yield return new SortEvent(SortEventType.PointKeyComputed, I: sourceIndex, Value: unchecked((int)key), StepId: step++);
                }

                yield break;
            }

            var q = Quantize(region);
            yield return new SortEvent(SortEventType.RegionHighlight, I: q.X0, J: q.Y0, Value: q.X1, Aux: q.Y1, StepId: step++);

            var midX = (region.X0 + region.X1) * 0.5f;
            var midY = (region.Y0 + region.Y1) * 0.5f;

            var buckets = new List<int>[4]
            {
                new(),
                new(),
                new(),
                new()
            };

            for (var i = 0; i < indices.Count; i++)
            {
                var sourceIndex = indices[i];
                var point = source[sourceIndex];
                var quad = DetermineQuadrant(point, midX, midY);
                buckets[quad].Add(sourceIndex);
                yield return new SortEvent(SortEventType.MarkBucket, I: sourceIndex, Value: unchecked((int)prefixKey), Aux: quad, StepId: step++);
            }

            var regions = new Region[4]
            {
                new(region.X0, region.Y0, midX, midY),
                new(midX, region.Y0, region.X1, midY),
                new(region.X0, midY, midX, region.Y1),
                new(midX, midY, region.X1, region.Y1)
            };

            for (var quad = 0; quad < 4; quad++)
            {
                if (buckets[quad].Count == 0)
                {
                    continue;
                }

                var childKey = (prefixKey << 2) | (uint)quad;
                foreach (var ev in BuildOrder(buckets[quad], regions[quad], depth + 1, childKey))
                {
                    yield return ev;
                }
            }
        }

        foreach (var ev in BuildOrder(rootIndices, new Region(0f, 0f, 1f, 1f), depth: 0, prefixKey: 0))
        {
            yield return ev;
        }

        if (ordered.Count != n)
        {
            ordered = Enumerable.Range(0, n)
                .OrderBy(index => keyBySourceIndex[index])
                .ThenBy(index => source[index].Id)
                .ToList();
        }

        var target = ordered.Select(index => source[index]).ToArray();
        var positionById = new Dictionary<int, int>(n);
        for (var i = 0; i < n; i++)
        {
            positionById[points[i].Id] = i;
        }

        for (var i = 0; i < n; i++)
        {
            var targetId = target[i].Id;
            while (points[i].Id != targetId)
            {
                var j = positionById[targetId];
                (points[i], points[j]) = (points[j], points[i]);
                positionById[points[i].Id] = i;
                positionById[points[j].Id] = j;
                yield return new SortEvent(SortEventType.PointSwap, I: i, J: j, StepId: step++);
            }

            yield return new SortEvent(SortEventType.OrderUpdate, I: i, Value: i, StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int DetermineQuadrant(SpatialPoint point, float midX, float midY)
    {
        var east = point.X >= midX;
        var north = point.Y >= midY;
        return (north ? 2 : 0) | (east ? 1 : 0);
    }

    private static (int X0, int Y0, int X1, int Y1) Quantize(Region region)
    {
        return (
            X0: (int)MathF.Round(Math.Clamp(region.X0, 0f, 1f) * 1000f),
            Y0: (int)MathF.Round(Math.Clamp(region.Y0, 0f, 1f) * 1000f),
            X1: (int)MathF.Round(Math.Clamp(region.X1, 0f, 1f) * 1000f),
            Y1: (int)MathF.Round(Math.Clamp(region.Y1, 0f, 1f) * 1000f));
    }
}
