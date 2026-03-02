using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class ThreeWayStringQuickSortAlgorithm : IStringSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(StringItem[] data, StringSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(StringItem[] rows)
    {
        long step = 0;
        if (rows.Length <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        IEnumerable<SortEvent> SortRange(int left, int right, int depth)
        {
            if (left >= right)
            {
                yield break;
            }

            yield return new SortEvent(SortEventType.MarkRange, I: left, J: right, Aux: depth, StepId: step++);
            yield return new SortEvent(SortEventType.PassStart, Value: depth, I: left, J: right, StepId: step++);
            yield return new SortEvent(SortEventType.CharIndex, I: left, Value: left, Aux: depth, StepId: step++);

            var pivot = CharAt(rows[left].Text, depth);
            var lt = left;
            var gt = right;
            var i = left + 1;

            while (i <= gt)
            {
                var current = CharAt(rows[i].Text, depth);
                yield return new SortEvent(SortEventType.CharCompare, I: i, J: left, Value: depth, Aux: current, StepId: step++);
                yield return new SortEvent(SortEventType.CharIndex, I: i, Value: i, Aux: depth, StepId: step++);

                if (current < pivot)
                {
                    if (lt != i)
                    {
                        (rows[lt], rows[i]) = (rows[i], rows[lt]);
                        yield return new SortEvent(SortEventType.Swap, I: lt, J: i, StepId: step++);
                    }

                    lt++;
                    i++;
                    continue;
                }

                if (current > pivot)
                {
                    if (i != gt)
                    {
                        (rows[i], rows[gt]) = (rows[gt], rows[i]);
                        yield return new SortEvent(SortEventType.Swap, I: i, J: gt, StepId: step++);
                    }

                    gt--;
                    continue;
                }

                i++;
            }

            foreach (var ev in SortRange(left, lt - 1, depth))
            {
                yield return ev;
            }

            if (pivot >= 0)
            {
                foreach (var ev in SortRange(lt, gt, depth + 1))
                {
                    yield return ev;
                }
            }

            foreach (var ev in SortRange(gt + 1, right, depth))
            {
                yield return ev;
            }

            yield return new SortEvent(SortEventType.PassEnd, Value: depth, I: left, J: right, StepId: step++);
        }

        foreach (var ev in SortRange(0, rows.Length - 1, depth: 0))
        {
            yield return ev;
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int CharAt(string text, int depth)
    {
        if (depth < 0 || depth >= text.Length)
        {
            return -1;
        }

        return text[depth] & 0xFF;
    }
}
