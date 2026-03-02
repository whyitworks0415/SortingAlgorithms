using System.Numerics;
using System.Globalization;
using System.Diagnostics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SortingVisualizerApp.Algorithms;
using SortingVisualizerApp.Audio;
using SortingVisualizerApp.Core;
using SortingVisualizerApp.Gpu;
using SortingVisualizerApp.Rendering;
using SortingVisualizerApp.Simulation;
using SortingVisualizerApp.UI;

namespace SortingVisualizerApp.App;

public sealed partial class VisualizerWindow : GameWindow
{
    private readonly AlgorithmRegistry _registry = new();
    private readonly SimulationEngine _simulation = new();
    private readonly SimulationEngine _comparisonSimulation = new();
    private readonly StringSimulationEngine _stringSimulation = new();
    private readonly SpatialSimulationEngine _spatialSimulation = new();
    private readonly RuntimeControls _controls = new();

    private ImGuiController? _imGui;
    private BarsViewRenderer? _barsRenderer;
    private NetworkViewRenderer? _networkRenderer;
    private ExternalViewRenderer? _externalRenderer;
    private GraphViewRenderer? _graphRenderer;
    private StringViewRenderer? _stringRenderer;
    private SpatialViewRenderer? _spatialRenderer;
    private SplitBarsRenderer? _splitBarsRenderer;
    private AudioEngine? _audioEngine;
    private GpuSortService? _gpuSortService;

    private string _selectedAlgorithmId = string.Empty;
    private string _algorithmSearch = string.Empty;
    private bool _showFavoritesOnly;
    private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
    private string _comparisonRightAlgorithmId = string.Empty;
    private bool _comparisonMode;
    private bool _comparisonAudioBoth;

    private DistributionPreset _distribution = DistributionPreset.Random;
    private float _distributionStrength = 0.6f;
    private float _duplicateStrength = 0.5f;
    private float _reverseStrength = 1.0f;
    private VisualizationMode _visualizationMode = VisualizationMode.Bars;
    private int _arraySize = 2048;
    private int _stringCount = 256;
    private int _stringLength = 12;
    private StringAlphabetSet _stringAlphabet = StringAlphabetSet.Lowercase;
    private StringDistributionPreset _stringDistribution = StringDistributionPreset.Random;
    private int _spatialCount = 4096;
    private SpatialDistributionPreset _spatialDistribution = SpatialDistributionPreset.Uniform;
    private bool _spatialShowOrder = true;
    private bool _spatialShowGrid = true;
    private int _currentSeed;
    private float _graphEdgeDensity = 0.15f;

    private bool _showSidePanel = true;
    private bool _showHudOverlay = true;
    private bool _showDiagnostics;
    private bool _panelAutoHide;

    private UiPanelPage _currentPanelPage = UiPanelPage.Run;
    private int _algorithmStatusFilterIndex;
    private int _algorithmViewFilterIndex;
    private string _algorithmCategoryFilter = "All";
    private readonly string[] _panelPageLabels =
    {
        "1 Run",
        "2 Data",
        "3 Algorithm",
        "4 View",
        "5 Audio",
        "6 Analysis",
        "7 Replay"
    };

    private int[] _snapshotBuffer = Array.Empty<int>();
    private int[] _memoryAccessBuffer = Array.Empty<int>();
    private int _memoryAccessMax;
    private StringItem[] _stringSnapshotBuffer = Array.Empty<StringItem>();
    private int[] _stringMemoryAccessBuffer = Array.Empty<int>();
    private int _stringMemoryAccessMax;
    private SpatialPoint[] _spatialSnapshotBuffer = Array.Empty<SpatialPoint>();
    private uint[] _spatialKeyBuffer = Array.Empty<uint>();
    private int[] _spatialMemoryAccessBuffer = Array.Empty<int>();
    private int _spatialMemoryAccessMax;
    private readonly SortEvent[] _visualEventsBuffer = new SortEvent[4096];
    private readonly SortEvent[] _comparisonVisualEventsBuffer = new SortEvent[4096];
    private readonly AudioTrigger[] _audioBuffer = new AudioTrigger[64];
    private readonly AudioTrigger[] _comparisonAudioBuffer = new AudioTrigger[64];
    private int[] _comparisonSnapshotBuffer = Array.Empty<int>();
    private int[] _comparisonMemoryAccessBuffer = Array.Empty<int>();
    private int _comparisonMemoryAccessMax;
    private int _comparisonMaxValue = 1;

    private double _fpsTimer;
    private int _fpsFrames;
    private float _fps;

    private bool _recordNextRun = true;
    private ReplayFileV1? _lastReplay;
    private ReplayFileV1? _loadedReplay;
    private bool _replayMode;
    private int _replayEventCursor;
    private bool _pendingAutoReplayCapture;

    private string _statusText = "Ready";
    private GpuExecutionMetrics _lastGpuMetrics = GpuExecutionMetrics.Empty;
    private double _gpuProgress;
    private bool _effectiveShowMemoryHeatmap;
    private const int HeatmapAutoDisableThreshold = 1_000_000;
    private int _snapshotCounter;
    private bool _snapshotRequested;
    private NetworkSchedule? _activeNetworkSchedule;
    private int _networkCurrentStage = -1;
    private readonly HashSet<long> _networkSwapPairs = new();
    private readonly Dictionary<int, ExternalRunTracker> _externalRuns = new();
    private readonly Dictionary<int, ExternalMergeGroupTracker> _externalGroups = new();
    private GraphDefinition? _activeGraph;
    private int[] _graphInDegrees = Array.Empty<int>();
    private bool[] _graphEmitted = Array.Empty<bool>();
    private int _graphSelectedNode = -1;
    private (int From, int To)? _graphActiveEdge;
    private int _stringHighlightRowA = -1;
    private int _stringHighlightRowB = -1;
    private int _stringCurrentCharIndex = -1;
    private int[] _stringBucketHistogram = Array.Empty<int>();
    private readonly List<int> _spatialHighlightedIndices = new(8);
    private SpatialRegionHighlight? _spatialRegionHighlight;
    private readonly List<string> _registryMetadataIssues = new();
    private string _registrySummaryText = string.Empty;
    private readonly List<ComparisonAnalysisRecord> _comparisonHistory = new();
    private bool _comparisonCapturePending;
    private string _lastComparisonExportPath = string.Empty;

    private bool _benchmarkUseFavorites = true;
    private bool _benchmarkHeadless = true;
    private int _benchmarkSeed = 1337;
    private Task<BenchmarkSuiteResult>? _benchmarkTask;
    private CancellationTokenSource? _benchmarkCts;
    private BenchmarkSuiteResult? _lastBenchmarkSuite;
    private string _lastBenchmarkCsvPath = string.Empty;
    private string _benchmarkStatusText = "Benchmark idle.";
    private Task<GrowthBenchmarkSuiteResult>? _growthTask;
    private CancellationTokenSource? _growthCts;
    private GrowthBenchmarkSuiteResult? _lastGrowthSuite;
    private string _growthStatusText = "Growth analysis idle.";
    private string _lastGrowthExportPath = string.Empty;
    private string _growthSizeSeries = "128,256,512,1024,2048,4096";
    private int _growthSeed = 1337;
    private bool _growthHeadless = true;

    private readonly string _appDataDir;
    private readonly string _analysisRootDir;
    private readonly string _settingsPath;
    private readonly string _presetPath;
    private readonly string _defaultReplayPath;
    private string _replayPathInput;

    public VisualizerWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SortingVisualizerApp");
        _analysisRootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortingVisualizerApp");
        _settingsPath = Path.Combine(_appDataDir, "settings.json");
        _presetPath = Path.Combine(_appDataDir, "preset.json");
        _defaultReplayPath = Path.Combine(_appDataDir, "replays", "latest.replay");
        _replayPathInput = _defaultReplayPath;
        _currentSeed = Random.Shared.Next();
        _benchmarkSeed = _currentSeed;
        _growthSeed = _currentSeed;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Directory.CreateDirectory(_appDataDir);

        _imGui = new ImGuiController(this);
        _barsRenderer = new BarsViewRenderer();
        _networkRenderer = new NetworkViewRenderer();
        _externalRenderer = new ExternalViewRenderer();
        _graphRenderer = new GraphViewRenderer();
        _stringRenderer = new StringViewRenderer();
        _spatialRenderer = new SpatialViewRenderer();
        _splitBarsRenderer = new SplitBarsRenderer();
        _audioEngine = new AudioEngine();
        _gpuSortService = new GpuSortService(Path.Combine(AppContext.BaseDirectory, "Gpu", "Shaders"));
        _gpuSortService.Initialize();

        GL.ClearColor(0.03f, 0.03f, 0.03f, 1f);

        var firstImplemented = _registry.All.FirstOrDefault(static x => x.IsImplemented);
        _selectedAlgorithmId = firstImplemented?.Id ?? string.Empty;
        var firstBarsImplemented = _registry.All.FirstOrDefault(static x =>
            x.Status == AlgorithmImplementationStatus.A
            && (x.SupportedViews & SupportedViews.Bars) != 0
            && x.Factory is not null);
        _comparisonRightAlgorithmId = firstBarsImplemented?.Id ?? _selectedAlgorithmId;

        LoadSettings(_settingsPath, "settings");
        if (_registry.TryGet(_selectedAlgorithmId, out var selectedMeta))
        {
            EnsureModeCompatibility(selectedMeta, setStatus: false);
        }

#if DEBUG
        ValidateRegistryMetadata(includeStableSmoke: true);
#else
        ValidateRegistryMetadata(includeStableSmoke: false);
#endif

        if (!HasCurrentModeData())
        {
            RegenerateData();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _imGui?.WindowResized(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _simulation.UpdateRuntimeControls(_controls);
        _comparisonSimulation.UpdateRuntimeControls(_controls);
        _stringSimulation.UpdateRuntimeControls(_controls);
        _spatialSimulation.UpdateRuntimeControls(_controls);

        _fpsTimer += args.Time;
        _fpsFrames++;
        if (_fpsTimer >= 0.5)
        {
            _fps = (float)(_fpsFrames / _fpsTimer);
            _fpsTimer = 0;
            _fpsFrames = 0;
        }

        TryCaptureReplayAfterCompletion();
        PollBenchmarkTask();
        PollGrowthTask();
        TryCaptureComparisonCompletion();

        if (_panelAutoHide)
        {
            _showSidePanel = !IsAnySimulationRunning();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        var barsCount = 0;
        var maxValue = 1;
        var stringCount = 0;
        var spatialCount = 0;
        var comparisonBarsCount = 0;
        _comparisonMaxValue = 1;

        var visualCount = 0;
        var comparisonVisualCount = 0;
        var audioCount = 0;
        var comparisonAudioCount = 0;
        var visualBudget = ResolveFrameVisualBudget();
        _effectiveShowMemoryHeatmap = _controls.ShowMemoryHeatmap;

        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            var leftBudget = Math.Max(64, visualBudget / 2);
            var rightBudget = Math.Max(64, visualBudget - leftBudget);
            barsCount = _simulation.CopyDataTo(ref _snapshotBuffer, out maxValue);
            _simulation.CopyMemoryAccessTo(ref _memoryAccessBuffer, out _memoryAccessMax);
            visualCount = _simulation.DrainVisualEvents(_visualEventsBuffer.AsSpan(0, Math.Min(_visualEventsBuffer.Length, leftBudget)));
            audioCount = _simulation.DrainAudioEvents(_audioBuffer);

            comparisonBarsCount = _comparisonSimulation.CopyDataTo(ref _comparisonSnapshotBuffer, out _comparisonMaxValue);
            _comparisonSimulation.CopyMemoryAccessTo(ref _comparisonMemoryAccessBuffer, out _comparisonMemoryAccessMax);
            comparisonVisualCount = _comparisonSimulation.DrainVisualEvents(_comparisonVisualEventsBuffer.AsSpan(0, Math.Min(_comparisonVisualEventsBuffer.Length, rightBudget)));
            comparisonAudioCount = _comparisonSimulation.DrainAudioEvents(_comparisonAudioBuffer);
        }
        else if (IsBarsFamilyMode(_visualizationMode))
        {
            barsCount = _simulation.CopyDataTo(ref _snapshotBuffer, out maxValue);
            _simulation.CopyMemoryAccessTo(ref _memoryAccessBuffer, out _memoryAccessMax);
            visualCount = _simulation.DrainVisualEvents(_visualEventsBuffer.AsSpan(0, Math.Min(_visualEventsBuffer.Length, visualBudget)));
            audioCount = _simulation.DrainAudioEvents(_audioBuffer);
        }
        else if (_visualizationMode == VisualizationMode.String)
        {
            stringCount = _stringSimulation.CopyDataTo(ref _stringSnapshotBuffer);
            _stringSimulation.CopyMemoryAccessTo(ref _stringMemoryAccessBuffer, out _stringMemoryAccessMax);
            visualCount = _stringSimulation.DrainVisualEvents(_visualEventsBuffer.AsSpan(0, Math.Min(_visualEventsBuffer.Length, visualBudget)));
            audioCount = _stringSimulation.DrainAudioEvents(_audioBuffer);
        }
        else if (_visualizationMode == VisualizationMode.Spatial)
        {
            spatialCount = _spatialSimulation.CopyDataTo(ref _spatialSnapshotBuffer, ref _spatialKeyBuffer);
            _spatialSimulation.CopyMemoryAccessTo(ref _spatialMemoryAccessBuffer, out _spatialMemoryAccessMax);
            visualCount = _spatialSimulation.DrainVisualEvents(_visualEventsBuffer.AsSpan(0, Math.Min(_visualEventsBuffer.Length, visualBudget)));
            audioCount = _spatialSimulation.DrainAudioEvents(_audioBuffer);
        }

        var visualEvents = _visualEventsBuffer.AsSpan(0, visualCount);
        UpdateViewTrackers(visualEvents);

        var activeCount = Math.Max(Math.Max(barsCount, stringCount), spatialCount);
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            activeCount = Math.Max(activeCount, comparisonBarsCount);
        }

        if (_controls.ShowMemoryHeatmap && activeCount >= HeatmapAutoDisableThreshold)
        {
            _effectiveShowMemoryHeatmap = false;
        }

        var frameState = BuildFrameState(
            _snapshotBuffer,
            barsCount,
            maxValue,
            _memoryAccessBuffer,
            _memoryAccessMax,
            _stringSnapshotBuffer,
            stringCount,
            _stringMemoryAccessBuffer,
            _stringMemoryAccessMax,
            _spatialSnapshotBuffer,
            _spatialKeyBuffer,
            spatialCount,
            _spatialMemoryAccessBuffer,
            _spatialMemoryAccessMax,
            _effectiveShowMemoryHeatmap,
            _visualEventsBuffer,
            visualCount);

        if (_imGui is not null)
        {
            _imGui.Update((float)args.Time);
        }

        if (_comparisonMode
            && _visualizationMode == VisualizationMode.Bars
            && _splitBarsRenderer is not null)
        {
            _splitBarsRenderer.Draw(
                _snapshotBuffer,
                barsCount,
                maxValue,
                _memoryAccessBuffer,
                _memoryAccessMax,
                _visualEventsBuffer.AsSpan(0, visualCount),
                _comparisonSnapshotBuffer,
                comparisonBarsCount,
                _comparisonMaxValue,
                _comparisonMemoryAccessBuffer,
                _comparisonMemoryAccessMax,
                _comparisonVisualEventsBuffer.AsSpan(0, comparisonVisualCount),
                ClientSize.X,
                ClientSize.Y,
                _controls.VisualEnabled,
                Math.Clamp(_controls.OverlayIntensity / 100.0f, 0.0f, 1.0f),
                _controls.ShowRangesOverlay,
                _controls.ShowPivotOverlay,
                _controls.ShowBucketsOverlay,
                _effectiveShowMemoryHeatmap,
                _controls.NormalizeHeatmapByMax);
        }
        else
        {
            DrawActiveView(frameState);
        }

        _audioEngine?.PlayTriggers(_audioBuffer, audioCount, _controls);
        if (_comparisonMode && _comparisonAudioBoth)
        {
            _audioEngine?.PlayTriggers(_comparisonAudioBuffer, comparisonAudioCount, _controls);
        }

        if (_imGui is not null)
        {
            if (_showSidePanel)
            {
                DrawSidePanel();
            }

            if (_showHudOverlay)
            {
                DrawHudOverlay();
            }

            if (_showDiagnostics)
            {
                DrawDiagnosticsOverlay();
            }

            _imGui.Render();
        }

        if (_snapshotRequested)
        {
            CaptureSnapshot();
            _snapshotRequested = false;
        }

        SwapBuffers();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.IsRepeat)
        {
            return;
        }

