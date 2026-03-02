namespace SortingVisualizerApp.Algorithms;

public sealed class PatienceSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var piles = new List<Stack<int>>();
        var tops = new List<int>();

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var lo = 0;
            var hi = tops.Count;
            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                if (tops[mid] >= value)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }

            var pileIndex = lo;
            if (pileIndex == piles.Count)
            {
                piles.Add(new Stack<int>());
                tops.Add(value);
            }

            piles[pileIndex].Push(value);
            tops[pileIndex] = piles[pileIndex].Peek();
            MarkBucket(i, pileIndex, value);
        }

        var queue = new PriorityQueue<(int Value, int Pile), (int Value, int Pile)>();
        for (var pileIndex = 0; pileIndex < piles.Count; pileIndex++)
        {
            if (piles[pileIndex].Count > 0)
            {
                var top = piles[pileIndex].Peek();
                queue.Enqueue((top, pileIndex), (top, pileIndex));
            }
        }

        var outIndex = 0;
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            var pile = piles[next.Pile];
            var value = pile.Pop();
            Write(outIndex++, value);

            if (pile.Count > 0)
            {
                var top = pile.Peek();
                queue.Enqueue((top, next.Pile), (top, next.Pile));
            }
        }
    }
}
