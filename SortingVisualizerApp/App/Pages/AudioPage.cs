using ImGuiNET;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow
{
    private void DrawAudioPage()
    {
        PanelTheme.SectionHeader("Audio");

        var audioEnabled = _controls.AudioEnabled;
        PanelTheme.LabeledRow("Mute", () =>
        {
            var muted = !audioEnabled;
            if (ImGui.Checkbox("##audio-mute", ref muted))
            {
                _controls.AudioEnabled = !muted;
            }
        });

        PanelTheme.LabeledRow("Volume", () =>
        {
            var volume = _controls.AudioVolume;
            if (PanelTheme.SliderFloatWithInput("audio-volume", ref volume, 0f, 1f, "%.2f", 0.01f, 0.10f))
            {
                _controls.AudioVolume = volume;
            }
        });

        PanelTheme.LabeledRow("fMin", () =>
        {
            var fMin = _controls.AudioMinFrequency;
            if (PanelTheme.SliderFloatWithInput("audio-fmin", ref fMin, 40f, 800f, "%.0f", 1f, 20f))
            {
                _controls.AudioMinFrequency = fMin;
                if (_controls.AudioMaxFrequency <= _controls.AudioMinFrequency)
                {
                    _controls.AudioMaxFrequency = _controls.AudioMinFrequency + 20f;
                }
            }
        });

        PanelTheme.LabeledRow("fMax", () =>
        {
            var fMax = _controls.AudioMaxFrequency;
            if (PanelTheme.SliderFloatWithInput("audio-fmax", ref fMax, 200f, 5000f, "%.0f", 1f, 50f))
            {
                _controls.AudioMaxFrequency = Math.Max(_controls.AudioMinFrequency + 20f, fMax);
            }
        });

        PanelTheme.LabeledRow("Duration", () =>
        {
            var duration = _controls.ToneDurationMs;
            if (PanelTheme.SliderFloatWithInput("audio-duration", ref duration, 20f, 50f, "%.1f", 0.1f, 1.0f))
            {
                _controls.ToneDurationMs = duration;
            }
        });

        PanelTheme.LabeledRow("Attack", () =>
        {
            var attack = _controls.AttackMs;
            if (PanelTheme.SliderFloatWithInput("audio-attack", ref attack, 2f, 8f, "%.1f", 0.1f, 0.5f))
            {
                _controls.AttackMs = attack;
            }
        });

        PanelTheme.LabeledRow("Decay", () =>
        {
            var decay = _controls.DecayMs;
            if (PanelTheme.SliderFloatWithInput("audio-decay", ref decay, 8f, 35f, "%.1f", 0.1f, 1.0f))
            {
                _controls.DecayMs = decay;
            }
        });

        PanelTheme.LabeledRow("Release", () =>
        {
            var release = _controls.ReleaseMs;
            if (PanelTheme.SliderFloatWithInput("audio-release", ref release, 5f, 20f, "%.1f", 0.1f, 1.0f))
            {
                _controls.ReleaseMs = release;
            }
        });

        PanelTheme.LabeledRow("Polyphony", () =>
        {
            var poly = _controls.PolyphonyCap;
            if (PanelTheme.SliderIntWithInput("audio-poly", ref poly, 4, 32, "%d", 1, 4))
            {
                _controls.PolyphonyCap = poly;
            }
        });

        PanelTheme.LabeledRow("Tones/Frame", () =>
        {
            var tones = _controls.MaxAudioEventsPerFrame;
            if (PanelTheme.SliderIntWithInput("audio-tones", ref tones, 1, 5, "%d", 1, 1))
            {
                _controls.MaxAudioEventsPerFrame = tones;
            }
        });

        var waveNames = Enum.GetNames<WaveformType>();
        var waveIndex = (int)_controls.Waveform;
        PanelTheme.LabeledRow("Waveform", () =>
        {
            if (ImGui.Combo("##audio-wave", ref waveIndex, waveNames, waveNames.Length))
            {
                _controls.Waveform = (WaveformType)waveIndex;
            }
        });

        var profileNames = Enum.GetNames<SonificationProfile>();
        var profile = (int)_controls.SonificationProfile;
        PanelTheme.LabeledRow("Profile", () =>
        {
            if (ImGui.Combo("##audio-profile", ref profile, profileNames, profileNames.Length))
            {
                _controls.SonificationProfile = (SonificationProfile)profile;
            }
        });

        var normalization = _controls.AudioNormalizationEnabled;
        if (ImGui.Checkbox("Loudness normalization", ref normalization))
        {
            _controls.AudioNormalizationEnabled = normalization;
        }

        var limiter = _controls.AudioLimiterEnabled;
        if (ImGui.Checkbox("Limiter", ref limiter))
        {
            _controls.AudioLimiterEnabled = limiter;
        }

        var stereoPan = _controls.AudioStereoPanEnabled;
        if (ImGui.Checkbox("Stereo spatialization", ref stereoPan))
        {
            _controls.AudioStereoPanEnabled = stereoPan;
        }

        var spatialPanByX = _controls.AudioSpatialPanByX;
        if (ImGui.Checkbox("Spatial view: pan by X", ref spatialPanByX))
        {
            _controls.AudioSpatialPanByX = spatialPanByX;
        }

        PanelTheme.SectionHeader("Priority");
        ImGui.TextUnformatted("Fixed policy: swap/write > pivot > compare.");
        ImGui.TextUnformatted($"Active voices: {_audioEngine?.ActiveVoices ?? 0}, dropped: {_audioEngine?.DroppedVoices ?? 0}");
    }
}
