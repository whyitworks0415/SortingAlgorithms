using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class KdTreeOrderSortAlgorithm : ISpatialSortAlgorithm
{
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
        var all = Enumerable.Range(0, n).ToList();
        var order = new List<int>(n);

        IEnumerable<SortEvent> BuildOrder(List<int> subset, int depth, Region region)
        {
            if (subset.Count == 0)
            {
                yield break;
            }

            var axis = depth & 1;
            subset.Sort((a, b) =>
            {
                var pa = source[a];
                var pb = source[b];
                var axisCmp = axis == 0 ? pa.X.CompareTo(pb.X) : pa.Y.CompareTo(pb.Y);
                if (axisCmp != 0)
                {
                    return axisCmp;
                }

                var otherCmp = axis == 0 ? pa.Y.CompareTo(pb.Y) : pa.X.CompareTo(pb.X);
                if (otherCmp != 0)
                {
                    return otherCmp;
                }

                return pa.Id.CompareTo(pb.Id);
            });

            var mid = subset.Count / 2;
            var sourceIndex = subset[mid];
            var split = axis == 0 ? source[sourceIndex].X : source[sourceIndex].Y;

            var quantizedRegion = Quantize(region);
            yield return new SortEvent(
                SortEventType.RegionHighlight,
                I: quantizedRegion.X0,
                J: quantizedRegion.Y0,
                Value: quantizedRegion.X1,
                Aux: quantizedRegion.Y1,
                StepId: step++);
            yield return new SortEvent(SortEventType.PointKeyComputed, I: sourceIndex, Value: order.Count, StepId: step++);

            order.Add(sourceIndex);

            if (mid > 0)
            {
                var leftSubset = subset.GetRange(0, mid);
                var leftRegion = axis == 0
                    ? new Region(region.X0, region.Y0, Math.Clamp(split, region.X0, region.X1), region.Y1)
                    : new Region(region.X0, region.Y0, region.X1, Math.Clamp(split, region.Y0, region.Y1));
                foreach (var ev in BuildOrder(leftSubset, depth + 1, leftRegion))
                {
                    yield return ev;
                }
            }

            if (mid + 1 < subset.Count)
            {
                var rightSubset = subset.GetRange(mid + 1, subset.Count - mid - 1);
                var rightRegion = axis == 0
                    ? new Region(Math.Clamp(split, region.X0, region.X1), region.Y0, region.X1, region.Y1)
                    : new Region(region.X0, Math.Clamp(split, region.Y0, region.Y1), region.X1, region.Y1);
                foreach (var ev in BuildOrder(rightSubset, depth + 1, rightRegion))
                {
                    yield return ev;
                }
            }
        }

        foreach (var ev in BuildOrder(all, depth: 0, new Region(0f, 0f, 1f, 1f)))
        {
            yield return ev;
        }

        var keyBySourceIndex = new int[n];
        for (var i = 0; i < order.Count; i++)
        {
            keyBySourceIndex[order[i]] = i;
            yield return new SortEvent(SortEventType.PointKeyComputed, I: order[i], Value: i, StepId: step++);
        }

        var target = order.Select(index => source[index]).ToArray();
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

    private static (int X0, int Y0, int X1, int Y1) Quantize(Region region)
    {
        return (
            X0: (int)MathF.Round(Math.Clamp(region.X0, 0f, 1f) * 1000f),
            Y0: (int)MathF.Round(Math.Clamp(region.Y0, 0f, 1f) * 1000f),
            X1: (int)MathF.Round(Math.Clamp(region.X1, 0f, 1f) * 1000f),
            Y1: (int)MathF.Round(Math.Clamp(region.Y1, 0f, 1f) * 1000f));
    }
}
