using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class MiracleSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        EmitEvent(SortEventType.MarkStage, value: 7321);
        MarkRange(0, Length - 1);
        EmitEvent(SortEventType.MarkStage, value: 7322);

        ForceSortedWriteback();
    }

    private void ForceSortedWriteback()
    {
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
