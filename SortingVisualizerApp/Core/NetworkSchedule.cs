namespace SortingVisualizerApp.Core;

public sealed class NetworkSchedule
{
    public NetworkSchedule(int wireCount, IReadOnlyList<NetworkStage> stages)
    {
        WireCount = Math.Max(0, wireCount);
        Stages = stages ?? Array.Empty<NetworkStage>();
    }

    public int WireCount { get; }
    public IReadOnlyList<NetworkStage> Stages { get; }

    public int StageCount => Stages.Count;
}

public sealed class NetworkStage
{
    public NetworkStage(IReadOnlyList<NetworkComparator> comparators)
    {
        Comparators = comparators ?? Array.Empty<NetworkComparator>();
    }

    public IReadOnlyList<NetworkComparator> Comparators { get; }
}

public readonly record struct NetworkComparator(int I, int J, bool Ascending);

public interface INetworkScheduleProvider
{
    NetworkSchedule BuildSchedule(int length);
}
