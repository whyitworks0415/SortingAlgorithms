# Sorting Algorithm Visualizer & Sonifier (OpenGL)

Windows + .NET 8 + OpenTK + ImGui.NET + NAudio desktop app.

## Build

```powershell
dotnet restore SortingVisualizer.sln
dotnet build SortingVisualizer.sln
```

## Run App

```powershell
dotnet run --project SortingVisualizerApp/SortingVisualizerApp.csproj
```

## Run Algorithm Expansion Tests

```powershell
dotnet run --project SortingVisualizerTests/SortingVisualizerTests.csproj
```

## GPU Acceleration Notes

- GPU compute shaders are loaded from `SortingVisualizerApp/Gpu/Shaders/*.comp`.
- Toggle GPU on/off in `View` page: `Enable GPU Acceleration`.
- GPU-capable algorithms:
  - `GPU Bitonic Sort`
  - `GPU Radix LSD Sort`
- CPU fallback is always available if GPU runtime/shader init fails.
- Massive scale:
  - Bars `N` supports up to `5,000,000`.
  - Heatmap overlay is auto-disabled at very large `N` to protect FPS.

## Benchmark (App UI)

- Open app and click `Run Benchmark` in the `Persistence` section.
- Benchmark runs selected Bars-compatible A algorithms (selected item, and favorites if enabled).
- Export CSV with `Export Benchmark CSV`.
- Output path:
  - `%LocalAppData%\SortingVisualizerApp\bench\yyyyMMdd_HHmm.csv`

CSV columns:

`timestamp_utc,algorithm_id,algorithm_name,n,distribution,seed,elapsed_ms,comparisons,swaps,writes,processed_events,completed,sorted,multiset_preserved,error`

## Hotkeys

- `Space`: Start/Pause
- `S`: Step one event
- `R`: Reset to source data
- `G`: Regenerate/shuffle data
- `1`: Bars view
- `2`: Network view
- `3`: External view
- `4`: Graph view
- `5`: String view
- `6`: Spatial view
- `U`: Toggle side panel
- `H`: Toggle HUD overlay
- `D`: Toggle diagnostics overlay
- `F`: Toggle fullscreen