        if (e.Alt && TryHandlePanelPageHotkey(e.Key))
        {
            return;
        }

        switch (e.Key)
        {
            case Keys.U:
                _showSidePanel = !_showSidePanel;
                break;

            case Keys.H:
                _showHudOverlay = !_showHudOverlay;
                break;

            case Keys.D:
                _showDiagnostics = !_showDiagnostics;
                break;

            case Keys.F:
                ToggleFullscreen();
                break;

            case Keys.D1:
            case Keys.KeyPad1:
                SetVisualizationMode(VisualizationMode.Bars);
                break;

            case Keys.D2:
            case Keys.KeyPad2:
                SetVisualizationMode(VisualizationMode.Network);
                break;

            case Keys.D3:
            case Keys.KeyPad3:
                SetVisualizationMode(VisualizationMode.External);
                break;

            case Keys.D4:
            case Keys.KeyPad4:
                SetVisualizationMode(VisualizationMode.Graph);
                break;

            case Keys.D5:
            case Keys.KeyPad5:
                SetVisualizationMode(VisualizationMode.String);
                break;

            case Keys.D6:
            case Keys.KeyPad6:
                SetVisualizationMode(VisualizationMode.Spatial);
                break;

            case Keys.Space:
            {
                var stats = GetCurrentStats();
                if (!stats.IsRunning)
                {
                    StartSelectedAlgorithm();
                }
                else
                {
                    ToggleCurrentPause();
                }
                break;
            }

            case Keys.S:
                StepCurrentOnce();
                break;

            case Keys.R:
                ResetCurrentToSource();
                ResetViewTrackers();
                break;

            case Keys.G:
                RegenerateData();
                break;
        }
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        SaveSettings(_settingsPath, "settings");
        _benchmarkCts?.Cancel();
        _growthCts?.Cancel();

