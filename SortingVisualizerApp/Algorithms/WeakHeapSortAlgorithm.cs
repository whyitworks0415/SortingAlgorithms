namespace SortingVisualizerApp.Algorithms;

public sealed class WeakHeapSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var bits = new int[Length];

        for (var i = Length - 1; i >= 1; i--)
        {
            var j = i;
            var parent = j >> 1;
            while (parent > 0 && ((j & 1) == bits[parent]))
            {
                j = parent;
                parent = j >> 1;
            }

            WeakHeapMerge(parent, i, bits);
        }

        for (var end = Length - 1; end >= 2; end--)
        {
            MarkRange(0, end);
            Swap(0, end);

            var x = 1;
            while (true)
            {
                var child = (x << 1) + bits[x];
                if (child < end)
                {
                    x = child;
                }
                else
                {
                    break;
                }
            }

            while (x > 0)
            {
                WeakHeapMerge(0, x, bits);
                x >>= 1;
            }
        }

        Swap(0, 1);
    }

    private void WeakHeapMerge(int i, int j, int[] bits)
    {
        if (Compare(i, j) < 0)
        {
            Swap(i, j);
            bits[j] ^= 1;
        }
    }
}
