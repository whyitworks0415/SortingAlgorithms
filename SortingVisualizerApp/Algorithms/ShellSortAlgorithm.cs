namespace SortingVisualizerApp.Algorithms;

public sealed class ShellSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        for (var gap = Length / 2; gap > 0; gap /= 2)
        {
            MarkRange(0, Length - 1);
            for (var i = gap; i < Length; i++)
            {
                var value = Read(i);
                MarkPivotValue(i, value);

                var j = i;
                while (j >= gap)
                {
                    Compare(j - gap, i);
                    if (Read(j - gap) <= value)
                    {
                        break;
                    }

                    Write(j, Read(j - gap));
                    j -= gap;
                }

                Write(j, value);
            }
        }
    }
}
