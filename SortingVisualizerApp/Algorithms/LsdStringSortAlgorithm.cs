using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class LsdStringSortAlgorithm : IStringSortAlgorithm
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

        var maxLen = 0;
        for (var i = 0; i < n; i++)
        {
            maxLen = Math.Max(maxLen, rows[i].Text.Length);
        }

        var aux = new StringItem[n];
        const int radix = 257; // sentinel(0) + byte range [1..256]
        var counts = new int[radix + 1];

        for (var pass = maxLen - 1; pass >= 0; pass--)
        {
            yield return new SortEvent(SortEventType.PassStart, Value: pass, StepId: step++);
            Array.Clear(counts, 0, counts.Length);

            for (var i = 0; i < n; i++)
            {
                var bucket = CharBucket(rows[i].Text, pass);
                counts[bucket + 1]++;
                yield return new SortEvent(SortEventType.BucketMove, I: i, J: i, Value: pass, Aux: bucket, StepId: step++);
            }

            for (var r = 0; r < radix; r++)
            {
                counts[r + 1] += counts[r];
            }

            for (var i = 0; i < n; i++)
            {
                var bucket = CharBucket(rows[i].Text, pass);
                var destination = counts[bucket]++;
                aux[destination] = rows[i];

                yield return new SortEvent(SortEventType.BucketMove, I: i, J: destination, Value: pass, Aux: bucket, StepId: step++);
                yield return new SortEvent(SortEventType.Write, I: destination, Value: rows[i].Id, Aux: pass, StepId: step++);
            }

            Array.Copy(aux, rows, n);
            yield return new SortEvent(SortEventType.PassEnd, Value: pass, StepId: step++);
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
