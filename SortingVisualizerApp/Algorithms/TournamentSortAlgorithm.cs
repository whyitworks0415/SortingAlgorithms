namespace SortingVisualizerApp.Algorithms;

public sealed class TournamentSortAlgorithm : EventSortAlgorithmBase
{
    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var removed = new bool[Length];
        var output = new int[Length];

        for (var outIndex = 0; outIndex < Length; outIndex++)
        {
            MarkRange(0, Length - 1);

            var contenders = new List<int>(Length - outIndex);
            for (var i = 0; i < Length; i++)
            {
                if (!removed[i])
                {
                    contenders.Add(i);
                }
            }

            while (contenders.Count > 1)
            {
                var nextRound = new List<int>((contenders.Count + 1) / 2);
                for (var c = 0; c < contenders.Count; c += 2)
                {
                    if (c + 1 >= contenders.Count)
                    {
                        nextRound.Add(contenders[c]);
                        continue;
                    }

                    var a = contenders[c];
                    var b = contenders[c + 1];
                    nextRound.Add(Compare(a, b) <= 0 ? a : b);
                }

                contenders = nextRound;
            }

            var winner = contenders[0];
            MarkPivot(winner);
            output[outIndex] = Read(winner);
            removed[winner] = true;

            // Keep removed slots out of future winner rounds in the bar view.
            Write(winner, int.MaxValue - outIndex);
        }

        for (var i = 0; i < Length; i++)
        {
            Write(i, output[i]);
        }
    }
}
