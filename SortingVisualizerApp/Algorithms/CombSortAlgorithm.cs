namespace SortingVisualizerApp.Algorithms;

public sealed class CombSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var gap = Length;
        var swapped = true;

        while (gap > 1 || swapped)
        {
            gap = Math.Max(1, (int)(gap / 1.3));
            swapped = false;
            MarkRange(0, Length - 1);

            for (var i = 0; i + gap < Length; i++)
            {
                var j = i + gap;
                if (Compare(i, j) > 0)
                {
                    Swap(i, j);
                    swapped = true;
                }
            }
        }
    }
}
