using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SortingVisualizerApp.Core;

public static class AnalysisExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SaveComparisonCsv(string appDataRoot, IReadOnlyList<ComparisonAnalysisRecord> records)
    {
        var directory = EnsureAnalysisDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_comparison.csv");
        WriteComparisonCsv(path, records);
        return path;
    }

    public static string SaveComparisonJson(string appDataRoot, IReadOnlyList<ComparisonAnalysisRecord> records)
    {
        var directory = EnsureAnalysisDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_comparison.json");
        SaveJson(path, records);
        return path;
    }

    public static string SaveGrowthCsv(string appDataRoot, GrowthBenchmarkSuiteResult suiteResult)
    {
        var directory = EnsureAnalysisDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_growth.csv");
        WriteGrowthCsv(path, suiteResult);
        return path;
    }

    public static string SaveGrowthJson(string appDataRoot, GrowthBenchmarkSuiteResult suiteResult)
    {
        var directory = EnsureAnalysisDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_growth.json");
        SaveJson(path, suiteResult);
        return path;
    }

    public static string SaveComplexityMapCsv(string appDataRoot, IReadOnlyList<ComplexityMapExportRow> rows)
    {
        var directory = EnsureMapDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_complexity_map.csv");
        WriteComplexityMapCsv(path, rows);
        return path;
    }

    public static string SaveComplexityMapJson(string appDataRoot, IReadOnlyList<ComplexityMapExportRow> rows)
    {
        var directory = EnsureMapDirectory(appDataRoot);
        var path = Path.Combine(directory, $"{Timestamp()}_complexity_map.json");
        SaveJson(path, rows);
        return path;
    }

    public static string CreateComplexityMapPngPath(string appDataRoot)
    {
        var directory = EnsureMapDirectory(appDataRoot);
        return Path.Combine(directory, $"{Timestamp()}_complexity_map.png");
    }

    private static void WriteComparisonCsv(string path, IReadOnlyList<ComparisonAnalysisRecord> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        var builder = new StringBuilder(2048);
        builder.AppendLine("timestamp_utc,left_algorithm_id,left_algorithm_name,right_algorithm_id,right_algorithm_name,n,distribution,seed,left_elapsed_ms,left_comparisons,left_swaps,left_writes,left_events,left_completed,right_elapsed_ms,right_comparisons,right_swaps,right_writes,right_events,right_completed");

        foreach (var row in records)
        {
            builder.Append(row.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(Escape(row.LeftAlgorithmId)).Append(',');
            builder.Append(Escape(row.LeftAlgorithmName)).Append(',');
            builder.Append(Escape(row.RightAlgorithmId)).Append(',');
            builder.Append(Escape(row.RightAlgorithmName)).Append(',');
            builder.Append(row.Size.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Distribution.ToString()).Append(',');
            builder.Append(row.Seed.ToString(CultureInfo.InvariantCulture)).Append(',');

            builder.Append(row.Left.ElapsedMs.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Left.Comparisons.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Left.Swaps.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Left.Writes.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Left.ProcessedEvents.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Left.Completed ? "true" : "false").Append(',');

            builder.Append(row.Right.ElapsedMs.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Right.Comparisons.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Right.Swaps.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Right.Writes.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Right.ProcessedEvents.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Right.Completed ? "true" : "false");
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteGrowthCsv(string path, GrowthBenchmarkSuiteResult suiteResult)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        var builder = new StringBuilder(4096);
        builder.AppendLine("timestamp_utc,algorithm_id,algorithm_name,n,distribution,seed,elapsed_ms,comparisons,swaps,writes,processed_events,completed,sorted,multiset_preserved,error");

        foreach (var row in suiteResult.Results)
        {
            builder.Append(suiteResult.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(Escape(row.AlgorithmId)).Append(',');
            builder.Append(Escape(row.AlgorithmName)).Append(',');
            builder.Append(row.Size.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Distribution.ToString()).Append(',');
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

    private static void WriteComplexityMapCsv(string path, IReadOnlyList<ComplexityMapExportRow> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        var builder = new StringBuilder(4096);
        builder.AppendLine("algorithm_id,name,category,views,status,stable,x_metric,y_metric,size_metric,color_mode,x_value,y_value,size_value,difficulty_static,difficulty_dynamic,difficulty_effective,measured,measured_elapsed_ms,measured_comparisons,measured_swaps,measured_writes,measured_samples");

        foreach (var row in rows)
        {
            builder.Append(Escape(row.AlgorithmId)).Append(',');
            builder.Append(Escape(row.Name)).Append(',');
            builder.Append(Escape(row.Category)).Append(',');
            builder.Append(Escape(row.Views)).Append(',');
            builder.Append(row.Status).Append(',');
            builder.Append(row.Stable.HasValue ? (row.Stable.Value ? "true" : "false") : string.Empty).Append(',');
            builder.Append(Escape(row.XMetric)).Append(',');
            builder.Append(Escape(row.YMetric)).Append(',');
            builder.Append(Escape(row.SizeMetric)).Append(',');
            builder.Append(Escape(row.ColorMode)).Append(',');
            builder.Append(row.XValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.YValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.SizeValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.DifficultyStatic.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.DifficultyDynamic.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.DifficultyEffective.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(row.Measured ? "true" : "false").Append(',');
            builder.Append(row.MeasuredElapsedMs?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(row.MeasuredComparisons?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(row.MeasuredSwaps?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(row.MeasuredWrites?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty).Append(',');
            builder.Append(row.MeasuredSamples?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    public static void SaveJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static string EnsureAnalysisDirectory(string appDataRoot)
    {
        var directory = Path.Combine(appDataRoot, "analysis");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string EnsureMapDirectory(string appDataRoot)
    {
        var directory = Path.Combine(appDataRoot, "analysis", "maps");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Timestamp()
    {
        return DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
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
