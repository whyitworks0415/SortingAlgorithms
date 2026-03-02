using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class FibonacciHeapSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var queue = new PriorityQueue<int, int>();
        var rootIds = new List<int>(Length);

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            queue.Enqueue(value, value);
            rootIds.Add(i);
            EmitEvent(SortEventType.MarkStructure, i, value: value, aux: 1);

            if ((i & 7) == 7)
            {
                ConsolidateRoots(rootIds);
            }
        }

        ConsolidateRoots(rootIds);

        for (var outIndex = 0; outIndex < Length; outIndex++)
        {
            var value = queue.Dequeue();
            Write(outIndex, value);
            if ((outIndex & 63) == 0)
            {
                EmitEvent(SortEventType.HeapBoundary, Length - outIndex - 1, value: queue.Count);
            }
        }
    }

    private void ConsolidateRoots(List<int> roots)
    {
        if (roots.Count <= 1)
        {
            return;
        }

        var degree = 0;
        while (roots.Count > 1)
        {
            var a = roots[^1];
            roots.RemoveAt(roots.Count - 1);
            var b = roots[^1];
            roots.RemoveAt(roots.Count - 1);

            EmitEvent(SortEventType.MergeTree, a, b, value: degree, aux: degree + 1);
            EmitEvent(SortEventType.LevelHighlight, degree + 1, value: roots.Count);
            roots.Add(Math.Min(a, b));
            degree++;
        }
    }
}
