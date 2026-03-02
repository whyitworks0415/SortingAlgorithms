namespace SortingVisualizerApp.Algorithms;

public sealed class NaturalMergeSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var buffer = new int[Length];

        while (true)
        {
            var runs = new List<(int Start, int End)>();
            var start = 0;

            while (start < Length)
            {
                var end = start + 1;
                while (end < Length && Compare(end - 1, end) <= 0)
                {
                    end++;
                }

                runs.Add((start, end));
                start = end;
            }

            if (runs.Count <= 1)
            {
                break;
            }

            for (var r = 0; r < runs.Count; r += 2)
            {
                if (r + 1 >= runs.Count)
                {
                    var tail = runs[r];
                    for (var t = tail.Start; t < tail.End; t++)
                    {
                        buffer[t] = Read(t);
                    }

                    continue;
                }

                var left = runs[r];
                var right = runs[r + 1];
                EventSortCommon.MergeRange(this, buffer, left.Start, right.Start, right.End);
            }

            for (var i = 0; i < Length; i++)
            {
                Write(i, buffer[i]);
            }
        }
    }
}
