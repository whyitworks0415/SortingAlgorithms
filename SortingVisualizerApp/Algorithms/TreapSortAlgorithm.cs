using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class TreapSortAlgorithm : EventSortAlgorithmBase
{
    private sealed class Node
    {
        public Node(int value, int id, int priority)
        {
            Value = value;
            Id = id;
            Priority = priority;
        }

        public int Value;
        public int Count = 1;
        public int Id;
        public int Priority;
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
            var value = Read(i);
            var priority = ComputePriority(value, i);
            root = Insert(root, value, i, priority);
        }

        var output = new List<int>(Length);
        TraverseInOrder(root, output);

        for (var i = 0; i < output.Count; i++)
        {
            Write(i, output[i]);
        }
    }

    private Node Insert(Node? node, int value, int id, int priority)
    {
        if (node is null)
        {
            EmitEvent(SortEventType.MarkStructure, id, value: value, aux: priority);
            return new Node(value, id, priority);
        }

        if (value < node.Value)
        {
            node.Left = Insert(node.Left, value, id, priority);
            if (node.Left is not null && node.Left.Priority < node.Priority)
            {
                node = RotateRight(node);
            }
        }
        else if (value > node.Value)
        {
            node.Right = Insert(node.Right, value, id, priority);
            if (node.Right is not null && node.Right.Priority < node.Priority)
            {
                node = RotateLeft(node);
            }
        }
        else
        {
            node.Count++;
            EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: node.Count);
        }

        return node;
    }

    private Node RotateLeft(Node x)
    {
        var y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        EmitEvent(SortEventType.Rotation, y.Id, value: 0, aux: x.Id);
        return y;
    }

    private Node RotateRight(Node y)
    {
        var x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        EmitEvent(SortEventType.Rotation, x.Id, value: 1, aux: y.Id);
        return x;
    }

    private void TraverseInOrder(Node? node, List<int> output)
    {
        if (node is null)
        {
            return;
        }

        TraverseInOrder(node.Left, output);

        EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: node.Priority);
        for (var i = 0; i < node.Count; i++)
        {
            output.Add(node.Value);
        }

        TraverseInOrder(node.Right, output);
    }

    private static int ComputePriority(int value, int index)
    {
        var seed = HashCode.Combine(value, index, 270226);
        return seed & int.MaxValue;
    }
}
