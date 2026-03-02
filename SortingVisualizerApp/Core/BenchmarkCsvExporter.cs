using System.Globalization;
using System.Text;

namespace SortingVisualizerApp.Core;

public static class BenchmarkCsvExporter
{
    public static string SaveToDefaultPath(string appDataRoot, BenchmarkSuiteResult suiteResult)
    {
        var directory = Path.Combine(appDataRoot, "bench");
        Directory.CreateDirectory(directory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"{timestamp}.csv");

        Save(path, suiteResult);
        return path;
    }

    public static void Save(string path, BenchmarkSuiteResult suiteResult)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        var builder = new StringBuilder(4096);
        builder.AppendLine("timestamp_utc,algorithm_id,algorithm_name,n,distribution,seed,elapsed_ms,comparisons,swaps,writes,processed_events,completed,sorted,multiset_preserved,error");

        foreach (var row in suiteResult.Results)
        {
            builder.Append(Escape(suiteResult.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(Escape(row.AlgorithmId)).Append(',');
            builder.Append(Escape(row.AlgorithmName)).Append(',');
            builder.Append(row.Size.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(Escape(row.Distribution.ToString())).Append(',');
            builder.Append(row.Seed.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.ElapsedMs.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Comparisons.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Swaps.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Writes.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.ProcessedEvents.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Completed ? "true" : "false").Append(',');
            builder.Append(row.Sorted ? "true" : "false").Append(',');
            builder.Append(row.MultisetPreserved ? "true" : "false").Append(',');
            builder.Append(Escape(row.Error ?? string.Empty));
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
