using System.Numerics;
using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawPanelPageNavigation()
    {
        PanelTheme.SectionHeader("Pages (Alt+1..7)");

        var contentWidth = ImGui.GetContentRegionAvail().X;
        var firstRowCount = 4;
        var secondRowCount = 3;
        var spacing = PanelTheme.Grid;
        var firstWidth = (contentWidth - spacing * (firstRowCount - 1)) / firstRowCount;
        var secondWidth = (contentWidth - spacing * (secondRowCount - 1)) / secondRowCount;

        DrawPageButtonRow(0, firstRowCount, firstWidth);
        DrawPageButtonRow(firstRowCount, secondRowCount, secondWidth);
    }

    private void DrawPageButtonRow(int startIndex, int count, float width)
    {
        for (var i = 0; i < count; i++)
        {
            var pageIndex = startIndex + i;
            if (pageIndex >= _panelPageLabels.Length)
            {
                break;
            }

            var selected = (int)_currentPanelPage == pageIndex;
            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.22f, 0.66f, 0.96f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.24f, 0.70f, 1.0f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.18f, 0.58f, 0.90f, 1.0f));
            }

            if (ImGui.Button(_panelPageLabels[pageIndex], new Vector2(width, PanelTheme.RowHeight)))
            {
                _currentPanelPage = (UiPanelPage)pageIndex;
            }

            if (selected)
            {
                ImGui.PopStyleColor(3);
            }

            if (i < count - 1)
            {
                ImGui.SameLine();
            }
        }
    }

    private void DrawRunPage()
    {
        var stats = GetCurrentStats();

        PanelTheme.SectionHeader("Run Controls");
        if (PanelTheme.PrimaryButton("Start", 120))
        {
            StartSelectedAlgorithm();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton(stats.IsPaused ? "Resume" : "Pause", 110))
        {
            ToggleCurrentPause();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Step", 80))
        {
            StepCurrentOnce();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Reset", 80))
        {
            ResetCurrentToSource();
            ResetViewTrackers();
        }

        if (PanelTheme.SecondaryButton("Stop", 120))
        {
            StopCurrentEngine();
            TryCaptureReplayAfterCompletion(force: true);
            _statusText = "Stopped.";
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("New Data", 120))
        {
            RegenerateData();
        }

        ImGui.SameLine();
        if (PanelTheme.SecondaryButton("Shuffle", 120))
        {
            RegenerateData();
        }

        PanelTheme.SectionHeader("Speed / Detail");

        PanelTheme.LabeledRow("Speed Mode", () =>
        {
            var speedMode = (int)_controls.SpeedMode;
            if (ImGui.RadioButton("Events/sec", speedMode == (int)SpeedControlMode.EventsPerSecond))
            {
                _controls.SpeedMode = SpeedControlMode.EventsPerSecond;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Delay(ms)", speedMode == (int)SpeedControlMode.DelayMs))
            {
                _controls.SpeedMode = SpeedControlMode.DelayMs;
            }
        });

        PanelTheme.LabeledRow("Events/sec", () =>
        {
            var eps = (float)_controls.EventsPerSecond;
            if (PanelTheme.SliderFloatWithInput("run-eps", ref eps, 1f, 300000f, "%.0f", 1f, 100f))
            {
                _controls.EventsPerSecond = eps;
            }
        });

        PanelTheme.LabeledRow("Delay (ms)", () =>
        {
            var delay = (float)_controls.DelayMs;
            if (PanelTheme.SliderFloatWithInput("run-delay", ref delay, 0f, 100f, "%.3f", 0.001f, 0.1f))
            {
                _controls.DelayMs = delay;
            }
        });

        PanelTheme.LabeledRow("Parallelism", () =>
        {
            var parallelism = _controls.Parallelism;
            var maxParallelism = Math.Clamp(Environment.ProcessorCount * 2, 2, 128);
            if (PanelTheme.SliderIntWithInput("run-parallelism", ref parallelism, 1, maxParallelism, "%d", 1, 8))
            {
                _controls.Parallelism = parallelism;
            }
        });

        PanelTheme.LabeledRow("Detail", () =>
        {
            var visualDetail = (int)_controls.VisualDetail;
            if (PanelTheme.SliderIntWithInput("run-detail", ref visualDetail, 1, 3, "%d", 1, 1))
            {
                _controls.VisualDetail = (DetailLevel)visualDetail;
                if (_controls.LinkDetails)
                {
                    _controls.AudioDetail = _controls.VisualDetail;
                }
            }
        });

        var linkDetails = _controls.LinkDetails;
        if (ImGui.Checkbox("Link audio detail", ref linkDetails))
        {
            _controls.LinkDetails = linkDetails;
            if (_controls.LinkDetails)
            {
                _controls.AudioDetail = _controls.VisualDetail;
            }
        }

        PanelTheme.SectionHeader("Mode");
        ImGui.TextUnformatted($"Current ViewMode: {_visualizationMode}");
        ImGui.TextDisabled("View hotkeys: 1 Bars, 2 Network, 3 External, 4 Graph, 5 String, 6 Spatial");
        ImGui.TextDisabled("Panel page hotkeys: Alt+1..Alt+7");
    }
}
