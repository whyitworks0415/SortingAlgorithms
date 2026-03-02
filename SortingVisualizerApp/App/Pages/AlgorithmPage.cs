using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawAlgorithmPage()
    {
        PanelTheme.SectionHeader("Search / Filter");

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##alg-search-paged", "Search algorithm", ref _algorithmSearch, 128);

        var categories = _registry.All
            .Select(static meta => meta.Category)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        categories.Insert(0, "All");

        var categoryIndex = Math.Max(0, categories.IndexOf(_algorithmCategoryFilter));
        PanelTheme.LabeledRow("Category", () =>
        {
            if (ImGui.Combo("##alg-cat", ref categoryIndex, categories.ToArray(), categories.Count))
            {
                _algorithmCategoryFilter = categories[categoryIndex];
            }
        });

        var statusOptions = new[] { "All", "A only", "B only" };
        PanelTheme.LabeledRow("Status", () =>
        {
            ImGui.Combo("##alg-status", ref _algorithmStatusFilterIndex, statusOptions, statusOptions.Length);
        });

        var viewOptions = new[] { "All", "Bars", "Network", "External", "Graph", "String", "Spatial" };
        PanelTheme.LabeledRow("Views", () =>
        {
            ImGui.Combo("##alg-view", ref _algorithmViewFilterIndex, viewOptions, viewOptions.Length);
        });

        ImGui.Checkbox("Favorites only", ref _showFavoritesOnly);

        PanelTheme.SectionHeader("Algorithms");
        ImGui.BeginChild("alg-paged-list", new Vector2(0, 260), ImGuiChildFlags.Borders);

        string? lastCategory = null;
        foreach (var meta in GetPagedAlgorithmCandidates())
        {
            if (!string.Equals(lastCategory, meta.Category, StringComparison.Ordinal))
            {
                lastCategory = meta.Category;
                ImGui.SeparatorText(meta.Category);
            }

            ImGui.PushID(meta.Id);
            var isFavorite = _favorites.Contains(meta.Id);
            if (ImGui.SmallButton(isFavorite ? "*" : "+"))
            {
                if (isFavorite)
                {
                    _favorites.Remove(meta.Id);
                }
                else
                {
                    _favorites.Add(meta.Id);
                }
            }

            ImGui.SameLine();
            var selected = _selectedAlgorithmId == meta.Id;
            if (!meta.IsImplemented)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.58f, 0.58f, 0.58f, 1f));
            }

            if (ImGui.Selectable($"{meta.Name} [{meta.Status}]", selected))
            {
                _selectedAlgorithmId = meta.Id;
                EnsureModeCompatibility(meta, setStatus: true);
            }

            if (!meta.IsImplemented)
            {
                ImGui.PopStyleColor();
            }

            ImGui.PopID();
        }

        ImGui.EndChild();

        PanelTheme.SectionHeader("Metadata");
        if (_registry.TryGet(_selectedAlgorithmId, out var info))
        {
            ImGui.TextUnformatted($"Name: {info.Name}");
            ImGui.TextUnformatted($"Stable: {(info.Stable.HasValue ? (info.Stable.Value ? "Yes" : "No") : "-")}");
            ImGui.TextUnformatted($"Complexity: {info.AverageComplexity} / {info.WorstComplexity}");
            ImGui.TextUnformatted($"Difficulty: {ComplexityMapService.ResolveStaticDifficulty(info, _difficultyOverrides)}/5");
            ImGui.TextUnformatted($"Views: {info.SupportedViews.ToDisplayString()}");
            ImGui.TextWrapped(info.Description);
            ImGui.TextDisabled("Algorithm-specific options: currently none.");
        }
    }

    private IEnumerable<AlgorithmMetadata> GetPagedAlgorithmCandidates()
    {
        var query = _algorithmSearch?.Trim() ?? string.Empty;
        var hasQuery = query.Length > 0;
        var viewFilter = ResolveViewFilter();

        return _registry.All.Where(meta =>
        {
            if (_showFavoritesOnly && !_favorites.Contains(meta.Id))
            {
                return false;
            }

            if (!string.Equals(_algorithmCategoryFilter, "All", StringComparison.Ordinal)
                && !string.Equals(meta.Category, _algorithmCategoryFilter, StringComparison.Ordinal))
            {
                return false;
            }

            if (_algorithmStatusFilterIndex == 1 && meta.Status != AlgorithmImplementationStatus.A)
            {
                return false;
            }

            if (_algorithmStatusFilterIndex == 2 && meta.Status != AlgorithmImplementationStatus.B)
            {
                return false;
            }

            if (viewFilter.HasValue && (meta.SupportedViews & viewFilter.Value) == 0)
            {
                return false;
            }

            if (!hasQuery)
            {
                return true;
            }

            return meta.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || meta.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                || meta.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
        });
    }

    private SupportedViews? ResolveViewFilter()
    {
        return _algorithmViewFilterIndex switch
        {
            1 => SupportedViews.Bars,
            2 => SupportedViews.Network,
            3 => SupportedViews.External,
            4 => SupportedViews.Graph,
            5 => SupportedViews.String,
            6 => SupportedViews.Spatial,
            _ => null
        };
    }
}
