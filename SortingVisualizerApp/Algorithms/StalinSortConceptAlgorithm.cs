using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class StalinSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        EmitEvent(SortEventType.MarkStage, value: 7351);
        var kept = new List<int>(Length);
        var last = Read(0);
        kept.Add(last);
        MarkRange(0, 0);

        for (var i = 1; i < Length; i++)
        {
            if (Compare(i - 1, i) <= 0 && Read(i) >= last)
            {
                last = Read(i);
                kept.Add(last);
                MarkRange(i, i);
            }
            else
            {
                EmitEvent(SortEventType.BadPartition, i, value: Read(i));
            }
        }

        EmitEvent(SortEventType.MarkStage, value: 7352);
        // Preserve multiset/determinism in concept mode by final full sorting writeback.
        var arr = new int[Length];
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = Read(i);
        }

        Array.Sort(arr);
        for (var i = 0; i < arr.Length; i++)
        {
            Write(i, arr[i]);
        }
    }
}
