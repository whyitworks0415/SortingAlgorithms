using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public interface IViewRenderer : IDisposable
{
    VisualizationMode Mode { get; }
    void Draw(SimulationFrameState state);
}
