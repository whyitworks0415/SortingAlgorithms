namespace SortingVisualizerApp.Algorithms;

public sealed class BingoSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var next = 0;
        while (next < Length)
        {
            MarkRange(next, Length - 1);

            var minIndex = next;
            for (var i = next + 1; i < Length; i++)
            {
                if (Compare(i, minIndex) < 0)
                {
                    minIndex = i;
                }
            }

            var minValue = Read(minIndex);
            MarkPivot(minIndex);

            var iScan = next;
            while (iScan < Length)
            {
                Compare(iScan, minIndex);
                if (Read(iScan) == minValue)
                {
                    Swap(iScan, next);
                    next++;
                    if (iScan < next)
                    {
                        iScan = next;
                    }
                }
                else
                {
                    iScan++;
                }
            }
        }
    }
}
