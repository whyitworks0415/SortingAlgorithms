using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SplayTreeSortAlgorithm : EventSortAlgorithmBase
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

    private Node Insert(Node? root, int value, int id)
    {
        if (root is null)
        {
            EmitEvent(SortEventType.MarkStructure, id, value: value, aux: 0);
            return new Node(value, id);
        }

        root = Splay(root, value);

        if (value == root.Value)
        {
            root.Count++;
            EmitEvent(SortEventType.MarkStructure, root.Id, value: root.Value, aux: root.Count);
            return root;
        }

        var node = new Node(value, id);
        EmitEvent(SortEventType.MarkStructure, id, value: value, aux: 0);

        if (value < root.Value)
        {
            node.Right = root;
            node.Left = root.Left;
            root.Left = null;
        }
        else
        {
            node.Left = root;
            node.Right = root.Right;
            root.Right = null;
        }

        return node;
    }

    private Node Splay(Node root, int key)
    {
        if (root.Value == key)
        {
            return root;
        }

        if (key < root.Value)
        {
            if (root.Left is null)
            {
                return root;
            }

            if (key < root.Left.Value)
            {
                root.Left.Left = root.Left.Left is null ? null : Splay(root.Left.Left, key);
                root = RotateRight(root);
            }
            else if (key > root.Left.Value)
            {
                root.Left.Right = root.Left.Right is null ? null : Splay(root.Left.Right, key);
                if (root.Left.Right is not null)
                {
                    root.Left = RotateLeft(root.Left);
                }
            }

            return root.Left is null ? root : RotateRight(root);
        }

        if (root.Right is null)
        {
            return root;
        }

        if (key > root.Right.Value)
        {
            root.Right.Right = root.Right.Right is null ? null : Splay(root.Right.Right, key);
            root = RotateLeft(root);
        }
        else if (key < root.Right.Value)
        {
            root.Right.Left = root.Right.Left is null ? null : Splay(root.Right.Left, key);
            if (root.Right.Left is not null)
            {
                root.Right = RotateRight(root.Right);
            }
        }

        return root.Right is null ? root : RotateLeft(root);
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
        EmitEvent(SortEventType.MarkStructure, node.Id, value: node.Value, aux: node.Count);
        for (var i = 0; i < node.Count; i++)
        {
            output.Add(node.Value);
        }

        TraverseInOrder(node.Right, output);
    }
}
