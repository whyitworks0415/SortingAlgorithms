namespace SortingVisualizerApp.Algorithms;

public sealed class CocktailShakerSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        var start = 0;
        var end = Length - 1;
        var swapped = true;

        while (swapped && start < end)
        {
            MarkRange(start, end);
            swapped = false;

            for (var i = start; i < end; i++)
            {
                if (Compare(i, i + 1) > 0)
                {
                    Swap(i, i + 1);
                    swapped = true;
                }
            }

            if (!swapped)
            {
                break;
            }

            swapped = false;
            end--;

            for (var i = end; i > start; i--)
            {
                if (Compare(i - 1, i) > 0)
                {
                    Swap(i - 1, i);
                    swapped = true;
                }
            }

            start++;
        }
    }
}
