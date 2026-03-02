using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BogoSortConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var random = new Random(HashCode.Combine(Length, 7301));
        var maxAttempts = Math.Min(512, Length * 3 + 32);

        EmitEvent(SortEventType.MarkStage, value: 7301);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            EmitEvent(SortEventType.MarkStage, value: 7302, aux: attempt);
            if (IsSorted())
            {
                return;
            }

            for (var i = Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                Swap(i, j);
            }
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
