namespace SortingVisualizerApp.Algorithms;

public sealed class OptimizedBubbleSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        var n = Length;
        while (n > 1)
        {
            MarkRange(0, n - 1);
            var lastSwap = 0;

            for (var i = 1; i < n; i++)
            {
                if (Compare(i - 1, i) > 0)
                {
                    Swap(i - 1, i);
                    lastSwap = i;
                }
            }

            if (lastSwap == 0)
            {
                break;
            }

            n = lastSwap;
        }
    }
}
