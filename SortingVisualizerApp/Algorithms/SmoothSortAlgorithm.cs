using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SmoothSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var leonardo = BuildLeonardoSequence(Length);
        EmitEvent(SortEventType.MarkStage, value: 4000 + leonardo.Count);

        // Build stage (smooth-sort inspired heap building)
        for (var i = Length / 2 - 1; i >= 0; i--)
        {
            EmitEvent(SortEventType.MarkStage, i, value: 4100, aux: i);
            SiftDown(i, Length, isExtractStage: false);
        }

        // Extract stage
        for (var end = Length - 1; end > 0; end--)
        {
            EmitEvent(SortEventType.MarkStage, end, value: 4200, aux: end);
            EmitEvent(SortEventType.HeapBoundary, end, value: end);
            MarkRange(0, end);
            Swap(0, end);
            SiftDown(0, end, isExtractStage: true);
        }
    }

    private void SiftDown(int root, int heapSize, bool isExtractStage)
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

            EmitEvent(SortEventType.MarkRange, root, largest, value: isExtractStage ? 1 : 0);
            Swap(root, largest);
            root = largest;
        }
    }

    private static List<int> BuildLeonardoSequence(int max)
    {
        var seq = new List<int> { 1, 1 };
        while (true)
        {
            var next = seq[^1] + seq[^2] + 1;
            if (next > max)
            {
                break;
            }

            seq.Add(next);
        }

        return seq;
    }
}
