namespace SortingVisualizerApp.Algorithms;

public sealed class SelectionSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        for (var i = 0; i < Length - 1; i++)
        {
            MarkRange(i, Length - 1);
            var minIndex = i;
            MarkPivot(minIndex);

            for (var j = i + 1; j < Length; j++)
            {
                if (Compare(j, minIndex) < 0)
                {
                    minIndex = j;
                    MarkPivot(minIndex);
                }
            }

            Swap(i, minIndex);
        }
    }
}
