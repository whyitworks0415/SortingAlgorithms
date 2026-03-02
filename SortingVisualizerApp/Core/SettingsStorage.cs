using System.Text.Json;

namespace SortingVisualizerApp.Core;

public sealed class VisualizerSettings
{
    public string SelectedAlgorithmId { get; set; } = string.Empty;
    public string[] Favorites { get; set; } = Array.Empty<string>();
    public int ArraySize { get; set; } = 512;
    public DistributionPreset Distribution { get; set; } = DistributionPreset.Random;
    public int StringCount { get; set; } = 256;
    public int StringLength { get; set; } = 12;
    public StringAlphabetSet StringAlphabet { get; set; } = StringAlphabetSet.Lowercase;
    public StringDistributionPreset StringDistribution { get; set; } = StringDistributionPreset.Random;
    public int SpatialCount { get; set; } = 2048;
    public SpatialDistributionPreset SpatialDistribution { get; set; } = SpatialDistributionPreset.Uniform;
    public bool SpatialShowOrder { get; set; } = true;
    public bool SpatialShowGrid { get; set; } = true;
    public VisualizationMode VisualizationMode { get; set; } = VisualizationMode.Bars;
    public float GraphEdgeDensity { get; set; } = 0.15f;
    public bool ShowSidePanel { get; set; } = true;
    public bool ShowHudOverlay { get; set; } = true;
    public bool ShowDiagnostics { get; set; } = false;
    public Dictionary<string, int> DifficultyOverrides { get; set; } = new();
    public RuntimeControlsDto Controls { get; set; } = new();
}

public sealed class RuntimeControlsDto
{
    public SpeedControlMode SpeedMode { get; set; } = SpeedControlMode.EventsPerSecond;
    public double EventsPerSecond { get; set; } = 5000.0;
    public double DelayMs { get; set; } = 0.1;
    public int Parallelism { get; set; } = Math.Clamp(Environment.ProcessorCount, 1, 32);
    public DetailLevel VisualDetail { get; set; } = DetailLevel.L2;
    public DetailLevel AudioDetail { get; set; } = DetailLevel.L1;
    public bool LinkDetails { get; set; } = true;
    public bool VisualEnabled { get; set; } = true;
    public bool AudioEnabled { get; set; } = true;
    public int MaxAudioEventsPerFrame { get; set; } = 3;
    public int MaxVisualEventsPerFrame { get; set; } = 2048;
    public float AudioVolume { get; set; } = 0.15f;
    public float AudioMinFrequency { get; set; } = 140.0f;
    public float AudioMaxFrequency { get; set; } = 1600.0f;
    public WaveformType Waveform { get; set; } = WaveformType.Sine;
    public SonificationProfile SonificationProfile { get; set; } = SonificationProfile.Default;
    public bool AudioNormalizationEnabled { get; set; } = true;
    public bool AudioLimiterEnabled { get; set; } = true;
    public bool AudioStereoPanEnabled { get; set; }
    public bool AudioSpatialPanByX { get; set; } = true;
    public float ToneDurationMs { get; set; } = 30.0f;
    public float AttackMs { get; set; } = 3.0f;
    public float DecayMs { get; set; } = 16.0f;
    public float ReleaseMs { get; set; } = 8.0f;
    public int PolyphonyCap { get; set; } = 16;
    public int OverlayIntensity { get; set; } = 80;
    public bool ShowRangesOverlay { get; set; } = true;
    public bool ShowPivotOverlay { get; set; } = true;
    public bool ShowBucketsOverlay { get; set; } = true;
    public bool ShowMemoryHeatmap { get; set; }
    public bool NormalizeHeatmapByMax { get; set; } = true;
    public int CacheLineSize { get; set; } = 64;
    public bool GpuAccelerationEnabled { get; set; } = true;
    public bool CompareCpuGpuTiming { get; set; } = true;
    public bool ShowGpuThreadOverlay { get; set; }
    public bool ShowGpuBitonicStageGrid { get; set; }
    public double PlaybackRate { get; set; } = 1.0;
}

public static class SettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(string path, VisualizerSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static VisualizerSettings? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<VisualizerSettings>(json, JsonOptions);
    }
}
