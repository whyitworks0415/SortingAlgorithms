using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SpatialSortAlgorithm : ISpatialSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(SpatialPoint[] data, SpatialSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    public static uint SpatialLexicographicKey16(SpatialPoint point)
    {
        var x = (uint)Math.Clamp((int)MathF.Round(Math.Clamp(point.X, 0f, 1f) * 65535f), 0, 65535);
        var y = (uint)Math.Clamp((int)MathF.Round(Math.Clamp(point.Y, 0f, 1f) * 65535f), 0, 65535);
        return (x << 16) | y;
    }

    private static IEnumerable<SortEvent> ExecuteIterator(SpatialPoint[] points)
    {
        long step = 0;
        if (points.Length <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var keys = new uint[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var key = SpatialLexicographicKey16(points[i]);
            keys[i] = key;
            yield return new SortEvent(SortEventType.PointKeyComputed, I: i, Value: unchecked((int)key), StepId: step++);
        }

        var sorted = points
            .Select((point, index) => new RankedPoint(point, keys[index]))
            .OrderBy(static pair => pair.Key)
            .ThenBy(static pair => pair.Point.Id)
            .ToArray();

        var positionById = new Dictionary<int, int>(points.Length);
        for (var i = 0; i < points.Length; i++)
        {
            positionById[points[i].Id] = i;
        }

        for (var i = 0; i < points.Length; i++)
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
