using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SkipListSortAlgorithm : EventSortAlgorithmBase
{
    private const int MaxLevel = 16;
    private const double Probability = 0.5;

    private sealed class Node
    {
        public Node(int value, int id, int level)
        {
            Value = value;
            Id = id;
            Forward = new Node?[level];
        }

        public int Value;
        public int Id;
        public Node?[] Forward;
    }

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var random = new Random(HashCode.Combine(Length, Read(0), Read(Length - 1), 270301));
        var head = new Node(int.MinValue, -1, MaxLevel);
        var currentLevel = 1;

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var level = RandomLevel(random);
            EmitEvent(SortEventType.LevelHighlight, level, value: i);

            if (level > currentLevel)
            {
                currentLevel = level;
            }

            var update = new Node[MaxLevel];
            var x = head;
            for (var l = currentLevel - 1; l >= 0; l--)
            {
                while (x.Forward[l] is not null && x.Forward[l]!.Value < value)
                {
                    EmitEvent(SortEventType.MarkStructure, x.Forward[l]!.Id, value: x.Forward[l]!.Value, aux: l);
                    x = x.Forward[l]!;
                }

                update[l] = x;
            }

            var node = new Node(value, i, level);
            EmitEvent(SortEventType.MarkStructure, i, value: value, aux: level);

            for (var l = 0; l < level; l++)
            {
                node.Forward[l] = update[l].Forward[l];
                update[l].Forward[l] = node;
            }
        }

        var outputIndex = 0;
        var cursor = head.Forward[0];
        while (cursor is not null)
        {
            Write(outputIndex++, cursor.Value);
            cursor = cursor.Forward[0];
        }
    }

    private static int RandomLevel(Random random)
    {
        var level = 1;
        while (level < MaxLevel && random.NextDouble() < Probability)
        {
            level++;
        }

        return level;
    }
}
