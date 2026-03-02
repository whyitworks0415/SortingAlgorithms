namespace SortingVisualizerApp.Core;

public readonly record struct AudioTrigger(
    SortEventType Type,
    int Value,
    int MaxValue,
    float Pan = 0.0f);
