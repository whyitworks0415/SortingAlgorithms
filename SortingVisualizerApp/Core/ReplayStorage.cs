using System.IO.Compression;

namespace SortingVisualizerApp.Core;

public static class ReplayStorage
{
    private const int Version = 1;
    private const string Magic = "SVR1";

    public static void Save(string path, SortReplayLog log)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal, leaveOpen: false);
        using var writer = new BinaryWriter(gzip);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(log.AlgorithmId ?? string.Empty);
        writer.Write(log.CreatedUtc.ToBinary());

        writer.Write(log.InitialData.Length);
        for (var i = 0; i < log.InitialData.Length; i++)
        {
            writer.Write(log.InitialData[i]);
        }

        writer.Write(log.Events.Length);
        for (var i = 0; i < log.Events.Length; i++)
        {
            var ev = log.Events[i];
            writer.Write((byte)ev.Type);
            writer.Write(ev.I);
            writer.Write(ev.J);
            writer.Write(ev.Value);
            writer.Write(ev.Aux);
            writer.Write(ev.StepId);
        }
    }

    public static SortReplayLog Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress, leaveOpen: false);
        using var reader = new BinaryReader(gzip);

        var magic = reader.ReadString();
        if (!string.Equals(magic, Magic, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid replay file header.");
        }

        var version = reader.ReadInt32();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported replay version: {version}.");
        }

        var algorithmId = reader.ReadString();
        var createdUtc = DateTime.FromBinary(reader.ReadInt64());

        var dataLength = reader.ReadInt32();
        if (dataLength < 0)
        {
            throw new InvalidDataException("Invalid replay data length.");
        }

        var initialData = new int[dataLength];
        for (var i = 0; i < dataLength; i++)
        {
            initialData[i] = reader.ReadInt32();
        }

        var eventCount = reader.ReadInt32();
        if (eventCount < 0)
        {
            throw new InvalidDataException("Invalid replay event count.");
        }

        var events = new SortEvent[eventCount];
        for (var i = 0; i < eventCount; i++)
        {
            var type = (SortEventType)reader.ReadByte();
            var eI = reader.ReadInt32();
            var eJ = reader.ReadInt32();
            var value = reader.ReadInt32();
            var aux = reader.ReadInt32();
            var step = reader.ReadInt64();
            events[i] = new SortEvent(type, eI, eJ, value, aux, step);
        }

        return new SortReplayLog(algorithmId, createdUtc, initialData, events);
    }
}
