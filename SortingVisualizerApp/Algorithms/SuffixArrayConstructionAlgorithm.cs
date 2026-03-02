using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Algorithms;

public sealed class SuffixArrayConstructionAlgorithm : IStringSortAlgorithm
{
    public IEnumerable<SortEvent> Execute(StringItem[] data, StringSortOptions options)
    {
        return ExecuteIterator(data.ToArray());
    }

    public static int[] BuildSuffixArray(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<int>();
        }

        var tokens = new int[text.Length + 1];
        for (var i = 0; i < text.Length; i++)
        {
            tokens[i] = text[i] + 1;
        }

        tokens[^1] = 0;
        var sa = BuildSuffixArrayCore(tokens);
        return sa.Where(index => index < text.Length).ToArray();
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

        var totalChars = rows.Sum(static item => item.Text.Length + 1);
        var tokens = new int[Math.Max(1, totalChars)];
        var rowStartPositions = new int[n];
        var tokenCount = 0;

        for (var row = 0; row < n; row++)
        {
            rowStartPositions[row] = tokenCount;
            var text = rows[row].Text;
            for (var c = 0; c < text.Length; c++)
            {
                tokens[tokenCount++] = 512 + (text[c] & 0xFF);
            }

            tokens[tokenCount++] = row + 1;
        }

        if (tokenCount == 0)
        {
            yield return new SortEvent(SortEventType.Done, StepId: step);
            yield break;
        }

        var compactTokens = new int[tokenCount];
        Array.Copy(tokens, compactTokens, tokenCount);

        IEnumerable<SortEvent> EmitSuffixArrayConstruction(int[] buildTokens)
        {
            var m = buildTokens.Length;
            var saBuild = Enumerable.Range(0, m).ToArray();
            var rankBuild = buildTokens.ToArray();
            var tempBuild = new int[m];

            var pass = 0;
            for (var k = 1; k < m; k <<= 1, pass++)
            {
                yield return new SortEvent(SortEventType.PassStart, Value: pass, I: k, J: m, StepId: step++);
                yield return new SortEvent(SortEventType.CharIndex, Value: pass, Aux: k, StepId: step++);

                Array.Sort(saBuild, (a, b) =>
                {
                    var cmp = rankBuild[a].CompareTo(rankBuild[b]);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    var ra = a + k < m ? rankBuild[a + k] : -1;
                    var rb = b + k < m ? rankBuild[b + k] : -1;
                    cmp = ra.CompareTo(rb);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    return a.CompareTo(b);
                });

                tempBuild[saBuild[0]] = 0;
                var classes = 0;
                var sampleStride = Math.Max(1, m / 128);

                for (var i = 1; i < m; i++)
                {
                    var prev = saBuild[i - 1];
                    var curr = saBuild[i];
                    yield return new SortEvent(SortEventType.CharCompare, I: prev, J: curr, Value: pass, Aux: k, StepId: step++);

                    var prevSecond = prev + k < m ? rankBuild[prev + k] : -1;
                    var currSecond = curr + k < m ? rankBuild[curr + k] : -1;
                    if (rankBuild[prev] != rankBuild[curr] || prevSecond != currSecond)
                    {
                        classes++;
                    }

                    tempBuild[curr] = classes;
                    if ((i % sampleStride) == 0 || i == m - 1)
                    {
                        yield return new SortEvent(SortEventType.BucketMove, I: i, J: curr, Value: pass, Aux: Math.Clamp(classes, 0, 255), StepId: step++);
                    }
                }

                (rankBuild, tempBuild) = (tempBuild, rankBuild);
                yield return new SortEvent(SortEventType.PassEnd, Value: pass, I: classes, J: k, StepId: step++);
                if (classes == m - 1)
                {
                    break;
                }
            }
        }

        foreach (var ev in EmitSuffixArrayConstruction(compactTokens))
        {
            yield return ev;
        }

        var suffixArray = BuildSuffixArrayCore(compactTokens);
        var rowByStart = new Dictionary<int, int>(n);
        for (var i = 0; i < n; i++)
        {
            rowByStart[rowStartPositions[i]] = i;
        }

        var sortedRows = new List<StringItem>(n);
        for (var i = 0; i < suffixArray.Length; i++)
        {
            if (!rowByStart.TryGetValue(suffixArray[i], out var rowIndex))
            {
                continue;
            }

            sortedRows.Add(rows[rowIndex]);
        }

        if (sortedRows.Count != n)
        {
            sortedRows = rows
                .OrderBy(static item => item.Text, StringComparer.Ordinal)
                .ThenBy(static item => item.OriginalIndex)
                .ToList();
        }

        for (var i = 0; i < sortedRows.Count; i++)
        {
            yield return new SortEvent(SortEventType.Write, I: i, Value: sortedRows[i].Id, Aux: 0, StepId: step++);
        }

        yield return new SortEvent(SortEventType.Done, StepId: step);
    }

    private static int[] BuildSuffixArrayCore(int[] tokens)
    {
        var n = tokens.Length;
        if (n <= 1)
        {
            return Enumerable.Range(0, n).ToArray();
        }

        var sa = Enumerable.Range(0, n).ToArray();
        var rank = tokens.ToArray();
        var temp = new int[n];

        for (var k = 1; k < n; k <<= 1)
        {
            Array.Sort(sa, (a, b) =>
            {
                var cmp = rank[a].CompareTo(rank[b]);
                if (cmp != 0)
                {
                    return cmp;
                }

                var ra = a + k < n ? rank[a + k] : -1;
                var rb = b + k < n ? rank[b + k] : -1;
                cmp = ra.CompareTo(rb);
                if (cmp != 0)
                {
                    return cmp;
                }

                return a.CompareTo(b);
            });

            temp[sa[0]] = 0;
            var classes = 0;
            for (var i = 1; i < n; i++)
            {
                var prev = sa[i - 1];
                var curr = sa[i];
                var prevSecond = prev + k < n ? rank[prev + k] : -1;
                var currSecond = curr + k < n ? rank[curr + k] : -1;
                if (rank[prev] != rank[curr] || prevSecond != currSecond)
                {
                    classes++;
                }

                temp[curr] = classes;
            }

            (rank, temp) = (temp, rank);
            if (classes == n - 1)
            {
                break;
            }
        }

        return sa;
    }
}
