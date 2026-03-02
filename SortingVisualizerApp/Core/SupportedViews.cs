namespace SortingVisualizerApp.Core;

[Flags]
public enum SupportedViews
{
    None = 0,
    Bars = 1 << 0,
    Network = 1 << 1,
    External = 1 << 2,
    Graph = 1 << 3,
    String = 1 << 4,
    Spatial = 1 << 5,
    All = Bars | Network | External | Graph | String | Spatial
}
