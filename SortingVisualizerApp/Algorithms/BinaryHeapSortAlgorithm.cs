using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BinaryHeapSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        for (var i = Length / 2 - 1; i >= 0; i--)
        {
            EmitEvent(SortEventType.MarkStructure, i, value: 1);
            SiftDown(i, Length, buildPhase: true);
        }

        for (var end = Length - 1; end > 0; end--)
        {
            EmitEvent(SortEventType.HeapBoundary, end, value: end);
            Swap(0, end);
            SiftDown(0, end, buildPhase: false);
        }
    }

    private void SiftDown(int root, int heapSize, bool buildPhase)
    {
        while (true)
        {
            var left = root * 2 + 1;
            if (left >= heapSize)
            {
                return;
            }

            var right = left + 1;
            var largest = root;

            if (Compare(left, largest) > 0)
            {
                largest = left;
            }

            if (right < heapSize && Compare(right, largest) > 0)
            {
                largest = right;
            }

            if (largest == root)
            {
                return;
            }

            EmitEvent(SortEventType.MarkStructure, largest, value: buildPhase ? 0 : 1);
            Swap(root, largest);
            root = largest;
        }
    }
}
