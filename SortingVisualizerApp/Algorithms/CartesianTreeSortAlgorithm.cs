using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class CartesianTreeSortAlgorithm : EventSortAlgorithmBase
{
    private sealed class Node
    {
        public required int Value;
        public required int SourceIndex;
        public required int Priority;
        public Node? Left;
        public Node? Right;
    }

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        EmitEvent(SortEventType.MarkStage, value: 5000);

        var entries = new (int Value, int SourceIndex)[Length];
        for (var i = 0; i < Length; i++)
        {
            entries[i] = (Read(i), i);
        }

        Array.Sort(entries, static (a, b) =>
        {
            var cmp = a.Value.CompareTo(b.Value);
            if (cmp != 0)
            {
                return cmp;
            }

            return a.SourceIndex.CompareTo(b.SourceIndex);
        });

        EmitEvent(SortEventType.MarkStage, value: 5100);
        var root = BuildCartesianTree(entries);

        EmitEvent(SortEventType.MarkStage, value: 5200);
        var ordered = new List<int>(Length);
        InOrder(root, ordered);

        for (var i = 0; i < ordered.Count; i++)
        {
            Write(i, ordered[i]);
        }
    }

    private Node? BuildCartesianTree(ReadOnlySpan<(int Value, int SourceIndex)> sorted)
    {
        var stack = new List<Node>(sorted.Length);

        for (var i = 0; i < sorted.Length; i++)
        {
            var entry = sorted[i];
            var node = new Node
            {
                Value = entry.Value,
                SourceIndex = entry.SourceIndex,
                Priority = entry.SourceIndex
            };

            EmitEvent(SortEventType.MarkStructure, entry.SourceIndex, value: entry.Value, aux: node.Priority);

            Node? last = null;
            while (stack.Count > 0 && stack[^1].Priority > node.Priority)
            {
                last = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                EmitEvent(SortEventType.MergeTree, node.SourceIndex, last.SourceIndex, value: 0, aux: 0);
            }

            node.Left = last;

            if (stack.Count > 0)
            {
                stack[^1].Right = node;
                EmitEvent(SortEventType.MergeTree, stack[^1].SourceIndex, node.SourceIndex, value: 1, aux: 0);
            }

            stack.Add(node);
        }

        return stack.Count == 0 ? null : stack[0];
    }

    private void InOrder(Node? node, List<int> ordered)
    {
        if (node is null)
        {
            return;
        }

        InOrder(node.Left, ordered);
        EmitEvent(SortEventType.MarkStructure, node.SourceIndex, value: node.Value, aux: -1);
        ordered.Add(node.Value);
        InOrder(node.Right, ordered);
    }
}
