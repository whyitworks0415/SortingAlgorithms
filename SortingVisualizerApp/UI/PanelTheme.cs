using System.Numerics;
using ImGuiNET;

namespace SortingVisualizerApp.UI;

public static class PanelTheme
{
    public const float Grid = 8.0f;
    public const float RowHeight = 36.0f;
    public const float Radius = 6.0f;

    public static void ApplyMinimalTheme()
    {
        var style = ImGui.GetStyle();
        style.WindowRounding = Radius;
        style.ChildRounding = Radius;
        style.FrameRounding = Radius;
        style.GrabRounding = Radius;
        style.ScrollbarRounding = Radius;
        style.FramePadding = new Vector2(10.0f, 9.0f);
        style.ItemSpacing = new Vector2(Grid, Grid);
        style.ItemInnerSpacing = new Vector2(Grid, Grid * 0.75f);

        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.95f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.11f, 0.11f, 0.11f, 1.0f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.16f, 0.16f, 0.16f, 1.0f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.22f, 0.22f, 0.22f, 1.0f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.12f, 0.12f, 0.12f, 1.0f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.17f, 0.17f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.24f, 0.24f, 0.24f, 1.0f);
        colors[(int)ImGuiCol.Separator] = new Vector4(0.25f, 0.25f, 0.25f, 1.0f);
        colors[(int)ImGuiCol.Text] = new Vector4(0.93f, 0.93f, 0.93f, 1.0f);
        colors[(int)ImGuiCol.Header] = new Vector4(0.13f, 0.13f, 0.13f, 1.0f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.18f, 0.18f, 0.18f, 1.0f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.21f, 0.21f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.24f, 0.68f, 0.96f, 1.0f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.73f, 0.73f, 0.73f, 1.0f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.24f, 0.68f, 0.96f, 1.0f);
    }

    public static bool PrimaryButton(string label, float width = 0f)
    {
        var size = new Vector2(width <= 0f ? 0f : width, RowHeight);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.22f, 0.66f, 0.96f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.28f, 0.72f, 1.0f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.16f, 0.56f, 0.90f, 1.0f));
        var clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    public static bool SecondaryButton(string label, float width = 0f)
    {
        var size = new Vector2(width <= 0f ? 0f : width, RowHeight);
        return ImGui.Button(label, size);
    }

    public static void SectionHeader(string title)
    {
        ImGui.SeparatorText(title);
    }

    public static void LabeledRow(string label, Action drawControl, float labelWidth = 150f)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        drawControl();
    }

    public static bool SliderIntWithInput(
        string id,
        ref int value,
        int min,
        int max,
        string format = "%d",
        int inputStep = 1,
        int inputFastStep = 100)
    {
        var changed = false;
        var available = ImGui.GetContentRegionAvail().X;
        var inputWidth = Math.Clamp(available * 0.32f, 80f, 140f);
        var sliderWidth = Math.Max(32f, available - inputWidth - Grid);

        ImGui.PushID(id);
        ImGui.SetNextItemWidth(sliderWidth);
        changed |= ImGui.SliderInt("##slider", ref value, min, max, format);
        ImGui.SameLine(0f, Grid);
        ImGui.SetNextItemWidth(inputWidth);
        changed |= ImGui.InputInt("##input", ref value, inputStep, inputFastStep);
        value = Math.Clamp(value, min, max);
        ImGui.PopID();

        return changed;
    }

    public static bool SliderFloatWithInput(
        string id,
        ref float value,
        float min,
        float max,
        string format = "%.3f",
        float inputStep = 0.01f,
        float inputFastStep = 0.1f)
    {
        var changed = false;
        var available = ImGui.GetContentRegionAvail().X;
        var inputWidth = Math.Clamp(available * 0.32f, 80f, 140f);
        var sliderWidth = Math.Max(32f, available - inputWidth - Grid);

        ImGui.PushID(id);
        ImGui.SetNextItemWidth(sliderWidth);
        changed |= ImGui.SliderFloat("##slider", ref value, min, max, format);
        ImGui.SameLine(0f, Grid);
        ImGui.SetNextItemWidth(inputWidth);
        changed |= ImGui.InputFloat("##input", ref value, inputStep, inputFastStep, format);
        value = Math.Clamp(value, min, max);
        ImGui.PopID();

        return changed;
    }
}
