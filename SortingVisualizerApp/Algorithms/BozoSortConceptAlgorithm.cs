using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BozoSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var random = new Random(HashCode.Combine(Length, 7302));
        var maxAttempts = Math.Min(1024, Length * 5 + 64);

        EmitEvent(SortEventType.MarkStage, value: 7311);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            EmitEvent(SortEventType.MarkStage, value: 7312, aux: attempt);
            if (IsSorted())
            {
                return;
            }

            var i = random.Next(Length);
            var j = random.Next(Length);
            Swap(i, j);
        }

        ForceSortedWriteback();
    }

    private bool IsSorted()
    {
        for (var i = 1; i < Length; i++)
        {
            if (Compare(i - 1, i) > 0)
            {
                return false;
            }
        }

        return true;
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
