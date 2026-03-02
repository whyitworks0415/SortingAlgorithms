using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class AvlTreeSortAlgorithm : EventSortAlgorithmBase
{
    private sealed class Node
    {
        public Node(int value, int id)
        {
            Value = value;
            Id = id;
        }

        public int Value;
        public int Count = 1;
        public int Height = 1;
        public int Id;
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
        }

        var output = new List<int>(Length);
        TraverseInOrder(root, output);

        for (var i = 0; i < output.Count; i++)
        {
            Write(i, output[i]);
        }
    }

    private Node Insert(Node? node, int value, int nodeId)
    {
        if (node is null)
        {
            EmitEvent(SortEventType.MarkStructure, nodeId, value: value, aux: 0);
            return new Node(value, nodeId);
        }

        if (value < node.Value)
        {
            node.Left = Insert(node.Left, value, nodeId);
        }
        else if (value > node.Value)
        {
            node.Right = Insert(node.Right, value, nodeId);
        }
        else
        {
            node.Count++;
            EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: node.Count);
            return node;
        }

        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        var balance = HeightOf(node.Left) - HeightOf(node.Right);

        if (balance > 1)
        {
            if (value < node.Left!.Value)
            {
                return RotateRight(node);
            }

            node.Left = RotateLeft(node.Left!);
            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (value > node.Right!.Value)
            {
                return RotateLeft(node);
            }

            node.Right = RotateRight(node.Right!);
            return RotateLeft(node);
        }

        return node;
    }

    private Node RotateLeft(Node x)
    {
        var y = x.Right!;
        var t2 = y.Left;

        y.Left = x;
        x.Right = t2;

        x.Height = 1 + Math.Max(HeightOf(x.Left), HeightOf(x.Right));
        y.Height = 1 + Math.Max(HeightOf(y.Left), HeightOf(y.Right));

        EmitEvent(SortEventType.Rotation, y.Id, value: 0, aux: x.Id);
        return y;
    }

    private Node RotateRight(Node y)
    {
        var x = y.Left!;
        var t2 = x.Right;

        x.Right = y;
        y.Left = t2;

        y.Height = 1 + Math.Max(HeightOf(y.Left), HeightOf(y.Right));
        x.Height = 1 + Math.Max(HeightOf(x.Left), HeightOf(x.Right));

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

        EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: -1);
        for (var i = 0; i < node.Count; i++)
        {
            output.Add(node.Value);
        }

        TraverseInOrder(node.Right, output);
    }

    private static int HeightOf(Node? node)
    {
        return node?.Height ?? 0;
    }
}
