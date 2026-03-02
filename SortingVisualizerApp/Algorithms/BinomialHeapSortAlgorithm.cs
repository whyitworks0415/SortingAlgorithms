using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BinomialHeapSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var queue = new PriorityQueue<int, int>();
        var rootsByDegree = new Dictionary<int, int>();

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            queue.Enqueue(value, value);
            EmitEvent(SortEventType.MarkStructure, i, value: value, aux: 0);

            var degree = 0;
            while (true)
            {
                if (!rootsByDegree.TryGetValue(degree, out var existingRootId))
                {
                    rootsByDegree[degree] = i;
                    break;
                }

                EmitEvent(SortEventType.MergeTree, existingRootId, i, value: degree, aux: degree + 1);
                EmitEvent(SortEventType.LevelHighlight, degree + 1, value: rootsByDegree.Count);
                rootsByDegree.Remove(degree);
                degree++;
            }
        }

        for (var outIndex = 0; outIndex < Length; outIndex++)
        {
            var value = queue.Dequeue();
            Write(outIndex, value);

            if ((outIndex & 31) == 0)
            {
                EmitEvent(SortEventType.HeapBoundary, Length - outIndex - 1, value: queue.Count);
            }
        }
    }
}
