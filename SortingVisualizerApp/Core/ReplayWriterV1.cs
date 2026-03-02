using System.IO.Compression;

namespace SortingVisualizerApp.Core;

public static class ReplayWriterV1
{
    private static readonly byte[] Magic = "SVR3RP"u8.ToArray();
    private const byte Version = 1;

    public static void Save(string path, ReplayFileV1 replay)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal, leaveOpen: false);
        using var writer = new BinaryWriter(gzip);

        writer.Write(Magic);
        writer.Write(Version);

        writer.Write(replay.AlgorithmId ?? string.Empty);
        writer.Write(replay.N);
        writer.Write(replay.Seed);
        writer.Write((byte)replay.Distribution);
        writer.Write(replay.MaxValue);
        writer.Write(replay.CreatedUtc.ToBinary());

        writer.Write(replay.Events.Length);
        writer.Write(replay.Keyframes.Length);

        var previousKeyframeIndex = 0;
        for (var k = 0; k < replay.Keyframes.Length; k++)
        {
            var keyframe = replay.Keyframes[k];
            WriteVarUInt(writer, (uint)Math.Max(0, keyframe.EventIndex - previousKeyframeIndex));
            previousKeyframeIndex = keyframe.EventIndex;

            WriteVarUInt(writer, (uint)keyframe.Snapshot.Length);
            var prev = 0;
            for (var i = 0; i < keyframe.Snapshot.Length; i++)
            {
                var delta = keyframe.Snapshot[i] - prev;
                WriteVarInt(writer, delta);
                prev = keyframe.Snapshot[i];
            }
        }

        var prevI = 0;
        var prevJ = 0;
        var prevValue = 0;
        var prevAux = 0;
        var prevStep = 0L;

        for (var i = 0; i < replay.Events.Length; i++)
        {
            var ev = replay.Events[i];
            writer.Write((byte)ev.Type);

            WriteVarInt(writer, ev.I - prevI);
            WriteVarInt(writer, ev.J - prevJ);
            WriteVarInt(writer, ev.Value - prevValue);
            WriteVarInt(writer, ev.Aux - prevAux);
            WriteVarLong(writer, ev.StepId - prevStep);

            prevI = ev.I;
            prevJ = ev.J;
            prevValue = ev.Value;
            prevAux = ev.Aux;
            prevStep = ev.StepId;
        }
    }

    internal static void WriteVarUInt(BinaryWriter writer, uint value)
    {
        while (value >= 0x80u)
        {
            writer.Write((byte)((value & 0x7Fu) | 0x80u));
            value >>= 7;
        }

        writer.Write((byte)value);
    }

    internal static void WriteVarULong(BinaryWriter writer, ulong value)
    {
        while (value >= 0x80ul)
        {
            writer.Write((byte)((value & 0x7Ful) | 0x80ul));
            value >>= 7;
        }

        writer.Write((byte)value);
    }

    internal static void WriteVarInt(BinaryWriter writer, int value)
    {
        var zigzag = (uint)((value << 1) ^ (value >> 31));
        WriteVarUInt(writer, zigzag);
    }

    internal static void WriteVarLong(BinaryWriter writer, long value)
    {
        var zigzag = (ulong)((value << 1) ^ (value >> 63));
        WriteVarULong(writer, zigzag);
    }
}
