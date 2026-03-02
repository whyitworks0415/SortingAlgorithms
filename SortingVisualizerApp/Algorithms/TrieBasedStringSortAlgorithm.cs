using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class TrieBasedStringSortAlgorithm : IStringSortAlgorithm
{
    private sealed class TrieNode
    {
        public int NodeId { get; init; }
        public SortedDictionary<int, TrieNode> Children { get; } = new();
        public List<StringItem> TerminalItems { get; } = new();
    }

    public IEnumerable<SortEvent> Execute(StringItem[] data, StringSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    private static IEnumerable<SortEvent> ExecuteIterator(StringItem[] rows)
    {
        long step = 0;
        var n = rows.Length;
        if (n <= 1)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var rowIndexById = new Dictionary<int, int>(n);
        for (var i = 0; i < n; i++)
        {
            rowIndexById[rows[i].Id] = i;
        }

        var root = new TrieNode { NodeId = 0 };
        var nextNodeId = 1;

        for (var rowIndex = 0; rowIndex < n; rowIndex++)
        {
            var text = rows[rowIndex].Text;
            var node = root;
            yield return new SortEvent(SortEventType.PassStart, Value: 0, I: rowIndex, StepId: step++);

            for (var depth = 0; depth < text.Length; depth++)
            {
                var bucket = text[depth] & 0xFF;
                yield return new SortEvent(SortEventType.CharIndex, I: rowIndex, Value: rowIndex, Aux: depth, StepId: step++);
                yield return new SortEvent(SortEventType.BucketMove, I: rowIndex, J: rowIndex, Value: depth, Aux: bucket, StepId: step++);

                if (!node.Children.TryGetValue(bucket, out var child))
                {
                    child = new TrieNode { NodeId = nextNodeId++ };
                    node.Children[bucket] = child;
                    yield return new SortEvent(SortEventType.MarkStage, Value: child.NodeId, I: depth, StepId: step++);
                }
                else
                {
                    yield return new SortEvent(SortEventType.CharCompare, I: rowIndex, J: rowIndex, Value: depth, Aux: bucket, StepId: step++);
                }

                node = child;
            }

            node.TerminalItems.Add(rows[rowIndex]);
            yield return new SortEvent(SortEventType.PassEnd, Value: text.Length, I: rowIndex, StepId: step++);
        }

        var ordered = new List<StringItem>(n);

        IEnumerable<SortEvent> Traverse(TrieNode node, int depth)
        {
            foreach (var item in node.TerminalItems)
            {
                ordered.Add(item);
                yield return new SortEvent(SortEventType.CharIndex, I: ordered.Count - 1, Value: ordered.Count - 1, Aux: depth, StepId: step++);
            }

            foreach (var pair in node.Children)
            {
                var bucket = pair.Key;
                var child = pair.Value;
                yield return new SortEvent(SortEventType.PassStart, Value: depth, I: child.NodeId, Aux: bucket, StepId: step++);
                foreach (var ev in Traverse(child, depth + 1))
                {
                    yield return ev;
                }
                yield return new SortEvent(SortEventType.PassEnd, Value: depth, I: child.NodeId, Aux: bucket, StepId: step++);
            }
        }

        foreach (var ev in Traverse(root, depth: 0))
        {
            yield return ev;
        }

        for (var destination = 0; destination < ordered.Count; destination++)
        {
            var item = ordered[destination];
            rowIndexById.TryGetValue(item.Id, out var sourceIndex);
            yield return new SortEvent(SortEventType.BucketMove, I: sourceIndex, J: destination, Value: 0, Aux: 0, StepId: step++);
            yield return new SortEvent(SortEventType.Write, I: destination, Value: item.Id, Aux: 0, StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }
}
