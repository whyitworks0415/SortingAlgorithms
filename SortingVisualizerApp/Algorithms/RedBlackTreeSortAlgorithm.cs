using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class RedBlackTreeSortAlgorithm : EventSortAlgorithmBase
{
    private sealed class Node
    {
        public Node(int value, int id, bool red)
        {
            Value = value;
            Id = id;
            IsRed = red;
        }

        public int Value;
        public int Count = 1;
        public int Id;
        public bool IsRed;
        public Node? Left;
        public Node? Right;
    }

    protected override void SortCore()
    {
        if (Length <= 1)
        {
            return;
        }

        Node? root = null;
        for (var i = 0; i < Length; i++)
        {
            root = Insert(root, Read(i), i);
            if (root is not null)
            {
                root.IsRed = false;
            }
        }

        var output = new List<int>(Length);
        TraverseInOrder(root, output);
        for (var i = 0; i < output.Count; i++)
        {
            Write(i, output[i]);
        }
    }

    private Node Insert(Node? h, int value, int nodeId)
    {
        if (h is null)
        {
            EmitEvent(SortEventType.MarkStructure, nodeId, value: value, aux: 1);
            return new Node(value, nodeId, red: true);
        }

        if (value < h.Value)
        {
            h.Left = Insert(h.Left, value, nodeId);
        }
        else if (value > h.Value)
        {
            h.Right = Insert(h.Right, value, nodeId);
        }
        else
        {
            h.Count++;
            EmitEvent(SortEventType.MarkStructure, h.Id, value: h.Value, aux: h.Count);
        }

        if (IsRed(h.Right) && !IsRed(h.Left))
        {
            h = RotateLeft(h);
        }

        if (IsRed(h.Left) && IsRed(h.Left!.Left))
        {
            h = RotateRight(h);
        }

        if (IsRed(h.Left) && IsRed(h.Right))
        {
            FlipColors(h);
        }

        return h;
    }

    private Node RotateLeft(Node h)
    {
        var x = h.Right!;
        h.Right = x.Left;
        x.Left = h;
        x.IsRed = h.IsRed;
        h.IsRed = true;

        EmitEvent(SortEventType.Rotation, x.Id, value: 0, aux: h.Id);
        return x;
    }

    private Node RotateRight(Node h)
    {
        var x = h.Left!;
        h.Left = x.Right;
        x.Right = h;
        x.IsRed = h.IsRed;
        h.IsRed = true;

        EmitEvent(SortEventType.Rotation, x.Id, value: 1, aux: h.Id);
        return x;
    }

    private void FlipColors(Node h)
    {
        h.IsRed = !h.IsRed;
        if (h.Left is not null)
        {
            h.Left.IsRed = !h.Left.IsRed;
        }

        if (h.Right is not null)
        {
            h.Right.IsRed = !h.Right.IsRed;
        }

        EmitEvent(SortEventType.LevelHighlight, 0, value: h.Id);
    }

    private void TraverseInOrder(Node? node, List<int> output)
    {
        if (node is null)
        {
            return;
        }

        TraverseInOrder(node.Left, output);
        EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: node.IsRed ? 1 : 0);
        for (var i = 0; i < node.Count; i++)
        {
            output.Add(node.Value);
        }

        TraverseInOrder(node.Right, output);
    }

    private static bool IsRed(Node? node)
    {
        return node is not null && node.IsRed;
    }
}
