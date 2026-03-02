using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class MortonOrderSortAlgorithm : ISpatialSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(SpatialPoint[] data, SpatialSortOptions options)
    {
        return ExecuteIterator(data.ToArray(), useZOrderAlias: false);
    }

    internal static IEnumerable<SortEvent> ExecuteIterator(SpatialPoint[] points, bool useZOrderAlias)
    {
        long step = 0;
        var n = points.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var keys = new uint[n];
        for (var i = 0; i < n; i++)
        {
            var key = useZOrderAlias
                ? SpatialKeyUtils.ZOrderKey16(points[i].X, points[i].Y)
                : SpatialKeyUtils.MortonKey16(points[i].X, points[i].Y);
            keys[i] = key;
            yield return new SortEvent(SortEventType.PointKeyComputed, I: i, Value: unchecked((int)key), StepId: step++);
        }

        var sorted = points
            .Select((point, index) => new RankedPoint(point, keys[index]))
            .OrderBy(static pair => pair.Key)
            .ThenBy(static pair => pair.Point.Id)
            .ToArray();

        var positionById = new Dictionary<int, int>(n);
        for (var i = 0; i < n; i++)
        {
            positionById[points[i].Id] = i;
        }

        for (var i = 0; i < n; i++)
        {
            var targetId = sorted[i].Point.Id;
            while (points[i].Id != targetId)
            {
                var j = positionById[targetId];
                (points[i], points[j]) = (points[j], points[i]);

                positionById[points[i].Id] = i;
                positionById[points[j].Id] = j;

                yield return new SortEvent(SortEventType.PointSwap, I: i, J: j, StepId: step++);
            }

            yield return new SortEvent(SortEventType.OrderUpdate, I: i, Value: unchecked((int)sorted[i].Key), StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private readonly record struct RankedPoint(SpatialPoint Point, uint Key);
}
