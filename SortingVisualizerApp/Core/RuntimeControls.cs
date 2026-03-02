namespace SortingVisualizerApp.Core;

public sealed class RuntimeControls
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

    public double ResolveEventsPerSecond()
    {
        var baseRate = 1.0;
        if (SpeedMode == SpeedControlMode.EventsPerSecond)
        {
            baseRate = Math.Max(1.0, EventsPerSecond);
        }
        else
        {
            baseRate = DelayMs <= 0.0 ? 1_000_000.0 : Math.Max(1.0, 1000.0 / DelayMs);
        }

        return Math.Max(1.0, baseRate * Math.Clamp(PlaybackRate, 0.05, 20.0));
    }
}
