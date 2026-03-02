namespace SortingVisualizerApp.Algorithms;

public sealed class OddEvenSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var sorted = false;
        while (!sorted)
        {
            sorted = true;
            MarkRange(0, Length - 1);

            for (var i = 1; i < Length - 1; i += 2)
            {
                if (Compare(i, i + 1) > 0)
                {
                    Swap(i, i + 1);
                    sorted = false;
                }
            }

            for (var i = 0; i < Length - 1; i += 2)
            {
                if (Compare(i, i + 1) > 0)
                {
                    Swap(i, i + 1);
                    sorted = false;
                }
            }
        }
    }
}
