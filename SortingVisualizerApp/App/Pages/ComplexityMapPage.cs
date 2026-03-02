using System.Globalization;
using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private ComplexityMapXAxisMetric _complexityXAxis = ComplexityMapXAxisMetric.AvgTimeComplexity;
    private ComplexityMapYAxisMetric _complexityYAxis = ComplexityMapYAxisMetric.ExtraMemoryComplexity;
    private ComplexityMapColorMode _complexityColorMode = ComplexityMapColorMode.Category;
    private ComplexityMapSizeMetric _complexitySizeMetric = ComplexityMapSizeMetric.Fixed;
    private ComplexityDifficultyMode _complexityDifficultyMode = ComplexityDifficultyMode.Static;
    private int _complexityDifficultySortMode;
    private int _complexityDifficultyMin = 1;
    private int _complexityDifficultyMax = 5;
    private ComplexityStableFilter _complexityStableFilter = ComplexityStableFilter.All;
    private bool _complexityIncludeStatusA = true;
    private bool _complexityIncludeStatusB = true;
    private SupportedViews _complexityViewMask = SupportedViews.All;
    private readonly HashSet<string> _complexityCategoryFilter = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _difficultyOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ComplexityMapExportRow> _lastComplexityMapRows = new();
    private string _complexitySearch = string.Empty;
    private string _complexityMapStatus = "Complexity map ready.";
    private string _lastComplexityMapExportPath = string.Empty;
    private string _lastComplexityMapPngPath = string.Empty;
    private string _complexitySelectedAlgorithmId = string.Empty;
    private bool _complexityJumpToAlgorithmPage = true;
    private int _complexityMeasureSize = 2048;
    private int _complexityMeasureSeed = 1337;
    private DistributionPreset _complexityMeasureDistribution = DistributionPreset.Random;
    private Vector2 _complexityPlotMin;
    private Vector2 _complexityPlotMax;
    private bool _complexityHasPlotRect;

    private void DrawComplexityMapSection()
    {
        if (_complexityMeasureSize <= 0)
        {
            _complexityMeasureSize = _arraySize;
        }

        var xAxisNames = new[] { "Avg complexity", "Elapsed (ms)", "Comparisons", "Writes" };
        var yAxisNames = new[] { "Extra memory", "Elapsed (ms)", "Swaps", "Comparisons" };
        var colorNames = new[] { "Category", "Supported view", "A/B status" };
        var sizeNames = new[] { "Fixed", "Elapsed (ms)", "Swaps+Writes" };
        var difficultyModeNames = new[] { "Static", "Dynamic" };
        var stableFilterNames = new[] { "All", "Stable only", "Unstable only" };
        var difficultySortNames = new[] { "None", "Difficulty asc", "Difficulty desc" };

        var xIndex = (int)_complexityXAxis;
        var yIndex = (int)_complexityYAxis;
        var colorIndex = (int)_complexityColorMode;
        var sizeIndex = (int)_complexitySizeMetric;
        var diffModeIndex = (int)_complexityDifficultyMode;
        var stableIndex = (int)_complexityStableFilter;

        PanelTheme.LabeledRow("X axis", () =>
        {
            if (ImGui.Combo("##cmp-x-axis", ref xIndex, xAxisNames, xAxisNames.Length))
            {
                _complexityXAxis = (ComplexityMapXAxisMetric)Math.Clamp(xIndex, 0, xAxisNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Y axis", () =>
        {
            if (ImGui.Combo("##cmp-y-axis", ref yIndex, yAxisNames, yAxisNames.Length))
            {
                _complexityYAxis = (ComplexityMapYAxisMetric)Math.Clamp(yIndex, 0, yAxisNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Color", () =>
        {
            if (ImGui.Combo("##cmp-color", ref colorIndex, colorNames, colorNames.Length))
            {
                _complexityColorMode = (ComplexityMapColorMode)Math.Clamp(colorIndex, 0, colorNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Size", () =>
        {
            if (ImGui.Combo("##cmp-size", ref sizeIndex, sizeNames, sizeNames.Length))
            {
                _complexitySizeMetric = (ComplexityMapSizeMetric)Math.Clamp(sizeIndex, 0, sizeNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Difficulty mode", () =>
        {
            if (ImGui.Combo("##cmp-diff-mode", ref diffModeIndex, difficultyModeNames, difficultyModeNames.Length))
            {
                _complexityDifficultyMode = (ComplexityDifficultyMode)Math.Clamp(diffModeIndex, 0, difficultyModeNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Difficulty sort", () =>
        {
            ImGui.Combo("##cmp-diff-sort", ref _complexityDifficultySortMode, difficultySortNames, difficultySortNames.Length);
        });

        PanelTheme.SectionHeader("Measured Condition");

        var measureSize = _complexityMeasureSize;
        PanelTheme.LabeledRow("N", () =>
        {
            if (ImGui.SliderInt("##cmp-measure-n", ref measureSize, 8, 5_000_000))
            {
                _complexityMeasureSize = measureSize;
            }
        });

        var measureSeed = _complexityMeasureSeed;
        PanelTheme.LabeledRow("Seed", () =>
        {
            if (ImGui.InputInt("##cmp-measure-seed", ref measureSeed))
            {
                _complexityMeasureSeed = measureSeed;
            }
        });

        PanelTheme.LabeledRow("Distribution", () =>
        {
            var names = Enum.GetNames<DistributionPreset>();
            var idx = (int)_complexityMeasureDistribution;
            if (ImGui.Combo("##cmp-measure-dist", ref idx, names, names.Length))
            {
                _complexityMeasureDistribution = (DistributionPreset)Math.Clamp(idx, 0, names.Length - 1);
            }
        });

        PanelTheme.SectionHeader("Filter");

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##cmp-map-search", "Search algorithm", ref _complexitySearch, 128);

        PanelTheme.LabeledRow("Categories", () => DrawCategoryFilterCombo());
        PanelTheme.LabeledRow("Views", () => DrawViewFilterCombo());

        var includeA = _complexityIncludeStatusA;
        if (ImGui.Checkbox("Include A", ref includeA))
        {
            _complexityIncludeStatusA = includeA;
        }

        ImGui.SameLine();
        var includeB = _complexityIncludeStatusB;
        if (ImGui.Checkbox("Include B", ref includeB))
        {
            _complexityIncludeStatusB = includeB;
        }

        PanelTheme.LabeledRow("Stable", () =>
        {
            if (ImGui.Combo("##cmp-stable-filter", ref stableIndex, stableFilterNames, stableFilterNames.Length))
            {
                _complexityStableFilter = (ComplexityStableFilter)Math.Clamp(stableIndex, 0, stableFilterNames.Length - 1);
            }
        });

        PanelTheme.LabeledRow("Difficulty min", () =>
        {
            ImGui.SliderInt("##cmp-diff-min", ref _complexityDifficultyMin, 1, 5);
        });
        PanelTheme.LabeledRow("Difficulty max", () =>
        {
            ImGui.SliderInt("##cmp-diff-max", ref _complexityDifficultyMax, 1, 5);
        });
        if (_complexityDifficultyMin > _complexityDifficultyMax)
        {
            (_complexityDifficultyMin, _complexityDifficultyMax) = (_complexityDifficultyMax, _complexityDifficultyMin);
        }

        ImGui.Checkbox("Click -> open Algorithm page", ref _complexityJumpToAlgorithmPage);

        var measuredLookup = BuildConditionedMeasuredLookup(_complexityMeasureSize, _complexityMeasureDistribution, _complexityMeasureSeed);
        var filter = new ComplexityMapFilter
        {
            Search = _complexitySearch,
            Categories = _complexityCategoryFilter.Count == 0 ? null : _complexityCategoryFilter,
            ViewMask = _complexityViewMask,
            IncludeStatusA = _complexityIncludeStatusA,
            IncludeStatusB = _complexityIncludeStatusB,
            StableFilter = _complexityStableFilter,
            DifficultyMin = _complexityDifficultyMin,
            DifficultyMax = _complexityDifficultyMax
        };

        var build = ComplexityMapService.Build(new ComplexityMapBuildRequest
        {
            Algorithms = _registry.All,
            MeasuredLookup = measuredLookup,
            Filter = filter,
            DifficultyOverrides = _difficultyOverrides,
            XAxis = _complexityXAxis,
            YAxis = _complexityYAxis,
            SizeMetric = _complexitySizeMetric,
            DifficultyMode = _complexityDifficultyMode
        });

        var orderedPoints = ApplyDifficultyOrdering(build.Points);
        var visuals = orderedPoints.Select(ToVisualPoint).ToArray();
        var plotResult = ComplexityMapPlot.Draw(
            "complexity-map-plot",
            visuals,
            new Vector2(0, 280),
            AxisLabel(_complexityXAxis),
            AxisLabel(_complexityYAxis));

        _complexityHasPlotRect = plotResult.HasValidPlot;
        _complexityPlotMin = plotResult.PlotMin;
        _complexityPlotMax = plotResult.PlotMax;

        if (!string.IsNullOrWhiteSpace(plotResult.ClickedAlgorithmId))
        {
            _complexitySelectedAlgorithmId = plotResult.ClickedAlgorithmId;
            _selectedAlgorithmId = plotResult.ClickedAlgorithmId;

            if (_registry.TryGet(_selectedAlgorithmId, out var selected))
            {
                EnsureModeCompatibility(selected, setStatus: true);
            }

            if (_complexityJumpToAlgorithmPage)
            {
                _currentPanelPage = UiPanelPage.Algorithm;
            }
        }

        _lastComplexityMapRows.Clear();
        foreach (var point in orderedPoints)
        {
            _lastComplexityMapRows.Add(new ComplexityMapExportRow
            {
                AlgorithmId = point.Metadata.Id,
                Name = point.Metadata.Name,
                Category = point.Metadata.Category,
                Views = point.Metadata.SupportedViews.ToDisplayString(),
                Status = point.Metadata.Status,
                Stable = point.Metadata.Stable,
                XMetric = AxisLabel(_complexityXAxis),
                YMetric = AxisLabel(_complexityYAxis),
                SizeMetric = SizeLabel(_complexitySizeMetric),
                ColorMode = ColorModeLabel(_complexityColorMode),
                XValue = point.XRaw,
                YValue = point.YRaw,
                SizeValue = point.SizeRaw,
                DifficultyStatic = point.StaticDifficulty,
                DifficultyDynamic = point.DynamicDifficulty,
                DifficultyEffective = point.EffectiveDifficulty,
                Measured = point.IsMeasured,
                MeasuredElapsedMs = point.Measured?.ElapsedMs,
                MeasuredComparisons = point.Measured?.Comparisons,
                MeasuredSwaps = point.Measured?.Swaps,
                MeasuredWrites = point.Measured?.Writes,
                MeasuredSamples = point.Measured?.SampleCount
            });
        }

        ImGui.TextUnformatted($"Points: {build.FilteredCount:N0}/{build.SourceCount:N0}  Measured: {build.MeasuredCount:N0}");
        ImGui.TextWrapped(_complexityMapStatus);
        if (!string.IsNullOrWhiteSpace(_lastComplexityMapExportPath))
        {
            ImGui.TextWrapped($"Last map export: {_lastComplexityMapExportPath}");
        }

        if (!string.IsNullOrWhiteSpace(_lastComplexityMapPngPath))
        {
            ImGui.TextWrapped($"Last map PNG: {_lastComplexityMapPngPath}");
        }

        if (PanelTheme.SecondaryButton("Export Map CSV", 150))
        {
            ExportComplexityMapCsv();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Map JSON", 150))
        {
            ExportComplexityMapJson();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Map PNG", 150))
        {
            ExportComplexityMapPng();
        }

        DrawSelectedDifficultyOverrideEditor();
        DrawComplexityListTable(orderedPoints);
    }

    private void DrawComplexityListTable(IReadOnlyList<ComplexityMapPoint> points)
    {
        if (!ImGui.BeginTable(
                "complexity-map-list",
                4,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                new Vector2(0, 180)))
        {
            return;
        }

        ImGui.TableSetupColumn("Algorithm");
        ImGui.TableSetupColumn("Diff");
        ImGui.TableSetupColumn("Measured");
        ImGui.TableSetupColumn("Views");
        ImGui.TableHeadersRow();

        foreach (var point in points.Take(96))
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (ImGui.Selectable($"{point.Metadata.Name}##cmp-list-{point.Metadata.Id}", false, ImGuiSelectableFlags.SpanAllColumns))
            {
                _selectedAlgorithmId = point.Metadata.Id;
                _complexitySelectedAlgorithmId = point.Metadata.Id;
                if (_registry.TryGet(_selectedAlgorithmId, out var selected))
                {
                    EnsureModeCompatibility(selected, setStatus: true);
                }
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted($"{point.EffectiveDifficulty} ({point.StaticDifficulty}/{point.DynamicDifficulty})");
            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(point.IsMeasured ? "Yes" : "No");
            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted(point.Metadata.SupportedViews.ToDisplayString());
        }

        ImGui.EndTable();
    }

    private IReadOnlyList<ComplexityMapPoint> ApplyDifficultyOrdering(IReadOnlyList<ComplexityMapPoint> points)
    {
        return _complexityDifficultySortMode switch
        {
            1 => points.OrderBy(static point => point.EffectiveDifficulty).ThenBy(static point => point.Metadata.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            2 => points.OrderByDescending(static point => point.EffectiveDifficulty).ThenBy(static point => point.Metadata.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            _ => points.OrderBy(static point => point.Metadata.Name, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private ComplexityMapVisualPoint ToVisualPoint(ComplexityMapPoint point)
    {
        var color = ResolveColor(point.Metadata);
        var shape = point.Metadata.Stable switch
        {
            true => ComplexityPointShape.Circle,
            false => ComplexityPointShape.Square,
            _ => ComplexityPointShape.Diamond
        };

        var size = 4.0f + (float)Math.Clamp(point.SizeNormalized, 0.0, 1.0) * 7.0f;
        var tooltip = BuildTooltip(point);

        return new ComplexityMapVisualPoint
        {
            AlgorithmId = point.Metadata.Id,
            Label = point.Metadata.Name,
            Tooltip = tooltip,
            X = (float)Math.Clamp(point.XNormalized, 0.0, 1.0),
            Y = (float)Math.Clamp(point.YNormalized, 0.0, 1.0),
            Size = size,
            Color = color,
            IsMeasured = point.IsMeasured,
            Shape = shape
        };
    }

    private string BuildTooltip(ComplexityMapPoint point)
    {
        var measuredLabel = point.IsMeasured ? "measured" : "not measured";
        var measuredStats = point.Measured is null
            ? "No benchmark data for selected condition."
            : $"elapsed={point.Measured.ElapsedMs:0.00}ms, cmp={point.Measured.Comparisons:0}, swp={point.Measured.Swaps:0}, wr={point.Measured.Writes:0}, samples={point.Measured.SampleCount}";

        return $"{point.Metadata.Name} [{point.Metadata.Status}] ({measuredLabel})\n"
            + $"Category: {point.Metadata.Category}\n"
            + $"X={point.XRaw:0.###} ({(point.XUsesMeasured ? "measured" : "meta")}), Y={point.YRaw:0.###} ({(point.YUsesMeasured ? "measured" : "meta")})\n"
            + $"Difficulty static/dynamic/effective: {point.StaticDifficulty}/{point.DynamicDifficulty}/{point.EffectiveDifficulty}\n"
            + $"{measuredStats}";
    }

    private void DrawSelectedDifficultyOverrideEditor()
    {
        var targetId = !string.IsNullOrWhiteSpace(_complexitySelectedAlgorithmId) ? _complexitySelectedAlgorithmId : _selectedAlgorithmId;
        if (string.IsNullOrWhiteSpace(targetId) || !_registry.TryGet(targetId, out var meta))
        {
            return;
        }

        PanelTheme.SectionHeader("Difficulty Override");
        ImGui.TextUnformatted(meta.Name);
        var staticAuto = ComplexityMapService.ResolveStaticDifficulty(meta, null);
        var value = ComplexityMapService.ResolveStaticDifficulty(meta, _difficultyOverrides);

        PanelTheme.LabeledRow("Auto", () => ImGui.TextUnformatted(staticAuto.ToString(CultureInfo.InvariantCulture)));
        PanelTheme.LabeledRow("Override", () =>
        {
            if (ImGui.SliderInt("##cmp-difficulty-override", ref value, 1, 5))
            {
                _difficultyOverrides[targetId] = value;
                _complexityMapStatus = $"Difficulty override updated: {meta.Name} -> {value}.";
            }
        });

        if (PanelTheme.SecondaryButton("Clear Override", 140))
        {
            if (_difficultyOverrides.Remove(targetId))
            {
                _complexityMapStatus = $"Difficulty override cleared: {meta.Name}.";
            }
        }
    }

    private Dictionary<string, ComplexityMeasuredStats> BuildConditionedMeasuredLookup(int size, DistributionPreset distribution, int seed)
    {
        var aggregate = new Dictionary<string, (double Elapsed, double Cmp, double Swp, double Wr, int Count)>(StringComparer.OrdinalIgnoreCase);

        if (_lastBenchmarkSuite is not null
            && _lastBenchmarkSuite.Request.Size == size
            && _lastBenchmarkSuite.Request.Distribution == distribution
            && _lastBenchmarkSuite.Request.Seed == seed)
        {
            foreach (var row in _lastBenchmarkSuite.Results.Where(static row => row.Completed && string.IsNullOrWhiteSpace(row.Error)))
            {
                AddAggregate(row.AlgorithmId, row.ElapsedMs, row.Comparisons, row.Swaps, row.Writes);
            }
        }

        if (_lastGrowthSuite is not null)
        {
            foreach (var row in _lastGrowthSuite.Results.Where(row =>
                         row.Completed
                         && string.IsNullOrWhiteSpace(row.Error)
                         && row.Size == size
                         && row.Distribution == distribution
                         && row.Seed == seed))
            {
                AddAggregate(row.AlgorithmId, row.ElapsedMs, row.Comparisons, row.Swaps, row.Writes);
            }
        }

        foreach (var row in _comparisonHistory.Where(row =>
                     row.Size == size
                     && row.Distribution == distribution
                     && row.Seed == seed))
        {
            if (row.Left.Completed)
            {
                AddAggregate(row.LeftAlgorithmId, row.Left.ElapsedMs, row.Left.Comparisons, row.Left.Swaps, row.Left.Writes);
            }

            if (row.Right.Completed)
            {
                AddAggregate(row.RightAlgorithmId, row.Right.ElapsedMs, row.Right.Comparisons, row.Right.Swaps, row.Right.Writes);
            }
        }

        var measured = new Dictionary<string, ComplexityMeasuredStats>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in aggregate)
        {
            var avg = pair.Value;
            measured[pair.Key] = new ComplexityMeasuredStats
            {
                AlgorithmId = pair.Key,
                SampleCount = avg.Count,
                ElapsedMs = avg.Elapsed / avg.Count,
                Comparisons = avg.Cmp / avg.Count,
                Swaps = avg.Swp / avg.Count,
                Writes = avg.Wr / avg.Count
            };
        }

        return measured;

        void AddAggregate(string algorithmId, double elapsed, double comparisons, double swaps, double writes)
        {
            aggregate.TryGetValue(algorithmId, out var current);
            current.Elapsed += elapsed;
            current.Cmp += comparisons;
            current.Swp += swaps;
            current.Wr += writes;
            current.Count += 1;
            aggregate[algorithmId] = current;
        }
    }

    private void DrawCategoryFilterCombo()
    {
        var categories = _registry.All
            .Select(static meta => meta.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var preview = _complexityCategoryFilter.Count == 0
            ? "All"
            : _complexityCategoryFilter.Count == categories.Length
                ? "All"
                : $"{_complexityCategoryFilter.Count} selected";

        if (!ImGui.BeginCombo("##cmp-category-filter", preview))
        {
            return;
        }

        if (ImGui.Selectable("All", _complexityCategoryFilter.Count == 0))
        {
            _complexityCategoryFilter.Clear();
        }

        foreach (var category in categories)
        {
            var selected = _complexityCategoryFilter.Contains(category);
            if (ImGui.Selectable(category, selected))
            {
                if (!selected)
                {
                    _complexityCategoryFilter.Add(category);
                }
                else
                {
                    _complexityCategoryFilter.Remove(category);
                }
            }
        }

        ImGui.EndCombo();
    }

    private void DrawViewFilterCombo()
    {
        var preview = _complexityViewMask == SupportedViews.All ? "All" : _complexityViewMask.ToDisplayString();
        if (!ImGui.BeginCombo("##cmp-view-filter", preview))
        {
            return;
        }

        DrawViewMaskToggle("Bars", SupportedViews.Bars);
        DrawViewMaskToggle("Network", SupportedViews.Network);
        DrawViewMaskToggle("External", SupportedViews.External);
        DrawViewMaskToggle("Graph", SupportedViews.Graph);
        DrawViewMaskToggle("String", SupportedViews.String);
        DrawViewMaskToggle("Spatial", SupportedViews.Spatial);

        if (ImGui.Selectable("Reset: All"))
        {
            _complexityViewMask = SupportedViews.All;
        }

        if (_complexityViewMask == SupportedViews.None)
        {
            _complexityViewMask = SupportedViews.All;
        }

        ImGui.EndCombo();
    }

    private void DrawViewMaskToggle(string label, SupportedViews flag)
    {
        var selected = (_complexityViewMask & flag) != 0;
        if (ImGui.Selectable(label, selected))
        {
            if (selected)
            {
                _complexityViewMask &= ~flag;
            }
            else
            {
                _complexityViewMask |= flag;
            }
        }
    }

    private uint ResolveColor(AlgorithmMetadata meta)
    {
        return _complexityColorMode switch
        {
            ComplexityMapColorMode.Status => meta.Status == AlgorithmImplementationStatus.A
                ? PackColor(240, 240, 240, 255)
                : PackColor(110, 150, 200, 255),
            ComplexityMapColorMode.SupportedView => ResolveViewColor(meta.SupportedViews),
            _ => ResolveCategoryColor(meta.Category)
        };
    }

    private static uint ResolveViewColor(SupportedViews views)
    {
        if ((views & SupportedViews.String) != 0)
        {
            return PackColor(244, 178, 66, 255);
        }

        if ((views & SupportedViews.Spatial) != 0)
        {
            return PackColor(66, 194, 244, 255);
        }

        if ((views & SupportedViews.Network) != 0)
        {
            return PackColor(188, 129, 255, 255);
        }

        if ((views & SupportedViews.External) != 0)
        {
            return PackColor(103, 222, 150, 255);
        }

        if ((views & SupportedViews.Graph) != 0)
        {
            return PackColor(255, 133, 133, 255);
        }

        return PackColor(240, 240, 240, 255);
    }

    private static uint ResolveCategoryColor(string category)
    {
        var hash = DeterministicHash(category);
        var r = (byte)(90 + (hash & 0x5F));
        var g = (byte)(90 + ((hash >> 6) & 0x5F));
        var b = (byte)(90 + ((hash >> 12) & 0x5F));
        return PackColor(r, g, b, 255);
    }

    private static int DeterministicHash(string text)
    {
        unchecked
        {
            var hash = 2166136261;
            for (var i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619;
            }

            return (int)hash;
        }
    }

    private void ExportComplexityMapCsv()
    {
        if (_lastComplexityMapRows.Count == 0)
        {
            _complexityMapStatus = "No map points to export.";
            return;
        }

        try
        {
            _lastComplexityMapExportPath = AnalysisExportService.SaveComplexityMapCsv(_analysisRootDir, _lastComplexityMapRows);
            _complexityMapStatus = $"Complexity map CSV exported: {_lastComplexityMapExportPath}";
        }
        catch (Exception ex)
        {
            _complexityMapStatus = $"Complexity map CSV export failed: {ex.Message}";
        }
    }

    private void ExportComplexityMapJson()
    {
        if (_lastComplexityMapRows.Count == 0)
        {
            _complexityMapStatus = "No map points to export.";
            return;
        }

        try
        {
            _lastComplexityMapExportPath = AnalysisExportService.SaveComplexityMapJson(_analysisRootDir, _lastComplexityMapRows);
            _complexityMapStatus = $"Complexity map JSON exported: {_lastComplexityMapExportPath}";
        }
        catch (Exception ex)
        {
            _complexityMapStatus = $"Complexity map JSON export failed: {ex.Message}";
        }
    }

    private void ExportComplexityMapPng()
    {
        if (!_complexityHasPlotRect)
        {
            _complexityMapStatus = "Map PNG export failed: plot region is unavailable.";
            return;
        }

        try
        {
            var x = Math.Clamp((int)MathF.Floor(_complexityPlotMin.X), 0, Math.Max(0, ClientSize.X - 1));
            var yTop = Math.Clamp((int)MathF.Floor(_complexityPlotMin.Y), 0, Math.Max(0, ClientSize.Y - 1));
            var width = Math.Clamp((int)MathF.Ceiling(_complexityPlotMax.X - _complexityPlotMin.X), 1, ClientSize.X - x);
            var height = Math.Clamp((int)MathF.Ceiling(_complexityPlotMax.Y - _complexityPlotMin.Y), 1, ClientSize.Y - yTop);

            var glY = ClientSize.Y - (yTop + height);
            glY = Math.Clamp(glY, 0, Math.Max(0, ClientSize.Y - height));

            var pixels = new byte[width * height * 4];
            GL.ReadPixels(x, glY, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            var path = AnalysisExportService.CreateComplexityMapPngPath(_analysisRootDir);
            SnapshotExporter.SavePng(path, width, height, pixels);

            _lastComplexityMapPngPath = path;
            _complexityMapStatus = $"Complexity map PNG exported: {path}";
        }
        catch (Exception ex)
        {
            _complexityMapStatus = $"Complexity map PNG export failed: {ex.Message}";
        }
    }

    private static string AxisLabel(ComplexityMapXAxisMetric metric)
    {
        return metric switch
        {
            ComplexityMapXAxisMetric.AvgTimeComplexity => "Avg time complexity (meta)",
            ComplexityMapXAxisMetric.MeasuredElapsedMs => "Measured elapsed (ms)",
            ComplexityMapXAxisMetric.Comparisons => "Comparisons",
            ComplexityMapXAxisMetric.Writes => "Writes",
            _ => metric.ToString()
        };
    }

    private static string AxisLabel(ComplexityMapYAxisMetric metric)
    {
        return metric switch
        {
            ComplexityMapYAxisMetric.ExtraMemoryComplexity => "Extra memory complexity (meta)",
            ComplexityMapYAxisMetric.MeasuredElapsedMs => "Measured elapsed (ms)",
            ComplexityMapYAxisMetric.Swaps => "Swaps",
            ComplexityMapYAxisMetric.Comparisons => "Comparisons",
            _ => metric.ToString()
        };
    }

    private static string SizeLabel(ComplexityMapSizeMetric metric)
    {
        return metric switch
        {
            ComplexityMapSizeMetric.Fixed => "Fixed",
            ComplexityMapSizeMetric.ElapsedMs => "Elapsed (ms)",
            ComplexityMapSizeMetric.SwapsPlusWrites => "Swaps+Writes",
            _ => metric.ToString()
        };
    }

    private static string ColorModeLabel(ComplexityMapColorMode mode)
    {
        return mode switch
        {
            ComplexityMapColorMode.Category => "Category",
            ComplexityMapColorMode.SupportedView => "SupportedView",
            ComplexityMapColorMode.Status => "Status",
            _ => mode.ToString()
        };
    }
}