        _simulation.Dispose();
        _comparisonSimulation.Dispose();
        _stringSimulation.Dispose();
        _spatialSimulation.Dispose();
        _audioEngine?.Dispose();
        _barsRenderer?.Dispose();
        _networkRenderer?.Dispose();
        _externalRenderer?.Dispose();
        _graphRenderer?.Dispose();
        _stringRenderer?.Dispose();
        _spatialRenderer?.Dispose();
        _splitBarsRenderer?.Dispose();
        _imGui?.Dispose();
        _gpuSortService?.Dispose();
    }

    private void DrawSidePanel()
    {
        PanelTheme.ApplyMinimalTheme();

        var panelWidth = 520.0f;
        var panelHeight = Math.Max(360.0f, ClientSize.Y - 24.0f);
        var panelX = Math.Max(12.0f, ClientSize.X - panelWidth - 12.0f);
        ImGui.SetNextWindowPos(new Vector2(panelX, 12.0f), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(panelWidth, panelHeight), ImGuiCond.Always);

        if (!ImGui.Begin("Control Panel", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
        {
            ImGui.End();
            return;
        }

        DrawPanelPageNavigation();
        ImGui.Separator();

        switch (_currentPanelPage)
        {
            case UiPanelPage.Run:
                DrawRunPage();
                break;
            case UiPanelPage.Data:
                DrawDataPage();
                break;
            case UiPanelPage.Algorithm:
                DrawAlgorithmPage();
                break;
            case UiPanelPage.View:
                DrawViewPage();
                break;
            case UiPanelPage.Audio:
                DrawAudioPage();
                break;
            case UiPanelPage.Analysis:
                DrawAnalysisPage();
                break;
            case UiPanelPage.ReplayExport:
                DrawReplayExportPage();
                break;
        }

        ImGui.End();
    }

    private void DrawHudOverlay()
    {
        var stats = GetCurrentStats();

        ImGui.SetNextWindowPos(new Vector2(18, 18), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.76f);

        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (!ImGui.Begin("HUD", flags))
        {
            ImGui.End();
            return;
        }

        ImGui.TextUnformatted($"FPS: {_fps:0.0}  Frame: {(_fps > 0 ? 1000.0 / _fps : 0.0):0.00} ms");
        ImGui.TextUnformatted($"Events/s: {stats.EffectiveEventsPerSecond:0.0}  Total: {stats.ProcessedEvents:N0}");
        ImGui.TextUnformatted($"Cmp: {stats.Comparisons:N0}  Swp: {stats.Swaps:N0}  Wr: {stats.Writes:N0}  DropCmp: {stats.DroppedComparisons:N0}");
        ImGui.TextUnformatted($"Cache hit/miss: {stats.CacheHits:N0}/{stats.CacheMisses:N0}  Parallel q:{stats.ParallelQueueDepth} active:{stats.ActiveParallelTasks}");
        ImGui.TextUnformatted($"GPU: {(_lastGpuMetrics.UsedGpu ? _lastGpuMetrics.Kind.ToString() : "off/fallback")}  dispatch {_lastGpuMetrics.DispatchMs:0.00} ms");
        ImGui.ProgressBar((float)Math.Clamp(_gpuProgress, 0.0, 1.0), new Vector2(180, 0), $"{Math.Clamp(_gpuProgress, 0.0, 1.0) * 100.0:0}%");
        ImGui.TextUnformatted($"State: {(stats.IsRunning ? (stats.IsPaused ? "Paused" : "Running") : (stats.IsCompleted ? "Completed" : "Idle"))}");

        if (_visualizationMode == VisualizationMode.Bars)
        {
            var bins = 0;
            var widthPx = 0.0f;
            var mode = BarsRenderMode.BarsRaw;
            var visibleCount = 0;

            if (_comparisonMode)
            {
                bins = _splitBarsRenderer?.LastLodBins ?? 0;
                widthPx = _splitBarsRenderer?.LastNominalBarWidthPx ?? 0.0f;
                mode = _splitBarsRenderer?.LastRenderMode ?? BarsRenderMode.BarsRaw;
                visibleCount = _splitBarsRenderer?.LastVisibleCount ?? 0;
            }
            else
            {
                bins = _barsRenderer?.LastLodBins ?? 0;
                widthPx = _barsRenderer?.LastNominalBarWidthPx ?? 0.0f;
                mode = _barsRenderer?.LastRenderMode ?? BarsRenderMode.BarsRaw;
                visibleCount = _barsRenderer?.LastVisibleCount ?? 0;
            }

            ImGui.TextUnformatted($"Bars: {mode}  barWidth: {widthPx:0.00}px  bins: {bins:N0}  visible: {visibleCount:N0}");
        }

        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            var left = _simulation.GetStatisticsSnapshot();
            var right = _comparisonSimulation.GetStatisticsSnapshot();
            ImGui.Separator();
            ImGui.TextUnformatted($"L  cmp {left.Comparisons:N0} / swp {left.Swaps:N0} / wr {left.Writes:N0} / {left.ElapsedMs:0.0} ms");
            ImGui.TextUnformatted($"R  cmp {right.Comparisons:N0} / swp {right.Swaps:N0} / wr {right.Writes:N0} / {right.ElapsedMs:0.0} ms");

            var drawList = ImGui.GetBackgroundDrawList();
            var x = ClientSize.X * 0.5f;
            drawList.AddLine(new Vector2(x, 0), new Vector2(x, ClientSize.Y), PackColor(140, 140, 140, 100), 1.0f);
        }

        if (_controls.ShowGpuThreadOverlay && _lastGpuMetrics.UsedGpu && IsBarsFamilyMode(_visualizationMode))
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var groups = Math.Clamp(_lastGpuMetrics.WorkGroupCount, 1, 48);
            for (var g = 1; g < groups; g++)
            {
                var x = ClientSize.X * (g / (float)groups);
                drawList.AddLine(new Vector2(x, 0), new Vector2(x, ClientSize.Y), PackColor(65, 145, 210, 26), 1.0f);
            }
        }

        if (_controls.ShowGpuBitonicStageGrid && _lastGpuMetrics.UsedGpu && _lastGpuMetrics.Kind == GpuSortKind.Bitonic)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var stages = Math.Clamp(_lastGpuMetrics.StageCount, 1, 96);
            var y0 = 2.0f;
            var y1 = 10.0f;
            for (var s = 0; s < stages; s++)
            {
                var x = 4.0f + (ClientSize.X - 8.0f) * (s / (float)Math.Max(1, stages - 1));
                drawList.AddLine(new Vector2(x, y0), new Vector2(x, y1), PackColor(245, 170, 70, 85), 1.0f);
            }
        }

        ImGui.End();
    }

    private void DrawDiagnosticsOverlay()
    {
        var stats = GetCurrentStats();
        var queues = GetCurrentQueueDepthSnapshot();

        ImGui.SetNextWindowPos(new Vector2(18, 140), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.78f);

        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (!ImGui.Begin("Diagnostics", flags))
        {
            ImGui.End();
            return;
        }

        ImGui.TextUnformatted("Diagnostics");
        ImGui.TextUnformatted($"Visual queue depth: {queues.VisualQueueDepth}");
        ImGui.TextUnformatted($"Audio queue depth: {queues.AudioQueueDepth}");
        var lodBins = 0;
        var lodAsync = false;
        var lodQueue = 0;

        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            lodBins = _splitBarsRenderer?.LastLodBins ?? 0;
            lodAsync = _splitBarsRenderer?.UsedAsyncLod == true;
            lodQueue = _splitBarsRenderer?.LodQueueDepth ?? 0;
        }
        else if (IsBarsFamilyMode(_visualizationMode))
        {
            lodBins = _barsRenderer?.LastLodBins ?? 0;
            lodAsync = _barsRenderer?.UsedAsyncLod == true;
            lodQueue = _barsRenderer?.LodQueueDepth ?? 0;
        }
        else if (_visualizationMode == VisualizationMode.Spatial)
        {
            lodBins = _spatialRenderer?.LastLodBins ?? 0;
            lodAsync = _spatialRenderer?.UsedAsyncLod == true;
            lodQueue = _spatialRenderer?.LodQueueDepth ?? 0;
        }

        var droppedCompares = stats.DroppedComparisons;
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            var left = _simulation.GetStatisticsSnapshot();
            var right = _comparisonSimulation.GetStatisticsSnapshot();
            droppedCompares = left.DroppedComparisons + right.DroppedComparisons;
        }

        ImGui.TextUnformatted($"LOD bins: {lodBins}");
        if (_visualizationMode == VisualizationMode.Bars)
        {
            var widthPx = _comparisonMode
                ? (_splitBarsRenderer?.LastNominalBarWidthPx ?? 0.0f)
                : (_barsRenderer?.LastNominalBarWidthPx ?? 0.0f);
            var visibleCount = _comparisonMode
                ? (_splitBarsRenderer?.LastVisibleCount ?? 0)
                : (_barsRenderer?.LastVisibleCount ?? 0);
            var mode = _comparisonMode
                ? (_splitBarsRenderer?.LastRenderMode ?? BarsRenderMode.BarsRaw)
                : (_barsRenderer?.LastRenderMode ?? BarsRenderMode.BarsRaw);
            ImGui.TextUnformatted($"Bars render mode: {mode}  barWidth(px): {widthPx:0.00}  visible: {visibleCount:N0}");
        }
        ImGui.TextUnformatted($"LOD async: {(lodAsync ? "on" : "off")}");
        ImGui.TextUnformatted($"LOD worker queue: {lodQueue}");
        ImGui.TextUnformatted($"Dropped compares: {droppedCompares:N0}");
        ImGui.TextUnformatted($"Cache hit/miss: {stats.CacheHits:N0}/{stats.CacheMisses:N0}");
        ImGui.TextUnformatted($"Parallel queue: {stats.ParallelQueueDepth}  Active tasks: {stats.ActiveParallelTasks}");
        ImGui.TextUnformatted($"Bad partitions: {stats.BadPartitions:N0}  Pivot quality: {stats.PivotQualityScore:0.000}");
        ImGui.TextUnformatted($"GPU used: {_lastGpuMetrics.UsedGpu} ({_lastGpuMetrics.Kind})");
        ImGui.TextUnformatted($"GPU upload/dispatch/readback: {_lastGpuMetrics.UploadMs:0.00}/{_lastGpuMetrics.DispatchMs:0.00}/{_lastGpuMetrics.ReadbackMs:0.00} ms");
        ImGui.TextUnformatted($"GPU dispatch count/stages/groups: {_lastGpuMetrics.DispatchCount}/{_lastGpuMetrics.StageCount}/{_lastGpuMetrics.WorkGroupCount}");
        ImGui.TextUnformatted($"GPU memory estimate: {_lastGpuMetrics.GpuMemoryBytes / (1024.0 * 1024.0):0.00} MiB");
        ImGui.TextUnformatted($"Heatmap effective: {_effectiveShowMemoryHeatmap} (requested={_controls.ShowMemoryHeatmap})");
        ImGui.TextUnformatted($"Audio active voices: {_audioEngine?.ActiveVoices ?? 0}");
        ImGui.TextUnformatted($"Audio dropped voices: {_audioEngine?.DroppedVoices ?? 0}");
        ImGui.TextUnformatted($"Elapsed: {stats.ElapsedMs:0.0} ms");
        ImGui.TextUnformatted($"Registry issues: {_registryMetadataIssues.Count}");

        ImGui.End();
    }

    private void DrawAlgorithmSection()
    {
        ImGui.TextUnformatted("Algorithm");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##alg-search", "Search algorithm", ref _algorithmSearch, 128);
        ImGui.Checkbox("Favorites only", ref _showFavoritesOnly);

        ImGui.BeginChild("alg-list", new Vector2(0, 200), ImGuiChildFlags.Borders);

        string? lastCategory = null;
        foreach (var meta in GetFilteredAlgorithms())
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
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.55f, 1f));
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

        if (_registry.TryGet(_selectedAlgorithmId, out var metaInfo))
        {
            ImGui.TextUnformatted($"Selected: {metaInfo.Name} ({metaInfo.Status})");
            ImGui.TextUnformatted($"Stable: {(metaInfo.Stable.HasValue ? (metaInfo.Stable.Value ? "Yes" : "No") : "-")}");
            ImGui.TextUnformatted($"Avg/Worst: {metaInfo.AverageComplexity} / {metaInfo.WorstComplexity}");
            ImGui.TextUnformatted($"Views: {metaInfo.SupportedViews.ToDisplayString()}");
        }
    }

    private void DrawDataAndVisualizationSection()
    {
        ImGui.TextUnformatted("Data / Visualization");
        var modeNames = new[] { "Bars", "Network", "External", "Graph", "String", "Spatial" };
        var modes = new[]
        {
            VisualizationMode.Bars,
            VisualizationMode.Network,
            VisualizationMode.External,
            VisualizationMode.Graph,
            VisualizationMode.String,
            VisualizationMode.Spatial
        };
        var modeIndex = Array.IndexOf(modes, _visualizationMode);
        if (modeIndex < 0)
        {
            modeIndex = 0;
        }

        if (ImGui.Combo("Visualization", ref modeIndex, modeNames, modeNames.Length))
        {
            SetVisualizationMode(modes[Math.Clamp(modeIndex, 0, modes.Length - 1)]);
        }

        if (_visualizationMode == VisualizationMode.String)
        {
            ImGui.SliderInt("String N", ref _stringCount, 8, 5000);
            ImGui.SliderInt("String Length", ref _stringLength, 2, 64);

            var alphabetNames = Enum.GetNames<StringAlphabetSet>();
            var alphabetIndex = (int)_stringAlphabet;
            if (ImGui.Combo("Alphabet", ref alphabetIndex, alphabetNames, alphabetNames.Length))
            {
                _stringAlphabet = (StringAlphabetSet)alphabetIndex;
            }

            var stringDistNames = Enum.GetNames<StringDistributionPreset>();
            var stringDistIndex = (int)_stringDistribution;
            if (ImGui.Combo("String Dist", ref stringDistIndex, stringDistNames, stringDistNames.Length))
            {
                _stringDistribution = (StringDistributionPreset)stringDistIndex;
            }
        }
        else if (_visualizationMode == VisualizationMode.Spatial)
        {
            ImGui.SliderInt("Point N", ref _spatialCount, 32, 200000);

            var spatialDistNames = Enum.GetNames<SpatialDistributionPreset>();
            var spatialDistIndex = (int)_spatialDistribution;
            if (ImGui.Combo("Spatial Dist", ref spatialDistIndex, spatialDistNames, spatialDistNames.Length))
            {
                _spatialDistribution = (SpatialDistributionPreset)spatialDistIndex;
            }

            ImGui.Checkbox("ShowOrder", ref _spatialShowOrder);
            ImGui.Checkbox("ShowGrid", ref _spatialShowGrid);
        }
        else
        {
            ImGui.SliderInt("N", ref _arraySize, 8, 5000000);

            var distNames = Enum.GetNames<DistributionPreset>();
            var distIndex = (int)_distribution;
            if (ImGui.Combo("Distribution", ref distIndex, distNames, distNames.Length))
            {
                _distribution = (DistributionPreset)distIndex;
            }
        }

        if (_registry.TryGet(_selectedAlgorithmId, out var meta) && !_visualizationMode.IsSupportedBy(meta.SupportedViews))
        {
            ImGui.TextColored(new Vector4(1f, 0.55f, 0.32f, 1f), "Selected algorithm does not support this view.");
            if (ImGui.SmallButton("Switch to supported view"))
            {
                EnsureModeCompatibility(meta, setStatus: true);
            }
        }

        if (_visualizationMode == VisualizationMode.Graph)
        {
            ImGui.SliderFloat("Graph edge density", ref _graphEdgeDensity, 0.01f, 0.45f, "%.2f");
        }

        if (ImGui.Button("Generate", new Vector2(100, 28)))
        {
            RegenerateData();
        }

        ImGui.SameLine();
        if (ImGui.Button("Shuffle", new Vector2(100, 28)))
        {
            RegenerateData();
        }

        ImGui.SameLine();
        if (ImGui.Button("Snapshot", new Vector2(100, 28)))
        {
            _snapshotRequested = true;
        }
    }

    private void DrawSpeedAndAudioSection()
    {
        ImGui.TextUnformatted("Speed / Detail / Audio");

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

        var eps = (float)_controls.EventsPerSecond;
        if (ImGui.SliderFloat("Target Events/s", ref eps, 1f, 300000f, "%.0f"))
        {
            _controls.EventsPerSecond = eps;
        }

        var delay = (float)_controls.DelayMs;
        if (ImGui.SliderFloat("Delay (ms)", ref delay, 0f, 100f, "%.3f"))
        {
            _controls.DelayMs = delay;
        }

        var visualDetail = (int)_controls.VisualDetail;
        if (ImGui.SliderInt("Visual Detail L1-L3", ref visualDetail, 1, 3))
        {
            _controls.VisualDetail = (DetailLevel)visualDetail;
            if (_controls.LinkDetails)
            {
                _controls.AudioDetail = _controls.VisualDetail;
            }
        }

        var linkDetails = _controls.LinkDetails;
        if (ImGui.Checkbox("Link Audio Detail", ref linkDetails))
        {
            _controls.LinkDetails = linkDetails;
            if (_controls.LinkDetails)
            {
                _controls.AudioDetail = _controls.VisualDetail;
            }
        }

        if (!_controls.LinkDetails)
        {
            var audioDetail = (int)_controls.AudioDetail;
            if (ImGui.SliderInt("Audio Detail L1-L3", ref audioDetail, 1, 3))
            {
                _controls.AudioDetail = (DetailLevel)audioDetail;
            }
        }

        var visualEnabled = _controls.VisualEnabled;
        if (ImGui.Checkbox("Visual Enabled", ref visualEnabled))
        {
            _controls.VisualEnabled = visualEnabled;
        }

        var audioEnabled = _controls.AudioEnabled;
        if (ImGui.Checkbox("Audio Enabled", ref audioEnabled))
        {
            _controls.AudioEnabled = audioEnabled;
        }

        var tones = _controls.MaxAudioEventsPerFrame;
        if (ImGui.SliderInt("Max tones/frame", ref tones, 1, 5))
        {
            _controls.MaxAudioEventsPerFrame = tones;
        }

        var polyphony = _controls.PolyphonyCap;
        if (ImGui.SliderInt("Polyphony cap", ref polyphony, 4, 32))
        {
            _controls.PolyphonyCap = polyphony;
        }

        var volume = _controls.AudioVolume;
        if (ImGui.SliderFloat("Master volume", ref volume, 0f, 1f, "%.2f"))
        {
            _controls.AudioVolume = volume;
        }

        var fMin = _controls.AudioMinFrequency;
        if (ImGui.SliderFloat("fMin", ref fMin, 40f, 800f, "%.0f Hz"))
        {
            _controls.AudioMinFrequency = fMin;
            if (_controls.AudioMaxFrequency <= _controls.AudioMinFrequency)
            {
                _controls.AudioMaxFrequency = _controls.AudioMinFrequency + 20f;
            }
        }

        var fMax = _controls.AudioMaxFrequency;
        if (ImGui.SliderFloat("fMax", ref fMax, 200f, 5000f, "%.0f Hz"))
        {
            _controls.AudioMaxFrequency = Math.Max(_controls.AudioMinFrequency + 20f, fMax);
        }

        var duration = _controls.ToneDurationMs;
        if (ImGui.SliderFloat("Tone duration", ref duration, 20f, 50f, "%.1f ms"))
        {
            _controls.ToneDurationMs = duration;
        }

        var attack = _controls.AttackMs;
        if (ImGui.SliderFloat("Attack", ref attack, 2f, 8f, "%.1f ms"))
        {
            _controls.AttackMs = attack;
        }

        var decay = _controls.DecayMs;
        if (ImGui.SliderFloat("Decay", ref decay, 8f, 35f, "%.1f ms"))
        {
            _controls.DecayMs = decay;
        }

        var release = _controls.ReleaseMs;
        if (ImGui.SliderFloat("Release", ref release, 5f, 20f, "%.1f ms"))
        {
            _controls.ReleaseMs = release;
        }

        var waveNames = Enum.GetNames<WaveformType>();
        var waveIndex = (int)_controls.Waveform;
        if (ImGui.Combo("Waveform", ref waveIndex, waveNames, waveNames.Length))
        {
            _controls.Waveform = (WaveformType)waveIndex;
        }
    }

    private void DrawControlSection(SortStatisticsSnapshot stats)
    {
        ImGui.TextUnformatted("Controls");

        if (ImGui.Button("Start", new Vector2(90, 28)))
        {
            StartSelectedAlgorithm();
        }

        ImGui.SameLine();
        if (ImGui.Button(stats.IsPaused ? "Resume" : "Pause", new Vector2(90, 28)))
        {
            ToggleCurrentPause();
        }

        ImGui.SameLine();
        if (ImGui.Button("Step", new Vector2(80, 28)))
        {
            StepCurrentOnce();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset", new Vector2(80, 28)))
        {
            ResetCurrentToSource();
            ResetViewTrackers();
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop", new Vector2(80, 28)))
        {
            StopCurrentEngine();
            TryCaptureReplayAfterCompletion(force: true);
            _statusText = "Stopped.";
        }

        var record = _recordNextRun;
        if (ImGui.Checkbox("Record next run", ref record))
        {
            _recordNextRun = record;
        }

        var replayMode = _replayMode;
        var replayAvailable = IsBarsFamilyMode(_visualizationMode);
        if (!replayAvailable)
        {
            replayMode = false;
            _replayMode = false;
        }

        if (!replayAvailable)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox("Replay mode", ref replayMode))
        {
            _replayMode = replayMode;
            _controls.PlaybackRate = _replayMode ? _controls.PlaybackRate : 1.0;
        }

        if (!replayAvailable)
        {
            ImGui.EndDisabled();
        }

        ImGui.TextUnformatted("Hotkeys: 1 Bars, 2 Network, 3 External, 4 Graph, 5 String, 6 Spatial, U panel, H HUD, F fullscreen");
    }

    private void DrawComparisonSection()
    {
        ImGui.TextUnformatted("Comparison (Side-by-Side)");

        var enableComparison = _comparisonMode;
        if (ImGui.Checkbox("Enable comparison mode", ref enableComparison))
        {
            _comparisonMode = enableComparison;
            if (_comparisonMode)
            {
                SetVisualizationMode(VisualizationMode.Bars);
                _replayMode = false;
                _controls.PlaybackRate = 1.0;
            }
            else
            {
                _comparisonSimulation.Stop();
            }
        }

        if (_visualizationMode != VisualizationMode.Bars)
        {
            ImGui.TextDisabled("Comparison mode renders in Bars view only.");
            return;
        }

        var barsMetas = _registry.All
            .Where(static meta =>
                meta.Status == AlgorithmImplementationStatus.A
                && (meta.SupportedViews & SupportedViews.Bars) != 0
                && meta.Factory is not null)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (barsMetas.Length == 0)
        {
            ImGui.TextDisabled("No Bars-compatible algorithms available.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_comparisonRightAlgorithmId)
            || barsMetas.All(meta => !string.Equals(meta.Id, _comparisonRightAlgorithmId, StringComparison.OrdinalIgnoreCase)))
        {
            _comparisonRightAlgorithmId = barsMetas[0].Id;
        }

        ImGui.TextUnformatted($"Left: {_selectedAlgorithmId}");
        var rightIndex = Array.FindIndex(barsMetas, meta => string.Equals(meta.Id, _comparisonRightAlgorithmId, StringComparison.OrdinalIgnoreCase));
        if (rightIndex < 0)
        {
            rightIndex = 0;
        }

        var rightNames = barsMetas.Select(static meta => meta.Name).ToArray();
        if (ImGui.Combo("Right algorithm", ref rightIndex, rightNames, rightNames.Length))
        {
            _comparisonRightAlgorithmId = barsMetas[Math.Clamp(rightIndex, 0, barsMetas.Length - 1)].Id;
        }

        ImGui.Checkbox("Mix right-side audio", ref _comparisonAudioBoth);

        if (ImGui.Button("Start Both", new Vector2(110, 26)))
        {
            StartComparison(left: true, right: true, regenerateWithSameSeed: true);
        }

        ImGui.SameLine();
        if (ImGui.Button("Start Left", new Vector2(100, 26)))
        {
            StartComparison(left: true, right: false, regenerateWithSameSeed: false);
        }

        ImGui.SameLine();
        if (ImGui.Button("Start Right", new Vector2(100, 26)))
        {
            StartComparison(left: false, right: true, regenerateWithSameSeed: false);
        }

        var leftStats = _simulation.GetStatisticsSnapshot();
        var rightStats = _comparisonSimulation.GetStatisticsSnapshot();

        if (ImGui.Button(leftStats.IsPaused ? "Resume L" : "Pause L", new Vector2(100, 24)))
        {
            _simulation.TogglePause();
        }

        ImGui.SameLine();
        if (ImGui.Button(rightStats.IsPaused ? "Resume R" : "Pause R", new Vector2(100, 24)))
        {
            _comparisonSimulation.TogglePause();
        }

        ImGui.SameLine();
        if (ImGui.Button("Step L", new Vector2(80, 24)))
        {
            _simulation.StepOnce();
        }

        ImGui.SameLine();
        if (ImGui.Button("Step R", new Vector2(80, 24)))
        {
            _comparisonSimulation.StepOnce();
        }

        if (ImGui.Button("Reset Both", new Vector2(120, 24)))
        {
            _simulation.ResetToSource();
            _comparisonSimulation.ResetToSource();
            _comparisonCapturePending = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Capture Result", new Vector2(130, 24)))
        {
            CaptureComparisonSnapshot(force: true);
        }

        ImGui.TextUnformatted($"L  cmp {leftStats.Comparisons:N0} | swp {leftStats.Swaps:N0} | wr {leftStats.Writes:N0} | t {leftStats.ElapsedMs:0.0} ms");
        ImGui.TextUnformatted($"R  cmp {rightStats.Comparisons:N0} | swp {rightStats.Swaps:N0} | wr {rightStats.Writes:N0} | t {rightStats.ElapsedMs:0.0} ms");
    }

    private void DrawReplaySection()
    {
        ImGui.TextUnformatted("Replay v1");

        if (!IsBarsFamilyMode(_visualizationMode))
        {
            ImGui.TextDisabled("Replay is available in Bars/Network/External/Graph modes.");
            return;
        }

        if (_comparisonMode)
        {
            ImGui.TextDisabled("Disable comparison mode to use replay.");
            return;
        }

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("Replay path", ref _replayPathInput, 512);

        if (ImGui.Button("Save Replay", new Vector2(120, 28)))
        {
            SaveReplay();
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Replay", new Vector2(120, 28)))
        {
            LoadReplay();
        }

        ImGui.SameLine();
        if (ImGui.Button("Play Replay", new Vector2(120, 28)))
        {
            StartReplayFromCursor();
        }

        if (_loadedReplay is not null)
        {
            var maxEvent = Math.Max(0, _loadedReplay.Events.Length);
            ImGui.SliderInt("Timeline (event)", ref _replayEventCursor, 0, maxEvent);

            var rate = (float)_controls.PlaybackRate;
            if (ImGui.SliderFloat("Replay speed", ref rate, 0.25f, 4.0f, "%.2fx"))
            {
                _controls.PlaybackRate = rate;
            }

            ImGui.TextUnformatted($"Loaded: {_loadedReplay.Events.Length:N0} events, {_loadedReplay.Keyframes.Length:N0} keyframes");
            ImGui.TextUnformatted($"Algorithm: {_loadedReplay.AlgorithmId}, Seed: {_loadedReplay.Seed}");
        }

        if (_lastReplay is not null)
        {
            ImGui.TextUnformatted($"Last captured: {_lastReplay.Events.Length:N0} events");
        }
    }

    private void DrawAnalysisSection()
    {
        ImGui.TextUnformatted("Comparison & Analysis");

        var growthSeed = _growthSeed;
        if (ImGui.InputInt("Growth seed", ref growthSeed))
        {
            _growthSeed = growthSeed;
        }

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("Growth N list", ref _growthSizeSeries, 256);

        ImGui.Checkbox("Growth headless", ref _growthHeadless);

        var growthRunning = _growthTask is not null;
        if (!growthRunning)
        {
            if (ImGui.Button("Run Growth Analysis", new Vector2(180, 26)))
            {
                StartGrowthAnalysis();
            }
        }
        else
        {
            if (ImGui.Button("Cancel Growth Analysis", new Vector2(180, 26)))
            {
                _growthCts?.Cancel();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Growth CSV", new Vector2(140, 26)))
        {
            ExportGrowthCsv();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Growth JSON", new Vector2(145, 26)))
        {
            ExportGrowthJson();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Comparison CSV", new Vector2(165, 26)))
        {
            ExportComparisonCsv();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Comparison JSON", new Vector2(170, 26)))
        {
            ExportComparisonJson();
        }

        ImGui.TextWrapped(_growthStatusText);
        if (!string.IsNullOrWhiteSpace(_lastGrowthExportPath))
        {
            ImGui.TextWrapped($"Last growth export: {_lastGrowthExportPath}");
        }

        if (!string.IsNullOrWhiteSpace(_lastComparisonExportPath))
        {
            ImGui.TextWrapped($"Last comparison export: {_lastComparisonExportPath}");
        }

        DrawGrowthPlots();
        DrawStatisticalSummaryTable();
    }

    private void DrawGrowthPlots()
    {
        if (_lastGrowthSuite is null || _lastGrowthSuite.Results.Count == 0)
        {
            return;
        }

        ImGui.SeparatorText("Growth Curves");
        DrawGrowthMetricPlot("Elapsed (ms)", static row => row.ElapsedMs, PackColor(56, 166, 255, 255));
        DrawGrowthMetricPlot("Comparisons", static row => row.Comparisons, PackColor(240, 240, 240, 255));
        DrawGrowthMetricPlot("Swaps", static row => row.Swaps, PackColor(255, 168, 66, 255));
    }

    private void DrawGrowthMetricPlot(string title, Func<GrowthBenchmarkPointResult, double> metricSelector, uint axisColor)
    {
        var suite = _lastGrowthSuite;
        if (suite is null)
        {
            return;
        }

        var grouped = suite.Results
            .Where(static row => row.Completed && string.IsNullOrWhiteSpace(row.Error))
            .GroupBy(static row => row.AlgorithmName, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.OrderBy(static row => row.Size).ToArray())
            .Where(static rows => rows.Length >= 2)
            .ToArray();

        if (grouped.Length == 0)
        {
            ImGui.TextDisabled($"No completed points for {title}.");
            return;
        }

        ImGui.TextUnformatted(title);
        var size = new Vector2(-1, 150);
        ImGui.BeginChild($"growth-{title}", size, ImGuiChildFlags.Borders);

        var cursor = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var width = Math.Max(120.0f, avail.X - 8.0f);
        var height = Math.Max(90.0f, avail.Y - 8.0f);
        var pMin = cursor + new Vector2(4.0f, 4.0f);
        var pMax = pMin + new Vector2(width, height);

        var allRows = grouped.SelectMany(static rows => rows).ToArray();
        var minN = allRows.Min(static row => row.Size);
        var maxN = allRows.Max(static row => row.Size);
        var maxMetric = Math.Max(1.0, allRows.Max(metricSelector));

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRect(pMin, pMax, axisColor, 0.0f, ImDrawFlags.None, 1.0f);

        for (var i = 1; i <= 4; i++)
        {
            var y = pMin.Y + height * (i / 5.0f);
            drawList.AddLine(new Vector2(pMin.X, y), new Vector2(pMax.X, y), PackColor(120, 120, 120, 50), 1.0f);
        }

        for (var index = 0; index < grouped.Length; index++)
        {
            var rows = grouped[index];
            var color = PlotColor(index);

            for (var i = 1; i < rows.Length; i++)
            {
                var p0 = ToPlotPoint(rows[i - 1], metricSelector, pMin, width, height, minN, maxN, maxMetric);
                var p1 = ToPlotPoint(rows[i], metricSelector, pMin, width, height, minN, maxN, maxMetric);
                drawList.AddLine(p0, p1, color, 1.5f);
            }
        }

        ImGui.Dummy(new Vector2(width + 8.0f, height + 8.0f));
        ImGui.EndChild();
    }

    private static Vector2 ToPlotPoint(
        GrowthBenchmarkPointResult row,
        Func<GrowthBenchmarkPointResult, double> metricSelector,
        Vector2 origin,
        float width,
        float height,
        int minN,
        int maxN,
        double maxMetric)
    {
        var xNorm = maxN == minN ? 0.0f : (row.Size - minN) / (float)(maxN - minN);
        var yNorm = (float)Math.Clamp(metricSelector(row) / maxMetric, 0.0, 1.0);
        var x = origin.X + xNorm * width;
        var y = origin.Y + (1.0f - yNorm) * height;
        return new Vector2(x, y);
    }

    private static uint PlotColor(int index)
    {
        var palette = new[]
        {
            PackColor(66, 156, 255, 255),
            PackColor(255, 166, 64, 255),
            PackColor(104, 214, 116, 255),
            PackColor(255, 96, 96, 255),
            PackColor(196, 132, 255, 255),
            PackColor(255, 236, 98, 255)
        };

        return palette[index % palette.Length];
    }

    private void DrawStatisticalSummaryTable()
    {
        var rows = _registry.All
            .Where(static meta =>
                meta.Status == AlgorithmImplementationStatus.A
                && (meta.SupportedViews & SupportedViews.Bars) != 0)
            .OrderBy(static meta => meta.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rows.Length == 0)
        {
            return;
        }

        var metricLookup = BuildActualMetricLookup();

        ImGui.SeparatorText("Statistical Summary");
        if (!ImGui.BeginTable("analysis-summary", 8, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 220)))
        {
            return;
        }

        ImGui.TableSetupColumn("Algorithm");
        ImGui.TableSetupColumn("Stable");
        ImGui.TableSetupColumn("Avg");
        ImGui.TableSetupColumn("Worst");
        ImGui.TableSetupColumn("Mem(est)");
        ImGui.TableSetupColumn("Avg ms");
        ImGui.TableSetupColumn("Avg cmp");
        ImGui.TableSetupColumn("Avg swp/wr");
        ImGui.TableHeadersRow();

        foreach (var meta in rows)
        {
            metricLookup.TryGetValue(meta.Id, out var metrics);
            metrics ??= new List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>();

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(meta.Name);

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(meta.Stable.HasValue ? (meta.Stable.Value ? "Yes" : "No") : "-");

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(meta.AverageComplexity);

            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted(meta.WorstComplexity);

            ImGui.TableSetColumnIndex(4);
            ImGui.TextUnformatted(EstimateMemoryComplexity(meta));

            ImGui.TableSetColumnIndex(5);
            ImGui.TextUnformatted(metrics.Count == 0 ? "-" : $"{metrics.Average(static m => m.ElapsedMs):0.00}");

            ImGui.TableSetColumnIndex(6);
            ImGui.TextUnformatted(metrics.Count == 0 ? "-" : $"{metrics.Average(static m => m.Comparisons):0}");

            ImGui.TableSetColumnIndex(7);
            ImGui.TextUnformatted(metrics.Count == 0
                ? "-"
                : $"{metrics.Average(static m => m.Swaps):0} / {metrics.Average(static m => m.Writes):0}");
        }

        ImGui.EndTable();
    }

    private Dictionary<string, List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>> BuildActualMetricLookup()
    {
        var lookup = new Dictionary<string, List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>>(StringComparer.OrdinalIgnoreCase);

        if (_lastBenchmarkSuite is not null)
        {
            foreach (var row in _lastBenchmarkSuite.Results.Where(static row => row.Completed && string.IsNullOrWhiteSpace(row.Error)))
            {
                if (!lookup.TryGetValue(row.AlgorithmId, out var list))
                {
                    list = new List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>();
                    lookup[row.AlgorithmId] = list;
                }

                list.Add((row.ElapsedMs, row.Comparisons, row.Swaps, row.Writes));
            }
        }

        if (_lastGrowthSuite is not null)
        {
            foreach (var row in _lastGrowthSuite.Results.Where(static row => row.Completed && string.IsNullOrWhiteSpace(row.Error)))
            {
                if (!lookup.TryGetValue(row.AlgorithmId, out var list))
                {
                    list = new List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>();
                    lookup[row.AlgorithmId] = list;
                }

                list.Add((row.ElapsedMs, row.Comparisons, row.Swaps, row.Writes));
            }
        }

        foreach (var record in _comparisonHistory)
        {
            if (!lookup.TryGetValue(record.LeftAlgorithmId, out var leftList))
            {
                leftList = new List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>();
                lookup[record.LeftAlgorithmId] = leftList;
            }

            leftList.Add((record.Left.ElapsedMs, record.Left.Comparisons, record.Left.Swaps, record.Left.Writes));

            if (!lookup.TryGetValue(record.RightAlgorithmId, out var rightList))
            {
                rightList = new List<(double ElapsedMs, long Comparisons, long Swaps, long Writes)>();
                lookup[record.RightAlgorithmId] = rightList;
            }

            rightList.Add((record.Right.ElapsedMs, record.Right.Comparisons, record.Right.Swaps, record.Right.Writes));
        }

        return lookup;
    }

    private static string EstimateMemoryComplexity(AlgorithmMetadata meta)
    {
        return ComplexityMapService.EstimateMemoryComplexity(meta);
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    private void DrawPersistenceSection()
    {
        ImGui.TextUnformatted("Persistence");

        if (ImGui.Button("Save settings", new Vector2(120, 28)))
        {
            SaveSettings(_settingsPath, "settings");
        }

        ImGui.SameLine();
        if (ImGui.Button("Load settings", new Vector2(120, 28)))
        {
            LoadSettings(_settingsPath, "settings");
        }

        ImGui.SameLine();
        if (ImGui.Button("Save preset", new Vector2(120, 28)))
        {
            SaveSettings(_presetPath, "preset");
        }

        ImGui.SameLine();
        if (ImGui.Button("Load preset", new Vector2(120, 28)))
        {
            LoadSettings(_presetPath, "preset");
        }

        if (ImGui.Button("Run A validation (N=2048)", new Vector2(250, 28)))
        {
            var errors = AlgorithmValidator.ValidateImplementedAlgorithms(_registry, 2048);
            _statusText = errors.Count == 0
                ? "Validation passed for all A algorithms."
                : $"Validation failed: {errors.Count} issues. First: {errors[0]}";
        }

        ImGui.SeparatorText("Benchmark");
        var benchmarkSeed = _benchmarkSeed;
        if (ImGui.InputInt("Benchmark seed", ref benchmarkSeed))
        {
            _benchmarkSeed = benchmarkSeed;
        }

        var useFavorites = _benchmarkUseFavorites;
        if (ImGui.Checkbox("Use favorites as set", ref useFavorites))
        {
            _benchmarkUseFavorites = useFavorites;
        }

        var headless = _benchmarkHeadless;
        if (ImGui.Checkbox("Headless benchmark", ref headless))
        {
            _benchmarkHeadless = headless;
        }

        var benchmarkRunning = _benchmarkTask is not null;
        if (!benchmarkRunning)
        {
            if (ImGui.Button("Run Benchmark", new Vector2(160, 28)))
            {
                StartBenchmark();
            }
        }
        else
        {
            if (ImGui.Button("Cancel Benchmark", new Vector2(160, 28)))
            {
                _benchmarkCts?.Cancel();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Export Benchmark CSV", new Vector2(180, 28)))
        {
            ExportBenchmarkCsv();
        }

        ImGui.TextWrapped(_benchmarkStatusText);
        if (_lastBenchmarkSuite is not null)
        {
            ImGui.TextUnformatted($"Rows: {_lastBenchmarkSuite.Results.Count}, Warnings: {_lastBenchmarkSuite.Warnings.Count}");
            if (!string.IsNullOrWhiteSpace(_lastBenchmarkCsvPath))
            {
                ImGui.TextWrapped($"Last CSV: {_lastBenchmarkCsvPath}");
            }
        }

        ImGui.SeparatorText("Registry Health");
        ImGui.TextWrapped(_registrySummaryText);
        if (_registryMetadataIssues.Count > 0)
        {
            ImGui.BeginChild("registry-issues", new Vector2(0, 110), ImGuiChildFlags.Borders);
            foreach (var issue in _registryMetadataIssues.Take(16))
            {
                ImGui.BulletText(issue);
            }
            ImGui.EndChild();
        }

        ImGui.TextWrapped(_statusText);
    }

    private IEnumerable<AlgorithmMetadata> GetFilteredAlgorithms()
    {
        var hasQuery = !string.IsNullOrWhiteSpace(_algorithmSearch);

        return _registry.All.Where(meta =>
        {
            if (_showFavoritesOnly && !_favorites.Contains(meta.Id))
            {
                return false;
            }

            if (!hasQuery)
            {
                return true;
            }

            return meta.Name.Contains(_algorithmSearch, StringComparison.OrdinalIgnoreCase)
                || meta.Category.Contains(_algorithmSearch, StringComparison.OrdinalIgnoreCase)
                || meta.Id.Contains(_algorithmSearch, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void StartSelectedAlgorithm()
    {
        if (_replayMode)
        {
            if (!IsBarsFamilyMode(_visualizationMode))
            {
                _statusText = "Replay mode is available only for Bars-family views.";
                return;
            }

            StartReplayFromCursor();
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedAlgorithmId))
        {
            return;
        }

        if (!_registry.TryGet(_selectedAlgorithmId, out var metadata))
        {
            _statusText = "Selected algorithm metadata is missing.";
            return;
        }

        EnsureModeCompatibility(metadata, setStatus: false);
        StopCurrentEngine();
        ResetViewTrackers();
        _controls.PlaybackRate = 1.0;

        if (_visualizationMode == VisualizationMode.String)
        {
            if (!_registry.TryCreateString(_selectedAlgorithmId, out _, out var stringAlgorithm) || stringAlgorithm is null)
            {
                _statusText = "Selected algorithm is not implemented for String view.";
                return;
            }

            var stringOptions = new StringSortOptions(_stringAlphabet, EmitExtendedEvents: true);
            _pendingAutoReplayCapture = false;
            _stringSimulation.Start(stringAlgorithm, stringOptions);
            _statusText = $"Started {_selectedAlgorithmId} (String).";
            return;
        }

        if (_visualizationMode == VisualizationMode.Spatial)
        {
            if (!_registry.TryCreateSpatial(_selectedAlgorithmId, out _, out var spatialAlgorithm) || spatialAlgorithm is null)
            {
                _statusText = "Selected algorithm is not implemented for Spatial view.";
                return;
            }

            var spatialOptions = new SpatialSortOptions(EmitExtendedEvents: true);
            _pendingAutoReplayCapture = false;
            _spatialSimulation.Start(spatialAlgorithm, spatialOptions);
            _statusText = $"Started {_selectedAlgorithmId} (Spatial).";
            return;
        }

        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            StartComparison(left: true, right: true, regenerateWithSameSeed: true);
            return;
        }

        if (_visualizationMode == VisualizationMode.Bars && _arraySize > 1_000_000 && !_controls.GpuAccelerationEnabled)
        {
            _statusText = "Large N (>1,000,000) is recommended with GPU acceleration enabled.";
        }

        if (!_registry.TryCreate(_selectedAlgorithmId, out _, out var algorithm) || algorithm is null)
        {
            _statusText = "Selected algorithm is not implemented (B).";
            return;
        }

        if (TryStartGpuAcceleratedAlgorithm(metadata))
        {
            return;
        }

        PrepareAlgorithmSpecificState(algorithm);
        _pendingAutoReplayCapture = _recordNextRun;
        _simulation.Start(algorithm, _selectedAlgorithmId, _recordNextRun, _controls.Parallelism);
        _statusText = $"Started {_selectedAlgorithmId}.";
    }

    private bool TryStartGpuAcceleratedAlgorithm(AlgorithmMetadata metadata)
    {
        if (!_controls.GpuAccelerationEnabled)
        {
            return false;
        }

        if (_comparisonMode || !IsBarsFamilyMode(_visualizationMode))
        {
            return false;
        }

        var kind = ResolveGpuSortKind(metadata);
        if (kind == GpuSortKind.None)
        {
            return false;
        }

        if (_gpuSortService is null)
        {
            _lastGpuMetrics = GpuExecutionMetrics.Empty with { Kind = kind, Message = "GPU service unavailable." };
            return false;
        }

        var count = _simulation.CopyDataTo(ref _snapshotBuffer, out _);
        if (count <= 0)
        {
            return false;
        }

        var source = new int[count];
        Array.Copy(_snapshotBuffer, source, count);

        _gpuProgress = 0.0;
        var cpuSortMs = 0.0;
        int[]? cpuReference = null;
        if (_controls.CompareCpuGpuTiming)
        {
            cpuReference = source.ToArray();
            var cpuTimer = Stopwatch.StartNew();
            Array.Sort(cpuReference);
            cpuTimer.Stop();
            cpuSortMs = cpuTimer.Elapsed.TotalMilliseconds;
        }

        var success = kind switch
        {
            GpuSortKind.Bitonic => _gpuSortService.TrySortBitonic(source, out var gpuSortedBitonic, out var metricsBitonic, p => _gpuProgress = p)
                ? StartGpuReplay(metricsBitonic with { CpuSortMs = cpuSortMs }, gpuSortedBitonic, metadata, cpuReference)
                : HandleGpuFailure(metricsBitonic with { CpuSortMs = cpuSortMs }),
            GpuSortKind.RadixLsd => _gpuSortService.TrySortRadixLsd(source, out var gpuSortedRadix, out var metricsRadix, p => _gpuProgress = p)
                ? StartGpuReplay(metricsRadix with { CpuSortMs = cpuSortMs }, gpuSortedRadix, metadata, cpuReference)
                : HandleGpuFailure(metricsRadix with { CpuSortMs = cpuSortMs }),
            _ => false
        };

        if (!success)
        {
            _gpuProgress = 0.0;
        }

        return success;
    }

    private bool StartGpuReplay(GpuExecutionMetrics metrics, int[] gpuSorted, AlgorithmMetadata metadata, int[]? cpuReference)
    {
        if (cpuReference is not null && !cpuReference.AsSpan().SequenceEqual(gpuSorted))
        {
            _lastGpuMetrics = metrics with { UsedGpu = false, Message = "GPU output mismatch vs CPU reference (fallback)." };
            _statusText = $"GPU verification failed for {metadata.Name}; fallback to CPU path.";
            return false;
        }

        _lastGpuMetrics = metrics with { Progress01 = 1.0, Message = "ok" };
        _gpuProgress = 1.0;

        ResetViewTrackers();
        _pendingAutoReplayCapture = false;
        _simulation.Start(new PrecomputedWriteSortAlgorithm(gpuSorted), $"{metadata.Id}:gpu", record: false, parallelism: 1);
        _statusText = $"{metadata.Name} GPU completed: dispatch {_lastGpuMetrics.DispatchMs:0.00} ms, upload {_lastGpuMetrics.UploadMs:0.00} ms.";
        return true;
    }

    private bool HandleGpuFailure(GpuExecutionMetrics metrics)
    {
        _lastGpuMetrics = metrics with { UsedGpu = false };
        return false;
    }

    private static GpuSortKind ResolveGpuSortKind(AlgorithmMetadata metadata)
    {
        return metadata.Name switch
        {
            "GPU Bitonic Sort" => GpuSortKind.Bitonic,
            "GPU Radix LSD Sort" => GpuSortKind.RadixLsd,
            _ => GpuSortKind.None
        };
    }

    private void StartReplayFromCursor()
    {
        if (!IsBarsFamilyMode(_visualizationMode))
        {
            _statusText = "Replay playback requires Bars-family view.";
            return;
        }

        if (_loadedReplay is null)
        {
            _statusText = "No replay loaded.";
            return;
        }

        _pendingAutoReplayCapture = false;
        _comparisonMode = false;
        _comparisonCapturePending = false;

        var startEvent = Math.Clamp(_replayEventCursor, 0, _loadedReplay.Events.Length);
        var state = ReplayUtilsV1.ReconstructStateAtEvent(_loadedReplay, startEvent);

        StopAllEngines();
        _simulation.LoadData(state);
        ResetViewTrackers();
        _simulation.Start(new ReplaySortAlgorithm(_loadedReplay.Events, startEvent), "replay-v1", record: false, parallelism: 1);
        _statusText = $"Replay started at event {startEvent:N0}.";
    }

    private void RegenerateData()
    {
        _currentSeed = Random.Shared.Next();
        StopAllEngines();
        ResetViewTrackers();
        _gpuProgress = 0.0;
        _lastGpuMetrics = GpuExecutionMetrics.Empty;

        if (_visualizationMode == VisualizationMode.String)
        {
            var generatedStrings = StringDataGenerator.Generate(
                _stringCount,
                _stringLength,
                _stringAlphabet,
                _stringDistribution,
                _currentSeed);
            _stringSimulation.LoadData(generatedStrings);
            _pendingAutoReplayCapture = false;
            _statusText = $"Generated String N={_stringCount:N0}, L={_stringLength}, alphabet={_stringAlphabet}, dist={_stringDistribution}, seed={_currentSeed}.";
            return;
        }

        if (_visualizationMode == VisualizationMode.Spatial)
        {
            var generatedPoints = SpatialDataGenerator.Generate(_spatialCount, _spatialDistribution, _currentSeed);
            _spatialSimulation.LoadData(generatedPoints);
            _pendingAutoReplayCapture = false;
            _statusText = $"Generated Spatial N={_spatialCount:N0}, dist={_spatialDistribution}, seed={_currentSeed}.";
            return;
        }

        var generated = DataGenerator.Generate(_arraySize, _distribution, _currentSeed);
        _simulation.LoadData(generated);
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            _comparisonSimulation.LoadData(generated.ToArray());
        }
        ResetViewTrackers();
        _pendingAutoReplayCapture = false;
        _statusText = $"Generated N={_arraySize:N0}, distribution={_distribution}, seed={_currentSeed}.";
    }

    private void StartComparison(bool left, bool right, bool regenerateWithSameSeed)
    {
        if (_visualizationMode != VisualizationMode.Bars)
        {
            _statusText = "Comparison mode requires Bars view.";
            return;
        }

        if (!left && !right)
        {
            return;
        }

        if (!_registry.TryGet(_selectedAlgorithmId, out var leftMeta)
            || leftMeta.Factory is null
            || leftMeta.Status != AlgorithmImplementationStatus.A
            || (leftMeta.SupportedViews & SupportedViews.Bars) == 0)
        {
            _statusText = "Left algorithm is not Bars-compatible A implementation.";
            return;
        }

        if (!_registry.TryGet(_comparisonRightAlgorithmId, out var rightMeta)
            || rightMeta.Factory is null
            || rightMeta.Status != AlgorithmImplementationStatus.A
            || (rightMeta.SupportedViews & SupportedViews.Bars) == 0)
        {
            _statusText = "Right algorithm is not Bars-compatible A implementation.";
            return;
        }

        _comparisonMode = true;
        _replayMode = false;
        _controls.PlaybackRate = 1.0;
        _pendingAutoReplayCapture = false;

        if (regenerateWithSameSeed || !_simulation.HasData || !_comparisonSimulation.HasData)
        {
            _currentSeed = Random.Shared.Next();
            var sharedData = DataGenerator.Generate(_arraySize, _distribution, _currentSeed);
            _simulation.LoadData(sharedData);
            _comparisonSimulation.LoadData(sharedData.ToArray());
        }

        if (left)
        {
            var leftAlgo = leftMeta.Factory.Invoke();
            PrepareAlgorithmSpecificState(leftAlgo);
            _simulation.Start(leftAlgo, leftMeta.Id, record: false, parallelism: _controls.Parallelism);
        }

        if (right)
        {
            var rightAlgo = rightMeta.Factory.Invoke();
            _comparisonSimulation.Start(rightAlgo, rightMeta.Id, record: false, parallelism: _controls.Parallelism);
        }

        _comparisonCapturePending = left && right;
        _statusText = $"Comparison started: L={leftMeta.Name}, R={rightMeta.Name}, N={_arraySize:N0}, seed={_currentSeed}.";
    }

    private void TryCaptureComparisonCompletion()
    {
        if (!_comparisonCapturePending)
        {
            return;
        }

        CaptureComparisonSnapshot(force: false);
    }

    private void CaptureComparisonSnapshot(bool force)
    {
        if (!_comparisonMode)
        {
            return;
        }

        var leftStats = _simulation.GetStatisticsSnapshot();
        var rightStats = _comparisonSimulation.GetStatisticsSnapshot();

        if (!force && !(leftStats.IsCompleted && rightStats.IsCompleted))
        {
            return;
        }

        if (!_registry.TryGet(_selectedAlgorithmId, out var leftMeta))
        {
            return;
        }

        if (!_registry.TryGet(_comparisonRightAlgorithmId, out var rightMeta))
        {
            return;
        }

        _comparisonHistory.Add(new ComparisonAnalysisRecord
        {
            CreatedAtUtc = DateTime.UtcNow,
            LeftAlgorithmId = leftMeta.Id,
            LeftAlgorithmName = leftMeta.Name,
            RightAlgorithmId = rightMeta.Id,
            RightAlgorithmName = rightMeta.Name,
            Size = _arraySize,
            Distribution = _distribution,
            Seed = _currentSeed,
            Left = new ComparisonSideSnapshot
            {
                Comparisons = leftStats.Comparisons,
                Swaps = leftStats.Swaps,
                Writes = leftStats.Writes,
                ProcessedEvents = leftStats.ProcessedEvents,
                ElapsedMs = leftStats.ElapsedMs,
                Completed = leftStats.IsCompleted
            },
            Right = new ComparisonSideSnapshot
            {
                Comparisons = rightStats.Comparisons,
                Swaps = rightStats.Swaps,
                Writes = rightStats.Writes,
                ProcessedEvents = rightStats.ProcessedEvents,
                ElapsedMs = rightStats.ElapsedMs,
                Completed = rightStats.IsCompleted
            }
        });

        _comparisonCapturePending = false;
        _growthStatusText = $"Comparison captured ({_comparisonHistory.Count:N0} record(s)).";
    }

    private void SaveReplay()
    {
        TryCaptureReplayAfterCompletion(force: true);
        if (_lastReplay is null)
        {
            _statusText = "No replay captured.";
            return;
        }

        var path = string.IsNullOrWhiteSpace(_replayPathInput) ? _defaultReplayPath : _replayPathInput;
        ReplayWriterV1.Save(path, _lastReplay);
        _statusText = $"Replay saved: {path}";
    }

    private void LoadReplay()
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(_replayPathInput) ? _defaultReplayPath : _replayPathInput;
            _loadedReplay = ReplayReaderV1.Load(path);
            _replayEventCursor = 0;
            _replayMode = true;
            if (!IsBarsFamilyMode(_visualizationMode))
            {
                SetVisualizationMode(VisualizationMode.Bars);
            }
            _controls.PlaybackRate = 1.0;
            _statusText = $"Replay loaded: {path}";
        }
        catch (Exception ex)
        {
            _statusText = $"Replay load failed: {ex.Message}";
        }
    }

    private void TryCaptureReplayAfterCompletion(bool force = false)
    {
        if (!IsBarsFamilyMode(_visualizationMode))
        {
            return;
        }

        if (!force && !_pendingAutoReplayCapture)
        {
            return;
        }

        var stats = _simulation.GetStatisticsSnapshot();
        if (!force && !stats.IsCompleted)
        {
            return;
        }

        if (!_simulation.TryGetRecordedRun(out var algorithmId, out var initialData, out var events, out _))
        {
            return;
        }

        if (events.Length == 0 || initialData.Length == 0)
        {
            return;
        }

        var maxValue = initialData.Length == 0 ? 1 : Math.Max(1, initialData.Max());
        _lastReplay = ReplayUtilsV1.BuildFromRecorded(
            algorithmId,
            seed: _currentSeed,
            distribution: _distribution,
            maxValue: maxValue,
            initialData: initialData,
            events: events,
            keyframeIntervalEvents: 50_000);

        _pendingAutoReplayCapture = false;
    }

    private void SaveSettings(string path, string label)
    {
        try
        {
            var settings = BuildSettingsSnapshot();
            SettingsStorage.Save(path, settings);
            _statusText = $"Saved {label}: {path}";
        }
        catch (Exception ex)
        {
            _statusText = $"Save {label} failed: {ex.Message}";
        }
    }

    private void LoadSettings(string path, string label)
    {
        try
        {
            var settings = SettingsStorage.Load(path);
            if (settings is null)
            {
                return;
            }

            ApplySettings(settings);
            _statusText = $"Loaded {label}: {path}";
        }
        catch (Exception ex)
        {
            _statusText = $"Load {label} failed: {ex.Message}";
        }
    }

    private VisualizerSettings BuildSettingsSnapshot()
    {
        return new VisualizerSettings
        {
            SelectedAlgorithmId = _selectedAlgorithmId,
            Favorites = _favorites.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            ArraySize = _arraySize,
            Distribution = _distribution,
            StringCount = _stringCount,
            StringLength = _stringLength,
            StringAlphabet = _stringAlphabet,
            StringDistribution = _stringDistribution,
            SpatialCount = _spatialCount,
            SpatialDistribution = _spatialDistribution,
            SpatialShowOrder = _spatialShowOrder,
            SpatialShowGrid = _spatialShowGrid,
            VisualizationMode = _visualizationMode,
            GraphEdgeDensity = _graphEdgeDensity,
            ShowSidePanel = _showSidePanel,
            ShowHudOverlay = _showHudOverlay,
            ShowDiagnostics = _showDiagnostics,
            DifficultyOverrides = _difficultyOverrides
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static pair => pair.Key, static pair => Math.Clamp(pair.Value, 1, 5), StringComparer.OrdinalIgnoreCase),
            Controls = new RuntimeControlsDto
            {
                SpeedMode = _controls.SpeedMode,
                EventsPerSecond = _controls.EventsPerSecond,
                DelayMs = _controls.DelayMs,
                Parallelism = _controls.Parallelism,
                VisualDetail = _controls.VisualDetail,
                AudioDetail = _controls.AudioDetail,
                LinkDetails = _controls.LinkDetails,
                VisualEnabled = _controls.VisualEnabled,
                AudioEnabled = _controls.AudioEnabled,
                MaxAudioEventsPerFrame = _controls.MaxAudioEventsPerFrame,
                MaxVisualEventsPerFrame = _controls.MaxVisualEventsPerFrame,
                AudioVolume = _controls.AudioVolume,
                AudioMinFrequency = _controls.AudioMinFrequency,
                AudioMaxFrequency = _controls.AudioMaxFrequency,
                Waveform = _controls.Waveform,
                SonificationProfile = _controls.SonificationProfile,
                AudioNormalizationEnabled = _controls.AudioNormalizationEnabled,
                AudioLimiterEnabled = _controls.AudioLimiterEnabled,
                AudioStereoPanEnabled = _controls.AudioStereoPanEnabled,
                AudioSpatialPanByX = _controls.AudioSpatialPanByX,
                ToneDurationMs = _controls.ToneDurationMs,
                AttackMs = _controls.AttackMs,
                DecayMs = _controls.DecayMs,
                ReleaseMs = _controls.ReleaseMs,
                PolyphonyCap = _controls.PolyphonyCap,
                OverlayIntensity = _controls.OverlayIntensity,
                ShowRangesOverlay = _controls.ShowRangesOverlay,
                ShowPivotOverlay = _controls.ShowPivotOverlay,
                ShowBucketsOverlay = _controls.ShowBucketsOverlay,
                ShowMemoryHeatmap = _controls.ShowMemoryHeatmap,
                NormalizeHeatmapByMax = _controls.NormalizeHeatmapByMax,
                CacheLineSize = _controls.CacheLineSize,
                GpuAccelerationEnabled = _controls.GpuAccelerationEnabled,
                CompareCpuGpuTiming = _controls.CompareCpuGpuTiming,
                ShowGpuThreadOverlay = _controls.ShowGpuThreadOverlay,
                ShowGpuBitonicStageGrid = _controls.ShowGpuBitonicStageGrid,
                PlaybackRate = _controls.PlaybackRate
            }
        };
    }

    private void ApplySettings(VisualizerSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.SelectedAlgorithmId) && _registry.TryGet(settings.SelectedAlgorithmId, out _))
        {
            _selectedAlgorithmId = settings.SelectedAlgorithmId;
        }

        _favorites.Clear();
        foreach (var item in settings.Favorites)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                _favorites.Add(item);
            }
        }

        _arraySize = Math.Clamp(settings.ArraySize, 8, 5000000);
        _distribution = settings.Distribution;
        _stringCount = Math.Clamp(settings.StringCount, 8, 5000);
        _stringLength = Math.Clamp(settings.StringLength, 2, 64);
        _stringAlphabet = settings.StringAlphabet;
        _stringDistribution = settings.StringDistribution;
        _spatialCount = Math.Clamp(settings.SpatialCount, 32, 200000);
        _spatialDistribution = settings.SpatialDistribution;
        _spatialShowOrder = settings.SpatialShowOrder;
        _spatialShowGrid = settings.SpatialShowGrid;
        _visualizationMode = Enum.IsDefined(settings.VisualizationMode) ? settings.VisualizationMode : VisualizationMode.Bars;
        _graphEdgeDensity = Math.Clamp(settings.GraphEdgeDensity, 0.01f, 0.45f);
        _showSidePanel = settings.ShowSidePanel;
        _showHudOverlay = settings.ShowHudOverlay;
        _showDiagnostics = settings.ShowDiagnostics;

        _difficultyOverrides.Clear();
        foreach (var pair in settings.DifficultyOverrides ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
        {
            _difficultyOverrides[pair.Key] = Math.Clamp(pair.Value, 1, 5);
        }

        var c = settings.Controls;
        _controls.SpeedMode = c.SpeedMode;
        _controls.EventsPerSecond = Math.Clamp(c.EventsPerSecond, 1.0, 300000.0);
        _controls.DelayMs = Math.Clamp(c.DelayMs, 0.0, 100.0);
        _controls.Parallelism = Math.Clamp(c.Parallelism, 1, 128);
        _controls.VisualDetail = c.VisualDetail;
        _controls.AudioDetail = c.AudioDetail;
        _controls.LinkDetails = c.LinkDetails;
        _controls.VisualEnabled = c.VisualEnabled;
        _controls.AudioEnabled = c.AudioEnabled;
        _controls.MaxAudioEventsPerFrame = Math.Clamp(c.MaxAudioEventsPerFrame, 1, 5);
        _controls.MaxVisualEventsPerFrame = Math.Clamp(c.MaxVisualEventsPerFrame, 128, _visualEventsBuffer.Length);
        _controls.AudioVolume = Math.Clamp(c.AudioVolume, 0.0f, 1.0f);
        _controls.AudioMinFrequency = Math.Clamp(c.AudioMinFrequency, 40.0f, 800.0f);
        _controls.AudioMaxFrequency = Math.Clamp(c.AudioMaxFrequency, _controls.AudioMinFrequency + 20.0f, 5000.0f);
        _controls.Waveform = c.Waveform;
        _controls.SonificationProfile = c.SonificationProfile;
        _controls.AudioNormalizationEnabled = c.AudioNormalizationEnabled;
        _controls.AudioLimiterEnabled = c.AudioLimiterEnabled;
        _controls.AudioStereoPanEnabled = c.AudioStereoPanEnabled;
        _controls.AudioSpatialPanByX = c.AudioSpatialPanByX;
        _controls.ToneDurationMs = Math.Clamp(c.ToneDurationMs, 20.0f, 50.0f);
        _controls.AttackMs = Math.Clamp(c.AttackMs, 2.0f, 8.0f);
        _controls.DecayMs = Math.Clamp(c.DecayMs, 8.0f, 35.0f);
        _controls.ReleaseMs = Math.Clamp(c.ReleaseMs, 5.0f, 20.0f);
        _controls.PolyphonyCap = Math.Clamp(c.PolyphonyCap, 4, 32);
        _controls.OverlayIntensity = Math.Clamp(c.OverlayIntensity, 0, 100);
        _controls.ShowRangesOverlay = c.ShowRangesOverlay;
        _controls.ShowPivotOverlay = c.ShowPivotOverlay;
        _controls.ShowBucketsOverlay = c.ShowBucketsOverlay;
        _controls.ShowMemoryHeatmap = c.ShowMemoryHeatmap;
        _controls.NormalizeHeatmapByMax = c.NormalizeHeatmapByMax;
        _controls.CacheLineSize = Math.Clamp(c.CacheLineSize, 4, 1024);
        _controls.GpuAccelerationEnabled = c.GpuAccelerationEnabled;
        _controls.CompareCpuGpuTiming = c.CompareCpuGpuTiming;
        _controls.ShowGpuThreadOverlay = c.ShowGpuThreadOverlay;
        _controls.ShowGpuBitonicStageGrid = c.ShowGpuBitonicStageGrid;
        _controls.PlaybackRate = Math.Clamp(c.PlaybackRate, 0.25, 4.0);

        if (_registry.TryGet(_selectedAlgorithmId, out var meta))
        {
            EnsureModeCompatibility(meta, setStatus: false);
        }

        RegenerateData();
    }

    private void ValidateRegistryMetadata(bool includeStableSmoke)
    {
        _registryMetadataIssues.Clear();
        _registryMetadataIssues.AddRange(RegistryMetadataValidator.Validate(_registry, includeStableSmoke));

        if (_registryMetadataIssues.Count == 0)
        {
            _registrySummaryText = includeStableSmoke
                ? "Registry health OK (metadata + stable smoke)."
                : "Registry health OK (metadata).";
            return;
        }

        _registrySummaryText = $"Registry issues: {_registryMetadataIssues.Count}";
    }

    private void StartBenchmark()
    {
        if (_benchmarkTask is not null)
        {
            return;
        }

        var algorithmIds = BuildBenchmarkAlgorithmSet();
        if (algorithmIds.Count == 0)
        {
            _benchmarkStatusText = "Benchmark skipped: no Bars-compatible A algorithm selected.";
            return;
        }

        _benchmarkCts?.Cancel();
        _benchmarkCts = new CancellationTokenSource();

        var request = new BenchmarkRequest
        {
            AlgorithmIds = algorithmIds,
            Size = _arraySize,
            Distribution = _distribution,
            Seed = _benchmarkSeed,
            HeadlessMode = _benchmarkHeadless,
            MaxEvents = Math.Max(5_000_000, _arraySize >= 2048 ? 35_000_000 : 15_000_000),
            TimeoutPerAlgorithm = _arraySize >= 2048 ? TimeSpan.FromSeconds(25) : TimeSpan.FromSeconds(12)
        };

        _benchmarkStatusText = $"Benchmark running ({algorithmIds.Count} algorithm(s), headless={(_benchmarkHeadless ? "on" : "off")})...";
        var token = _benchmarkCts.Token;
        _benchmarkTask = Task.Run(() => AlgorithmBenchmarkRunner.Run(_registry, request, token), token);
    }

    private void PollBenchmarkTask()
    {
        if (_benchmarkTask is null || !_benchmarkTask.IsCompleted)
        {
            return;
        }

        try
        {
            var suite = _benchmarkTask.GetAwaiter().GetResult();
            _lastBenchmarkSuite = suite;

            var succeeded = suite.Results.Count(static row => row.Completed && row.Sorted && row.MultisetPreserved && string.IsNullOrWhiteSpace(row.Error));
            _benchmarkStatusText = $"Benchmark completed: {succeeded}/{suite.Results.Count} successful.";
        }
        catch (OperationCanceledException)
        {
            _benchmarkStatusText = "Benchmark canceled.";
        }
        catch (Exception ex)
        {
            _benchmarkStatusText = $"Benchmark failed: {ex.Message}";
        }
        finally
        {
            _benchmarkTask = null;
            _benchmarkCts?.Dispose();
            _benchmarkCts = null;
        }
    }

    private void ExportBenchmarkCsv()
    {
        if (_lastBenchmarkSuite is null)
        {
            _benchmarkStatusText = "No benchmark results to export.";
            return;
        }

        try
        {
            _lastBenchmarkCsvPath = BenchmarkCsvExporter.SaveToDefaultPath(_appDataDir, _lastBenchmarkSuite);
            _benchmarkStatusText = $"Benchmark CSV exported: {_lastBenchmarkCsvPath}";
        }
        catch (Exception ex)
        {
            _benchmarkStatusText = $"CSV export failed: {ex.Message}";
        }
    }

    private void StartGrowthAnalysis()
    {
        if (_growthTask is not null)
        {
            return;
        }

        var sizes = ParseGrowthSizes(_growthSizeSeries);
        if (sizes.Count == 0)
        {
            _growthStatusText = "Growth skipped: invalid N list.";
            return;
        }

        var algorithmIds = BuildGrowthAlgorithmSet();
        if (algorithmIds.Count == 0)
        {
            _growthStatusText = "Growth skipped: no Bars-compatible A algorithm selected.";
            return;
        }

        _growthCts?.Cancel();
        _growthCts = new CancellationTokenSource();

        var request = new GrowthBenchmarkRequest
        {
            AlgorithmIds = algorithmIds,
            Sizes = sizes,
            Distribution = _distribution,
            Seed = _growthSeed,
            HeadlessMode = _growthHeadless,
            MaxEventsPerRun = Math.Max(6_000_000, sizes.Max() >= 2048 ? 35_000_000 : 15_000_000),
            TimeoutPerRun = sizes.Max() >= 2048 ? TimeSpan.FromSeconds(25) : TimeSpan.FromSeconds(12)
        };

        _growthStatusText = $"Growth running ({algorithmIds.Count} algorithm(s), {sizes.Count} size points)...";
        var token = _growthCts.Token;
        _growthTask = Task.Run(() => GrowthBenchmarkRunner.Run(_registry, request, token), token);
    }

    private void PollGrowthTask()
    {
        if (_growthTask is null || !_growthTask.IsCompleted)
        {
            return;
        }

        try
        {
            var suite = _growthTask.GetAwaiter().GetResult();
            _lastGrowthSuite = suite;

            var succeeded = suite.Results.Count(static row => row.Completed && row.Sorted && row.MultisetPreserved && string.IsNullOrWhiteSpace(row.Error));
            _growthStatusText = $"Growth completed: {succeeded}/{suite.Results.Count} successful points.";
        }
        catch (OperationCanceledException)
        {
            _growthStatusText = "Growth canceled.";
        }
        catch (Exception ex)
        {
            _growthStatusText = $"Growth failed: {ex.Message}";
        }
        finally
        {
            _growthTask = null;
            _growthCts?.Dispose();
            _growthCts = null;
        }
    }

    private void ExportGrowthCsv()
    {
        if (_lastGrowthSuite is null)
        {
            _growthStatusText = "No growth result to export.";
            return;
        }

        try
        {
            _lastGrowthExportPath = AnalysisExportService.SaveGrowthCsv(_analysisRootDir, _lastGrowthSuite);
            _growthStatusText = $"Growth CSV exported: {_lastGrowthExportPath}";
        }
        catch (Exception ex)
        {
            _growthStatusText = $"Growth CSV export failed: {ex.Message}";
        }
    }

    private void ExportGrowthJson()
    {
        if (_lastGrowthSuite is null)
        {
            _growthStatusText = "No growth result to export.";
            return;
        }

        try
        {
            _lastGrowthExportPath = AnalysisExportService.SaveGrowthJson(_analysisRootDir, _lastGrowthSuite);
            _growthStatusText = $"Growth JSON exported: {_lastGrowthExportPath}";
        }
        catch (Exception ex)
        {
            _growthStatusText = $"Growth JSON export failed: {ex.Message}";
        }
    }

    private void ExportComparisonCsv()
    {
        if (_comparisonHistory.Count == 0)
        {
            _growthStatusText = "No comparison records to export.";
            return;
        }

        try
        {
            _lastComparisonExportPath = AnalysisExportService.SaveComparisonCsv(_analysisRootDir, _comparisonHistory);
            _growthStatusText = $"Comparison CSV exported: {_lastComparisonExportPath}";
        }
        catch (Exception ex)
        {
            _growthStatusText = $"Comparison CSV export failed: {ex.Message}";
        }
    }

    private void ExportComparisonJson()
    {
        if (_comparisonHistory.Count == 0)
        {
            _growthStatusText = "No comparison records to export.";
            return;
        }

        try
        {
            _lastComparisonExportPath = AnalysisExportService.SaveComparisonJson(_analysisRootDir, _comparisonHistory);
            _growthStatusText = $"Comparison JSON exported: {_lastComparisonExportPath}";
        }
        catch (Exception ex)
        {
            _growthStatusText = $"Comparison JSON export failed: {ex.Message}";
        }
    }

    private List<int> ParseGrowthSizes(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<int>();
        }

        var sizes = new List<int>();
        var tokens = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            if (value < 8 || value > 5000000)
            {
                continue;
            }

            sizes.Add(value);
        }

        return sizes
            .Distinct()
            .OrderBy(static n => n)
            .ToList();
    }

    private List<string> BuildGrowthAlgorithmSet()
    {
        var ids = new List<string>();

        if (_benchmarkUseFavorites && _favorites.Count > 0)
        {
            foreach (var id in _favorites)
            {
                if (_registry.TryGet(id, out var meta)
                    && meta.Status == AlgorithmImplementationStatus.A
                    && (meta.SupportedViews & SupportedViews.Bars) != 0
                    && meta.Factory is not null)
                {
                    ids.Add(meta.Id);
                }
            }
        }

        if (_registry.TryGet(_selectedAlgorithmId, out var left)
            && left.Status == AlgorithmImplementationStatus.A
            && (left.SupportedViews & SupportedViews.Bars) != 0
            && left.Factory is not null)
        {
            ids.Add(left.Id);
        }

        if (_comparisonMode
            && _registry.TryGet(_comparisonRightAlgorithmId, out var right)
            && right.Status == AlgorithmImplementationStatus.A
            && (right.SupportedViews & SupportedViews.Bars) != 0
            && right.Factory is not null)
        {
            ids.Add(right.Id);
        }

        return ids
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> BuildBenchmarkAlgorithmSet()
    {
        var ids = new List<string>();

        if (_benchmarkUseFavorites && _favorites.Count > 0)
        {
            foreach (var id in _favorites)
            {
                if (!_registry.TryGet(id, out var meta)
                    || meta.Status != AlgorithmImplementationStatus.A
                    || (meta.SupportedViews & SupportedViews.Bars) == 0
                    || meta.Factory is null)
                {
                    continue;
                }

                ids.Add(meta.Id);
            }
        }

        if (_registry.TryGet(_selectedAlgorithmId, out var selected)
            && selected.Status == AlgorithmImplementationStatus.A
            && (selected.SupportedViews & SupportedViews.Bars) != 0
            && selected.Factory is not null)
        {
            ids.Add(selected.Id);
        }

        return ids
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool TryHandlePanelPageHotkey(Keys key)
    {
        return key switch
        {
            Keys.D1 or Keys.KeyPad1 => SetPanelPage(UiPanelPage.Run),
            Keys.D2 or Keys.KeyPad2 => SetPanelPage(UiPanelPage.Data),
            Keys.D3 or Keys.KeyPad3 => SetPanelPage(UiPanelPage.Algorithm),
            Keys.D4 or Keys.KeyPad4 => SetPanelPage(UiPanelPage.View),
            Keys.D5 or Keys.KeyPad5 => SetPanelPage(UiPanelPage.Audio),
            Keys.D6 or Keys.KeyPad6 => SetPanelPage(UiPanelPage.Analysis),
            Keys.D7 or Keys.KeyPad7 => SetPanelPage(UiPanelPage.ReplayExport),
            _ => false
        };
    }

    private bool SetPanelPage(UiPanelPage page)
    {
        _currentPanelPage = page;
        _showSidePanel = true;
        return true;
    }

    private bool IsAnySimulationRunning()
    {
        if (_simulation.GetStatisticsSnapshot().IsRunning)
        {
            return true;
        }

        if (_comparisonSimulation.GetStatisticsSnapshot().IsRunning)
        {
            return true;
        }

        if (_stringSimulation.GetStatisticsSnapshot().IsRunning)
        {
            return true;
        }

        if (_spatialSimulation.GetStatisticsSnapshot().IsRunning)
        {
            return true;
        }

        return false;
    }

    private bool HasCurrentModeData()
    {
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            return _simulation.HasData && _comparisonSimulation.HasData;
        }

        return _visualizationMode switch
        {
            VisualizationMode.String => _stringSimulation.HasData,
            VisualizationMode.Spatial => _spatialSimulation.HasData,
            _ => _simulation.HasData
        };
    }

    private SortStatisticsSnapshot GetCurrentStats()
    {
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            var left = _simulation.GetStatisticsSnapshot();
            var right = _comparisonSimulation.GetStatisticsSnapshot();
            return new SortStatisticsSnapshot(
                Comparisons: left.Comparisons + right.Comparisons,
                Swaps: left.Swaps + right.Swaps,
                Writes: left.Writes + right.Writes,
                ProcessedEvents: left.ProcessedEvents + right.ProcessedEvents,
                ElapsedMs: Math.Max(left.ElapsedMs, right.ElapsedMs),
                EffectiveEventsPerSecond: left.EffectiveEventsPerSecond + right.EffectiveEventsPerSecond,
                IsRunning: left.IsRunning || right.IsRunning,
                IsPaused: left.IsPaused || right.IsPaused,
                IsCompleted: left.IsCompleted && right.IsCompleted,
                DroppedComparisons: left.DroppedComparisons + right.DroppedComparisons,
                CacheHits: left.CacheHits + right.CacheHits,
                CacheMisses: left.CacheMisses + right.CacheMisses,
                ParallelQueueDepth: left.ParallelQueueDepth + right.ParallelQueueDepth,
                ActiveParallelTasks: left.ActiveParallelTasks + right.ActiveParallelTasks,
                BadPartitions: left.BadPartitions + right.BadPartitions,
                PivotQualityScore: (left.PivotQualityScore + right.PivotQualityScore) * 0.5);
        }

        return _visualizationMode switch
        {
            VisualizationMode.String => _stringSimulation.GetStatisticsSnapshot(),
            VisualizationMode.Spatial => _spatialSimulation.GetStatisticsSnapshot(),
            _ => _simulation.GetStatisticsSnapshot()
        };
    }

    private int ResolveFrameVisualBudget()
    {
        var configured = Math.Clamp(_controls.MaxVisualEventsPerFrame, 128, _visualEventsBuffer.Length);
        if (_fps >= 55.0f)
        {
            return configured;
        }

        if (_fps >= 30.0f)
        {
            return Math.Max(128, configured / 2);
        }

        return Math.Max(96, configured / 3);
    }

    private (int VisualQueueDepth, int AudioQueueDepth) GetCurrentQueueDepthSnapshot()
    {
        if (_comparisonMode && _visualizationMode == VisualizationMode.Bars)
        {
            var left = _simulation.GetQueueDepthSnapshot();
            var right = _comparisonSimulation.GetQueueDepthSnapshot();
            return (left.VisualQueueDepth + right.VisualQueueDepth, left.AudioQueueDepth + right.AudioQueueDepth);
        }

        return _visualizationMode switch
        {
            VisualizationMode.String => _stringSimulation.GetQueueDepthSnapshot(),
            VisualizationMode.Spatial => _spatialSimulation.GetQueueDepthSnapshot(),
            _ => _simulation.GetQueueDepthSnapshot()
        };
    }

    private void ToggleCurrentPause()
    {
        switch (_visualizationMode)
        {
            case VisualizationMode.String:
                _stringSimulation.TogglePause();
                break;
            case VisualizationMode.Spatial:
                _spatialSimulation.TogglePause();
                break;
            default:
                _simulation.TogglePause();
                break;
        }
    }

    private void StepCurrentOnce()
    {
        switch (_visualizationMode)
        {
            case VisualizationMode.String:
                _stringSimulation.StepOnce();
                break;
            case VisualizationMode.Spatial:
                _spatialSimulation.StepOnce();
                break;
            default:
                _simulation.StepOnce();
                break;
        }
    }

    private void ResetCurrentToSource()
    {
        switch (_visualizationMode)
        {
            case VisualizationMode.String:
                _stringSimulation.ResetToSource();
                break;
            case VisualizationMode.Spatial:
                _spatialSimulation.ResetToSource();
                break;
            default:
                _simulation.ResetToSource();
                break;
        }
    }

    private void ResetCurrentMemoryCounters()
    {
        switch (_visualizationMode)
        {
            case VisualizationMode.String:
                _stringSimulation.ResetMemoryAccessCounters();
                break;
            case VisualizationMode.Spatial:
                _spatialSimulation.ResetMemoryAccessCounters();
                break;
            default:
                _simulation.ResetMemoryAccessCounters();
                if (_comparisonMode)
                {
                    _comparisonSimulation.ResetMemoryAccessCounters();
                }
                break;
        }

        _statusText = "Memory access counters reset.";
    }

    private void StopCurrentEngine()
    {
        switch (_visualizationMode)
        {
            case VisualizationMode.String:
                _stringSimulation.Stop();
                break;
            case VisualizationMode.Spatial:
                _spatialSimulation.Stop();
                break;
            default:
                _simulation.Stop();
                if (_comparisonMode)
                {
                    _comparisonSimulation.Stop();
                }
                break;
        }
    }

    private void StopAllEngines()
    {
        _simulation.Stop();
        _comparisonSimulation.Stop();
        _stringSimulation.Stop();
        _spatialSimulation.Stop();
    }

    private void SetVisualizationMode(VisualizationMode mode)
    {
        if (_visualizationMode != mode)
        {
            StopAllEngines();
        }

        _visualizationMode = mode;
        if (_visualizationMode != VisualizationMode.Bars && _comparisonMode)
        {
            _comparisonMode = false;
            _comparisonCapturePending = false;
            _comparisonSimulation.Stop();
        }

        if (!IsBarsFamilyMode(_visualizationMode))
        {
            _replayMode = false;
            _controls.PlaybackRate = 1.0;
        }

        if (_registry.TryGet(_selectedAlgorithmId, out var meta))
        {
            EnsureModeCompatibility(meta, setStatus: true);
        }

        if (!HasCurrentModeData())
        {
            RegenerateData();
        }
    }

    private void EnsureModeCompatibility(AlgorithmMetadata metadata, bool setStatus)
    {
        if (_visualizationMode.IsSupportedBy(metadata.SupportedViews))
        {
            return;
        }

        var oldMode = _visualizationMode;
        _visualizationMode = metadata.SupportedViews.FirstSupportedMode();

        if (oldMode != _visualizationMode)
        {
            StopAllEngines();
            _pendingAutoReplayCapture = false;
            if (_visualizationMode != VisualizationMode.Bars)
            {
                _comparisonMode = false;
                _comparisonCapturePending = false;
            }

            if (!IsBarsFamilyMode(_visualizationMode))
            {
                _replayMode = false;
                _controls.PlaybackRate = 1.0;
            }

            if (!HasCurrentModeData())
            {
                RegenerateData();
            }
        }

        if (setStatus)
        {
            _statusText = $"View switched: {oldMode} -> {_visualizationMode} for {metadata.Name}.";
        }
    }

    private void PrepareAlgorithmSpecificState(ISortAlgorithm algorithm)
    {
        ResetViewTrackers();

        if (algorithm is INetworkScheduleProvider networkProvider)
        {
            _activeNetworkSchedule = networkProvider.BuildSchedule(Math.Max(1, _arraySize));
        }

        if (algorithm is IGraphAlgorithm graphAlgorithm)
        {
            graphAlgorithm.ConfigureGraph(_arraySize, _currentSeed, _graphEdgeDensity);
            _activeGraph = graphAlgorithm.Graph;
            _graphInDegrees = _activeGraph.InitialInDegrees.ToArray();
            _graphEmitted = new bool[_activeGraph.NodeCount];
            _graphSelectedNode = -1;
            _graphActiveEdge = null;
        }
    }

    private void ResetViewTrackers()
    {
        _activeNetworkSchedule = null;
        _networkCurrentStage = -1;
        _networkSwapPairs.Clear();

        _externalRuns.Clear();
        _externalGroups.Clear();

        _activeGraph = null;
        _graphInDegrees = Array.Empty<int>();
        _graphEmitted = Array.Empty<bool>();
        _graphSelectedNode = -1;
        _graphActiveEdge = null;

        _stringHighlightRowA = -1;
        _stringHighlightRowB = -1;
        _stringCurrentCharIndex = -1;
        _stringBucketHistogram = Array.Empty<int>();

        _spatialHighlightedIndices.Clear();
        _spatialRegionHighlight = null;
    }

    private void UpdateViewTrackers(ReadOnlySpan<SortEvent> events)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var ev = events[i];

            switch (ev.Type)
            {
                case SortEventType.Compare:
                case SortEventType.Swap:
                {
                    if (ev.Aux >= 0)
                    {
                        if (ev.Aux != _networkCurrentStage)
                        {
                            _networkCurrentStage = ev.Aux;
                            _networkSwapPairs.Clear();
                        }

                        if (ev.Type == SortEventType.Swap && ev.I >= 0 && ev.J >= 0)
                        {
                            _networkSwapPairs.Add(PairKey(ev.I, ev.J));
                        }
                    }
                    break;
                }

                case SortEventType.MarkStage:
                    if (ev.Value != _networkCurrentStage)
                    {
                        _networkCurrentStage = ev.Value;
                        _networkSwapPairs.Clear();
                    }
                    break;

                case SortEventType.RunCreated:
                {
                    var runId = ev.I;
                    if (runId < 0)
                    {
                        break;
                    }

                    _externalRuns[runId] = new ExternalRunTracker
                    {
                        RunId = runId,
                        Start = Math.Max(0, ev.J),
                        Length = Math.Max(1, ev.Value),
                        ReadCursor = -1,
                        WriteCursor = -1,
                        IsOutputRun = ev.Aux != 0
                    };
                    break;
                }

                case SortEventType.RunRead:
                {
                    if (_externalRuns.TryGetValue(ev.I, out var run))
                    {
                        run.ReadCursor = Math.Max(0, ev.J);
                    }
                    break;
                }

                case SortEventType.RunWrite:
                {
                    if (_externalRuns.TryGetValue(ev.I, out var run))
                    {
                        run.WriteCursor = Math.Max(0, ev.J);
                    }
                    break;
                }

                case SortEventType.MergeGroup:
                {
                    var groupId = Math.Max(0, ev.Value);
                    if (!_externalGroups.TryGetValue(groupId, out var group))
                    {
                        group = new ExternalMergeGroupTracker
                        {
                            GroupId = groupId,
                            OutputRunId = ev.J
                        };
                        _externalGroups[groupId] = group;
                    }

                    group.OutputRunId = ev.J;
                    group.InputRunIds.Add(ev.I);
                    break;
                }

                case SortEventType.MarkRun:
                {
                    if (_externalRuns.TryGetValue(ev.I, out var run))
                    {
                        if (ev.Value == 1)
                        {
                            run.ReadCursor = Math.Max(0, ev.Aux);
                        }
                        else if (ev.Value == 2)
                        {
                            run.WriteCursor = Math.Max(0, ev.Aux);
                        }
                    }
                    break;
                }

                case SortEventType.NodeSelected:
                    _graphSelectedNode = ev.I;
                    break;

                case SortEventType.NodeEmitted:
                    if ((uint)ev.I < (uint)_graphEmitted.Length)
                    {
                        _graphEmitted[ev.I] = true;
                    }
                    _graphSelectedNode = ev.I;
                    break;

                case SortEventType.InDegreeDecrement:
                    if ((uint)ev.J < (uint)_graphInDegrees.Length)
                    {
                        _graphInDegrees[ev.J] = Math.Max(0, ev.Value);
                    }
                    _graphActiveEdge = (ev.I, ev.J);
                    break;

                case SortEventType.CharCompare:
                    _stringHighlightRowA = ev.I;
                    _stringHighlightRowB = ev.J;
                    _stringCurrentCharIndex = ev.Value;
                    break;

                case SortEventType.CharIndex:
                    _stringCurrentCharIndex = ev.Aux != 0 ? ev.Aux : ev.Value;
                    break;

                case SortEventType.BucketMove:
                    _stringHighlightRowA = ev.I;
                    _stringHighlightRowB = ev.J;
                    _stringCurrentCharIndex = ev.Value;
                    if (ev.Aux >= 0 && ev.Aux < _stringBucketHistogram.Length)
                    {
                        _stringBucketHistogram[ev.Aux]++;
                    }
                    break;

                case SortEventType.PassStart:
                    _stringCurrentCharIndex = ev.Value;
                    if (_stringBucketHistogram.Length == 0)
                    {
                        _stringBucketHistogram = new int[257];
                    }
                    else
                    {
                        Array.Clear(_stringBucketHistogram, 0, _stringBucketHistogram.Length);
                    }
                    break;

                case SortEventType.PassEnd:
                    _stringCurrentCharIndex = ev.Value;
                    break;

                case SortEventType.PointSwap:
                    _spatialHighlightedIndices.Clear();
                    if (ev.I >= 0)
                    {
                        _spatialHighlightedIndices.Add(ev.I);
                    }
                    if (ev.J >= 0 && ev.J != ev.I)
                    {
                        _spatialHighlightedIndices.Add(ev.J);
                    }
                    break;

                case SortEventType.PointKeyComputed:
                case SortEventType.OrderUpdate:
                    _spatialHighlightedIndices.Clear();
                    if (ev.I >= 0)
                    {
                        _spatialHighlightedIndices.Add(ev.I);
                    }
                    break;

                case SortEventType.RegionHighlight:
                    _spatialRegionHighlight = DecodeSpatialRegionHighlight(ev);
                    break;
            }
        }
    }

    private SimulationFrameState BuildFrameState(
        int[] barsSnapshot,
        int barsCount,
        int maxValue,
        int[] barsMemoryAccess,
        int barsMemoryAccessMax,
        StringItem[] stringSnapshot,
        int stringCount,
        int[] stringMemoryAccess,
        int stringMemoryAccessMax,
        SpatialPoint[] spatialSnapshot,
        uint[] spatialKeys,
        int spatialCount,
        int[] spatialMemoryAccess,
        int spatialMemoryAccessMax,
        bool effectiveShowMemoryHeatmap,
        SortEvent[] eventBuffer,
        int visualCount)
    {
        var events = visualCount == eventBuffer.Length
            ? eventBuffer
            : eventBuffer.AsSpan(0, visualCount).ToArray();

        StringItem[] stringItems;
        if (stringCount <= 0)
        {
            stringItems = Array.Empty<StringItem>();
        }
        else
        {
            stringItems = new StringItem[stringCount];
            Array.Copy(stringSnapshot, stringItems, stringCount);
        }

        SpatialPoint[] spatialItems;
        uint[] spatialOrderKeys;
        if (spatialCount <= 0)
        {
            spatialItems = Array.Empty<SpatialPoint>();
            spatialOrderKeys = Array.Empty<uint>();
        }
        else
        {
            spatialItems = new SpatialPoint[spatialCount];
            Array.Copy(spatialSnapshot, spatialItems, spatialCount);
            spatialOrderKeys = new uint[spatialCount];
            Array.Copy(spatialKeys, spatialOrderKeys, spatialCount);
        }

        return new SimulationFrameState
        {
            Mode = _visualizationMode,
            VisualEnabled = _controls.VisualEnabled,
            ViewportWidth = ClientSize.X,
            ViewportHeight = ClientSize.Y,
            Overlay = new OverlayState
            {
                IntensityPercent = _controls.OverlayIntensity,
                ShowRanges = _controls.ShowRangesOverlay,
                ShowPivot = _controls.ShowPivotOverlay,
                ShowBuckets = _controls.ShowBucketsOverlay,
                ShowMemoryHeatmap = effectiveShowMemoryHeatmap,
                NormalizeHeatmapByMax = _controls.NormalizeHeatmapByMax
            },
            Bars = new BarsState
            {
                Snapshot = barsSnapshot,
                Count = barsCount,
                MaxValue = maxValue,
                RecentEvents = events,
                MemoryAccess = barsMemoryAccess,
                MemoryAccessMax = barsMemoryAccessMax
            },
            Network = BuildNetworkState(barsCount),
            External = BuildExternalState(),
            Graph = BuildGraphState(),
            String = new StringState
            {
                Items = stringItems,
                HighlightRowA = _stringHighlightRowA,
                HighlightRowB = _stringHighlightRowB,
                CurrentCharIndex = _stringCurrentCharIndex,
                BucketHistogram = _stringBucketHistogram,
                MemoryAccess = stringMemoryAccess,
                MemoryAccessMax = stringMemoryAccessMax
            },
            Spatial = new SpatialState
            {
                Points = spatialItems,
                Keys = spatialOrderKeys,
                HighlightedIndices = _spatialHighlightedIndices.ToArray(),
                ShowOrder = _spatialShowOrder,
                ShowGrid = _spatialShowGrid,
                RegionHighlight = _spatialRegionHighlight,
                MemoryAccess = spatialMemoryAccess,
                MemoryAccessMax = spatialMemoryAccessMax
            }
        };
    }

    private NetworkState BuildNetworkState(int wireCount)
    {
        var schedule = _activeNetworkSchedule;

        return new NetworkState
        {
            Schedule = schedule,
            WireCount = schedule?.WireCount ?? Math.Max(2, wireCount),
            CurrentStage = _networkCurrentStage,
            SwapPairKeys = _networkSwapPairs
        };
    }

    private ExternalState BuildExternalState()
    {
        var runs = _externalRuns.Values
            .OrderBy(static run => run.RunId)
            .Select(static run => new ExternalRunSnapshot(
                RunId: run.RunId,
                Start: run.Start,
                Length: run.Length,
                ReadCursor: run.ReadCursor,
                WriteCursor: run.WriteCursor,
                IsOutputRun: run.IsOutputRun))
            .ToArray();

        var groups = _externalGroups.Values
            .OrderBy(static group => group.GroupId)
            .Select(static group => new ExternalMergeGroupSnapshot(
                GroupId: group.GroupId,
                OutputRunId: group.OutputRunId,
                InputRunIds: group.InputRunIds.Distinct().ToArray()))
            .ToArray();

        return new ExternalState
        {
            Runs = runs,
            ActiveGroups = groups
        };
    }

    private GraphState BuildGraphState()
    {
        if (_activeGraph is null || _activeGraph.NodeCount <= 0)
        {
            return new GraphState();
        }

        var nodePositions = BuildGraphLayout(_activeGraph.NodeCount);
        var nodes = new GraphNodeSnapshot[_activeGraph.NodeCount];

        for (var i = 0; i < nodes.Length; i++)
        {
            var indegree = (uint)i < (uint)_graphInDegrees.Length ? _graphInDegrees[i] : 0;
            var emitted = (uint)i < (uint)_graphEmitted.Length && _graphEmitted[i];
            nodes[i] = new GraphNodeSnapshot(i, nodePositions[i], indegree, emitted);
        }

        var edges = new GraphEdgeSnapshot[_activeGraph.Edges.Length];
        for (var i = 0; i < edges.Length; i++)
        {
            var edge = _activeGraph.Edges[i];
            var isActive = _graphActiveEdge.HasValue && _graphActiveEdge.Value.From == edge.From && _graphActiveEdge.Value.To == edge.To;
            edges[i] = new GraphEdgeSnapshot(edge.From, edge.To, isActive);
        }

        return new GraphState
        {
            Nodes = nodes,
            Edges = edges,
            SelectedNode = _graphSelectedNode
        };
    }

    private void DrawActiveView(SimulationFrameState frameState)
    {
        var renderer = frameState.Mode switch
        {
            VisualizationMode.Bars => (IViewRenderer?)_barsRenderer,
            VisualizationMode.Network => _networkRenderer,
            VisualizationMode.External => _externalRenderer,
            VisualizationMode.Graph => _graphRenderer,
            VisualizationMode.String => _stringRenderer,
            VisualizationMode.Spatial => _spatialRenderer,
            _ => _barsRenderer
        };

        renderer?.Draw(frameState);
    }

    private static bool IsBarsFamilyMode(VisualizationMode mode)
    {
        return mode is VisualizationMode.Bars
            or VisualizationMode.Network
            or VisualizationMode.External
            or VisualizationMode.Graph;
    }

    private static long PairKey(int i, int j)
    {
        if (i > j)
        {
            (i, j) = (j, i);
        }

        return ((long)i << 32) | (uint)j;
    }

    private static SpatialRegionHighlight DecodeSpatialRegionHighlight(SortEvent ev)
    {
        var x0 = Math.Clamp(ev.I / 1000.0f, 0.0f, 1.0f);
        var y0 = Math.Clamp(ev.J / 1000.0f, 0.0f, 1.0f);
        var x1 = Math.Clamp(ev.Value / 1000.0f, 0.0f, 1.0f);
        var y1 = Math.Clamp(ev.Aux / 1000.0f, 0.0f, 1.0f);

        if (x1 < x0)
        {
            (x0, x1) = (x1, x0);
        }

        if (y1 < y0)
        {
            (y0, y1) = (y1, y0);
        }

        return new SpatialRegionHighlight(x0, y0, x1, y1);
    }

    private static Vector2[] BuildGraphLayout(int nodeCount)
    {
        var points = new Vector2[nodeCount];
        var center = new Vector2(0.5f, 0.5f);
        var radius = 0.42f;

        for (var i = 0; i < nodeCount; i++)
        {
            var angle = (float)(Math.PI * 2.0 * (i / (double)Math.Max(1, nodeCount)));
            points[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        return points;
    }

    private sealed class ExternalRunTracker
    {
        public int RunId { get; init; }
        public int Start { get; init; }
        public int Length { get; init; }
        public bool IsOutputRun { get; init; }
        public int ReadCursor { get; set; }
        public int WriteCursor { get; set; }
    }

    private sealed class ExternalMergeGroupTracker
    {
        public int GroupId { get; init; }
        public int OutputRunId { get; set; }
        public HashSet<int> InputRunIds { get; } = new();
    }

    private void CaptureSnapshot()
    {
        try
        {
            var width = ClientSize.X;
            var height = ClientSize.Y;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(_appDataDir, "snapshots", $"snapshot_{timestamp}_{_snapshotCounter++:D3}.png");
            SnapshotExporter.SavePng(path, width, height, pixels);
            _statusText = $"Snapshot saved: {path}";
        }
        catch (Exception ex)
        {
            _statusText = $"Snapshot failed: {ex.Message}";
        }
    }

    private void ToggleFullscreen()
    {
        WindowState = WindowState == OpenTK.Windowing.Common.WindowState.Fullscreen
            ? OpenTK.Windowing.Common.WindowState.Normal
            : OpenTK.Windowing.Common.WindowState.Fullscreen;
    }
}
