using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class MsdStringSortAlgorithm : IStringSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(StringItem[] data, StringSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(StringItem[] rows)
    {
        long step = 0;
        var n = rows.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var aux = new StringItem[n];
        const int radix = 257; // sentinel(0) + byte range [1..256]

        var stack = new Stack<(int Start, int EndExclusive, int Depth)>();
        stack.Push((0, n, 0));

        while (stack.Count > 0)
        {
            var (start, endExclusive, depth) = stack.Pop();
            if (endExclusive - start <= 1)
            {
                continue;
            }

            yield return new SortEvent(SortEventType.PassStart, Value: depth, I: start, J: endExclusive - 1, StepId: step++);

            var counts = new int[radix + 1];
            for (var i = start; i < endExclusive; i++)
            {
                var bucket = CharBucket(rows[i].Text, depth);
                counts[bucket + 1]++;
                yield return new SortEvent(SortEventType.BucketMove, I: i, J: i, Value: depth, Aux: bucket, StepId: step++);
            }

            for (var r = 0; r < radix; r++)
            {
                counts[r + 1] += counts[r];
            }

            var offsets = counts.ToArray();
            for (var i = start; i < endExclusive; i++)
            {
                var bucket = CharBucket(rows[i].Text, depth);
                var destination = start + offsets[bucket]++;
                aux[destination] = rows[i];

                yield return new SortEvent(SortEventType.BucketMove, I: i, J: destination, Value: depth, Aux: bucket, StepId: step++);
                yield return new SortEvent(SortEventType.Write, I: destination, Value: rows[i].Id, Aux: depth, StepId: step++);
            }

            Array.Copy(aux, start, rows, start, endExclusive - start);
            yield return new SortEvent(SortEventType.PassEnd, Value: depth, I: start, J: endExclusive - 1, StepId: step++);

            for (var bucket = radix - 1; bucket >= 1; bucket--)
            {
                var bucketStart = start + counts[bucket];
                var bucketEnd = start + counts[bucket + 1];
                if (bucketEnd - bucketStart > 1)
                {
                    stack.Push((bucketStart, bucketEnd, depth + 1));
                }
            }
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int CharBucket(string text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return 0;
        }

        return 1 + (text[index] & 0xFF);
    }
}
