namespace SortingVisualizerApp.Core;

public sealed record ReplayKeyframe(
    int EventIndex,
    int[] Snapshot);

public sealed record ReplayFileV1(
    string AlgorithmId,
    int N,
    int Seed,
    DistributionPreset Distribution,
    int MaxValue,
    DateTime CreatedUtc,
    SortEvent[] Events,
    ReplayKeyframe[] Keyframes);
