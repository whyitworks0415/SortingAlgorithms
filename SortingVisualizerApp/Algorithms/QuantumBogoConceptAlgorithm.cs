using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class QuantumBogoConceptAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var random = new Random(HashCode.Combine(Length, 7331));
        EmitEvent(SortEventType.MarkStage, value: 7331);

        var observedSorted = false;
        for (var attempt = 0; attempt < 32; attempt++)
        {
            EmitEvent(SortEventType.MarkStage, value: 7332, aux: attempt);
            if (random.NextDouble() < 0.07 && IsSorted())
            {
                observedSorted = true;
                break;
            }

            var i = random.Next(Length);
            var j = random.Next(Length);
            Swap(i, j);
        }

        if (!observedSorted)
        {
            EmitEvent(SortEventType.MarkStage, value: 7333);
            ForceSortedWriteback();
        }
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
