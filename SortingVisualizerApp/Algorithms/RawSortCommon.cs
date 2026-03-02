using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

internal static class RawSortCommon
{
    public static int LomutoPartition(int[] values, int left, int right, ref long step, IList<SortEvent> output)
    {
        output.Add(new SortEvent(SortEventType.MarkRange, left, right, StepId: step++));
        output.Add(new SortEvent(SortEventType.MarkPivot, right, Value: values[right], StepId: step++));

        var pivotValue = values[right];
        var i = left;

        for (var j = left; j < right; j++)
        {
            output.Add(new SortEvent(SortEventType.Compare, j, right, StepId: step++));
            if (values[j] > pivotValue)
            {
                continue;
            }

            if (i != j)
            {
                (values[i], values[j]) = (values[j], values[i]);
                output.Add(new SortEvent(SortEventType.Swap, i, j, StepId: step++));
            }

            i++;
        }

        if (i != right)
        {
            (values[i], values[right]) = (values[right], values[i]);
            output.Add(new SortEvent(SortEventType.Swap, i, right, StepId: step++));
        }

        var leftSize = i - left;
        var rightSize = right - i;
        var quality = Math.Min(leftSize, rightSize) / (double)Math.Max(1, Math.Max(leftSize, rightSize));
        output.Add(new SortEvent(SortEventType.PartitionInfo, left, right, Value: (int)Math.Round(Math.Clamp(quality, 0.0, 1.0) * 1000.0), Aux: i, StepId: step++));
        if (quality < 0.2)
        {
            output.Add(new SortEvent(SortEventType.BadPartition, i, left, Value: leftSize, Aux: rightSize, StepId: step++));
        }

        return i;
    }
}
