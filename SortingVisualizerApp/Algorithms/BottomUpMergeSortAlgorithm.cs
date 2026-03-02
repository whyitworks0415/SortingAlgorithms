namespace SortingVisualizerApp.Algorithms;

public sealed class BottomUpMergeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var buffer = new int[Length];

        for (var width = 1; width < Length; width *= 2)
        {
            for (var left = 0; left < Length; left += 2 * width)
            {
                var mid = Math.Min(left + width, Length);
                var right = Math.Min(left + (2 * width), Length);

                if (mid >= right)
                {
                    continue;
                }

                EventSortCommon.MergeRange(this, buffer, left, mid, right);
            }
        }
    }
}
