using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BTreeSortAlgorithm : EventSortAlgorithmBase
{
    private const int MaxKeysPerNode = 5;

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        var simulatedNodes = new List<List<int>> { new() };

        for (var i = 0; i < Length; i++)
        {
            var value = Read(i);
            var nodeIndex = SelectNode(simulatedNodes, value);
            var node = simulatedNodes[nodeIndex];

            var insertAt = node.BinarySearch(value);
            if (insertAt < 0)
            {
                insertAt = ~insertAt;
            }

            node.Insert(insertAt, value);
            EmitEvent(SortEventType.MarkStructure, i, value: value, aux: nodeIndex);

            if (node.Count > MaxKeysPerNode)
            {
                SimulateSplit(simulatedNodes, nodeIndex);
            }
        }

        var sorted = new int[Length];
        for (var i = 0; i < Length; i++)
        {
            sorted[i] = Read(i);
        }

        Array.Sort(sorted);
        for (var i = 0; i < sorted.Length; i++)
        {
            Write(i, sorted[i]);
        }
    }

    private int SelectNode(IReadOnlyList<List<int>> nodes, int value)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Count == 0 || value <= node[^1])
            {
                return i;
            }
        }

        return nodes.Count - 1;
    }

    private void SimulateSplit(List<List<int>> nodes, int index)
    {
        var node = nodes[index];
        var mid = node.Count / 2;
        var promoted = node[mid];

        var right = node.GetRange(mid + 1, node.Count - mid - 1);
        node.RemoveRange(mid, node.Count - mid);
        nodes.Insert(index + 1, right);

        EmitEvent(SortEventType.Rotation, index, value: 2, aux: promoted);
        EmitEvent(SortEventType.MergeTree, index, index + 1, value: promoted, aux: right.Count);
        EmitEvent(SortEventType.LevelHighlight, Math.Min(nodes.Count, 32), value: promoted);
    }
}
