namespace SortingVisualizerApp.Algorithms;

public sealed class AdaptiveMergeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var buffer = new int[Length];
        var runs = DetectRuns();

        while (runs.Count > 1)
        {
            var next = new List<(int Start, int EndExclusive)>((runs.Count + 1) / 2);
            for (var i = 0; i < runs.Count; i += 2)
            {
                if (i + 1 >= runs.Count)
                {
                    next.Add(runs[i]);
                    continue;
                }

                var left = runs[i];
                var right = runs[i + 1];
                Merge(buffer, left.Start, left.EndExclusive, right.EndExclusive);
                next.Add((left.Start, right.EndExclusive));
            }

            runs = next;
        }
    }

    private List<(int Start, int EndExclusive)> DetectRuns()
    {
        var runs = new List<(int Start, int EndExclusive)>();
        var start = 0;

        while (start < Length)
        {
            var end = start + 1;
            while (end < Length)
            {
                if (Compare(end - 1, end) > 0)
                {
                    break;
                }

                end++;
            }

            runs.Add((start, end));
            start = end;
        }

        return runs;
    }

    private void Merge(int[] buffer, int left, int mid, int rightExclusive)
    {
        MarkRange(left, rightExclusive - 1);
        var i = left;
        var j = mid;
        var k = left;

        while (i < mid && j < rightExclusive)
        {
            if (Compare(i, j) <= 0)
            {
                buffer[k++] = Read(i++);
            }
            else
            {
                buffer[k++] = Read(j++);
            }
        }

        while (i < mid)
        {
            buffer[k++] = Read(i++);
        }

        while (j < rightExclusive)
        {
            buffer[k++] = Read(j++);
        }

        for (var index = left; index < rightExclusive; index++)
        {
            Write(index, buffer[index]);
        }
    }
}
