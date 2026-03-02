using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawDataPage()
    {
        PanelTheme.SectionHeader("Dataset");

        if (_visualizationMode == VisualizationMode.String)
        {
            PanelTheme.LabeledRow("String Count", () =>
            {
                PanelTheme.SliderIntWithInput("str-count", ref _stringCount, 8, 5000, "%d", 1, 32);
            });

            PanelTheme.LabeledRow("Length", () =>
            {
                PanelTheme.SliderIntWithInput("str-length", ref _stringLength, 2, 64, "%d", 1, 8);
            });

            var alphabetNames = Enum.GetNames<StringAlphabetSet>();
            var alphabetIndex = (int)_stringAlphabet;
            PanelTheme.LabeledRow("Charset", () =>
            {
                if (ImGui.Combo("##str-alpha", ref alphabetIndex, alphabetNames, alphabetNames.Length))
                {
                    _stringAlphabet = (StringAlphabetSet)alphabetIndex;
                }
            });

            var presetNames = Enum.GetNames<StringDistributionPreset>();
            var presetIndex = (int)_stringDistribution;
            PanelTheme.LabeledRow("Preset", () =>
            {
                if (ImGui.Combo("##str-preset", ref presetIndex, presetNames, presetNames.Length))
                {
                    _stringDistribution = (StringDistributionPreset)presetIndex;
                }
            });
        }
        else if (_visualizationMode == VisualizationMode.Spatial)
        {
            PanelTheme.LabeledRow("Point Count", () =>
            {
                PanelTheme.SliderIntWithInput("sp-count", ref _spatialCount, 32, 200000, "%d", 16, 512);
            });

            var names = Enum.GetNames<SpatialDistributionPreset>();
            var index = (int)_spatialDistribution;
            PanelTheme.LabeledRow("Distribution", () =>
            {
                if (ImGui.Combo("##sp-dist", ref index, names, names.Length))
                {
                    _spatialDistribution = (SpatialDistributionPreset)index;
                }
            });
        }
        else
        {
            PanelTheme.LabeledRow("N", () =>
            {
                PanelTheme.SliderIntWithInput("data-n", ref _arraySize, 8, 5000000, "%d", 16, 2048);
            });

            var distNames = Enum.GetNames<DistributionPreset>();
            var distIndex = (int)_distribution;
            PanelTheme.LabeledRow("Distribution", () =>
            {
                if (ImGui.Combo("##data-dist", ref distIndex, distNames, distNames.Length))
                {
                    _distribution = (DistributionPreset)distIndex;
                }
            });

            PanelTheme.SectionHeader("Distribution Parameters");
            PanelTheme.LabeledRow("General Strength", () =>
            {
                PanelTheme.SliderFloatWithInput("dist-strength", ref _distributionStrength, 0.0f, 1.0f, "%.2f", 0.01f, 0.10f);
            });
            PanelTheme.LabeledRow("Duplicate Strength", () =>
            {
                PanelTheme.SliderFloatWithInput("dist-dup", ref _duplicateStrength, 0.0f, 1.0f, "%.2f", 0.01f, 0.10f);
            });
            PanelTheme.LabeledRow("Reverse Strength", () =>
            {
                PanelTheme.SliderFloatWithInput("dist-rev", ref _reverseStrength, 0.0f, 1.0f, "%.2f", 0.01f, 0.10f);
            });
        }

        if (_visualizationMode == VisualizationMode.Graph)
        {
            PanelTheme.SectionHeader("Graph Options");
            PanelTheme.LabeledRow("Node Count", () =>
            {
                PanelTheme.SliderIntWithInput("graph-nodes", ref _arraySize, 10, 2000, "%d", 1, 64);
            });
            PanelTheme.LabeledRow("Edge Density", () =>
            {
                PanelTheme.SliderFloatWithInput("graph-edge", ref _graphEdgeDensity, 0.01f, 0.45f, "%.2f", 0.01f, 0.05f);
            });
        }

        PanelTheme.SectionHeader("Actions");
        if (PanelTheme.PrimaryButton("Generate", 140))
        {
            RegenerateData();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Shuffle", 140))
        {
            RegenerateData();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Snapshot", 140))
        {
            _snapshotRequested = true;
        }
    }
}
