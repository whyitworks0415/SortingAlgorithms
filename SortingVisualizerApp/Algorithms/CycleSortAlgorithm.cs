namespace SortingVisualizerApp.Algorithms;

public sealed class CycleSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        for (var cycleStart = 0; cycleStart < Length - 1; cycleStart++)
        {
            MarkRange(cycleStart, Length - 1);
            var item = Read(cycleStart);
            var pos = cycleStart;

            for (var i = cycleStart + 1; i < Length; i++)
            {
                Compare(i, cycleStart);
                if (Read(i) < item)
                {
                    pos++;
                }
            }

            if (pos == cycleStart)
            {
                continue;
            }

            while (pos < Length && item == Read(pos))
            {
                Compare(pos, cycleStart);
                pos++;
            }

            if (pos >= Length)
            {
                continue;
            }

            if (pos != cycleStart)
            {
                var displaced = Read(pos);
                Write(pos, item);
                item = displaced;
            }

            while (pos != cycleStart)
            {
                pos = cycleStart;

                for (var i = cycleStart + 1; i < Length; i++)
                {
                    Compare(i, cycleStart);
                    if (Read(i) < item)
                    {
                        pos++;
                    }
                }

                while (pos < Length && item == Read(pos))
                {
                    Compare(pos, cycleStart);
                    pos++;
                }

                if (pos >= Length)
                {
                    break;
                }

                if (item != Read(pos))
                {
                    var displaced = Read(pos);
                    Write(pos, item);
                    item = displaced;
                }
            }
        }
    }
}
