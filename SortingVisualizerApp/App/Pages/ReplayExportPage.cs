using ImGuiNET;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawReplayExportPage()
    {
        PanelTheme.SectionHeader("Record / Replay");

        ImGui.Checkbox("Record next run", ref _recordNextRun);

        if (PanelTheme.SecondaryButton("Stop Current", 140))
        {
            StopCurrentEngine();
            TryCaptureReplayAfterCompletion(force: true);
            _statusText = "Stopped.";
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Capture Replay", 160))
        {
            TryCaptureReplayAfterCompletion(force: true);
        }

        DrawReplaySection();

        PanelTheme.SectionHeader("Export");
        if (PanelTheme.SecondaryButton("Export Benchmark CSV", 180))
        {
            ExportBenchmarkCsv();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Growth CSV", 160))
        {
            ExportGrowthCsv();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Growth JSON", 165))
        {
            ExportGrowthJson();
        }

        if (PanelTheme.SecondaryButton("Export Comparison CSV", 190))
        {
            ExportComparisonCsv();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Export Comparison JSON", 200))
        {
            ExportComparisonJson();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Save PNG Snapshot", 180))
        {
            _snapshotRequested = true;
        }

        PanelTheme.SectionHeader("Settings");
        if (PanelTheme.SecondaryButton("Save Settings", 140))
        {
            SaveSettings(_settingsPath, "settings");
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Load Settings", 140))
        {
            LoadSettings(_settingsPath, "settings");
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Save Preset", 140))
        {
            SaveSettings(_presetPath, "preset");
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Load Preset", 140))
        {
            LoadSettings(_presetPath, "preset");
        }

        ImGui.TextWrapped(_statusText);
    }
}
