namespace SortingVisualizerApp.Core;

public readonly record struct SortEvent(
    SortEventType Type,
    int I = -1,
    int J = -1,
    int Value = 0,
    int Aux = 0,
    long StepId = 0);
