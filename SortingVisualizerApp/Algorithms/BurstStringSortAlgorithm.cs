using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class BurstStringSortAlgorithm : IStringSortAlgorithm
{
    private sealed class TrieNode
    {
        public SortedDictionary<char, TrieNode> Children { get; } = new();
        public List<int> TerminalRowIndices { get; } = new();
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

        var root = new TrieNode();
        var nodeCount = 1;

        for (var rowIndex = 0; rowIndex < n; rowIndex++)
        {
            var text = rows[rowIndex].Text;
            var node = root;

            for (var depth = 0; depth < text.Length; depth++)
            {
                var symbol = text[depth];
                if (!node.Children.TryGetValue(symbol, out var child))
                {
                    child = new TrieNode();
                    node.Children[symbol] = child;
                    nodeCount++;
                    yield return new SortEvent(
                        SortEventType.TrieBuild,
                        I: depth,
                        Value: nodeCount,
                        Aux: symbol,
                        StepId: step++);
                }

                yield return new SortEvent(
                    SortEventType.CharIndex,
                    I: rowIndex,
                    J: depth,
                    Value: depth,
                    Aux: symbol,
                    StepId: step++);

                node = child;
            }

            node.TerminalRowIndices.Add(rowIndex);
            yield return new SortEvent(
                SortEventType.BucketMove,
                I: rowIndex,
                J: rowIndex,
                Value: text.Length,
                Aux: Math.Clamp(text.Length, 0, 255),
                StepId: step++);
        }

        var orderedSourceIndices = new List<int>(n);
        var traversalEvents = new List<SortEvent>(Math.Max(32, n));
        Traverse(root, orderedSourceIndices, traversalEvents, ref step);
        foreach (var ev in traversalEvents)
        {
            yield return ev;
        }

        var output = new StringItem[n];
        for (var i = 0; i < n; i++)
        {
            output[i] = rows[orderedSourceIndices[i]];
        }

        for (var i = 0; i < n; i++)
        {
            rows[i] = output[i];
            yield return new SortEvent(SortEventType.Write, I: i, Value: rows[i].Id, StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static void Traverse(TrieNode node, List<int> ordered, List<SortEvent> events, ref long step)
    {
        if (node.TerminalRowIndices.Count > 0)
        {
            foreach (var sourceIndex in node.TerminalRowIndices)
            {
                ordered.Add(sourceIndex);
                events.Add(new SortEvent(
                    SortEventType.BucketMove,
                    I: sourceIndex,
                    J: ordered.Count - 1,
                    Value: ordered.Count - 1,
                    Aux: 0,
                    StepId: step++));
            }
        }

        foreach (var pair in node.Children)
        {
            Traverse(pair.Value, ordered, events, ref step);
        }
    }
}
