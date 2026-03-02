namespace SortingVisualizerApp.Algorithms;

public sealed class GnomeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var index = 1;
        while (index < Length)
        {
            MarkRange(0, Length - 1);
            if (index == 0)
            {
                index = 1;
                continue;
            }

            if (Compare(index - 1, index) <= 0)
            {
                index++;
            }
            else
            {
                Swap(index - 1, index);
                index--;
            }
        }
    }
}
