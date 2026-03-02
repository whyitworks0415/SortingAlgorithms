using System.IO.Compression;

namespace SortingVisualizerApp.Core;

public static class ReplayReaderV1
{
    private static readonly byte[] Magic = "SVR3RP"u8.ToArray();
    private const byte Version = 1;

    public static ReplayFileV1 Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress, leaveOpen: false);
        using var reader = new BinaryReader(gzip);

        var magic = reader.ReadBytes(Magic.Length);
        if (!magic.SequenceEqual(Magic))
        {
            throw new InvalidDataException("Invalid replay header.");
        }

        var version = reader.ReadByte();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported replay version: {version}.");
        }

        var algorithmId = reader.ReadString();
        var n = reader.ReadInt32();
        var seed = reader.ReadInt32();
        var distribution = (DistributionPreset)reader.ReadByte();
        var maxValue = reader.ReadInt32();
        var createdUtc = DateTime.FromBinary(reader.ReadInt64());

        var eventCount = reader.ReadInt32();
        var keyframeCount = reader.ReadInt32();
        if (eventCount < 0 || keyframeCount < 0)
        {
            throw new InvalidDataException("Corrupted replay counts.");
        }

        var keyframes = new ReplayKeyframe[keyframeCount];
        var previousKeyframeIndex = 0;
        for (var k = 0; k < keyframeCount; k++)
        {
            var eventDelta = (int)ReadVarUInt(reader);
            var eventIndex = previousKeyframeIndex + eventDelta;
            previousKeyframeIndex = eventIndex;

            var length = (int)ReadVarUInt(reader);
            var snapshot = new int[length];
            var prev = 0;
            for (var i = 0; i < length; i++)
            {
                prev += ReadVarInt(reader);
                snapshot[i] = prev;
            }

            keyframes[k] = new ReplayKeyframe(eventIndex, snapshot);
        }

        var events = new SortEvent[eventCount];
        var prevI = 0;
        var prevJ = 0;
        var prevValue = 0;
        var prevAux = 0;
        var prevStep = 0L;

        for (var i = 0; i < eventCount; i++)
        {
            var type = (SortEventType)reader.ReadByte();
            prevI += ReadVarInt(reader);
            prevJ += ReadVarInt(reader);
            prevValue += ReadVarInt(reader);
            prevAux += ReadVarInt(reader);
            prevStep += ReadVarLong(reader);

            events[i] = new SortEvent(type, prevI, prevJ, prevValue, prevAux, prevStep);
        }

        return new ReplayFileV1(
            AlgorithmId: algorithmId,
            N: n,
            Seed: seed,
            Distribution: distribution,
            MaxValue: maxValue,
            CreatedUtc: createdUtc,
            Events: events,
            Keyframes: keyframes);
    }

    internal static uint ReadVarUInt(BinaryReader reader)
    {
        uint result = 0;
        var shift = 0;
        while (true)
        {
            if (shift > 35)
            {
                throw new InvalidDataException("VarUInt too long.");
            }

            var b = reader.ReadByte();
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }
    }

    internal static ulong ReadVarULong(BinaryReader reader)
    {
        ulong result = 0;
        var shift = 0;
        while (true)
        {
            if (shift > 70)
            {
                throw new InvalidDataException("VarULong too long.");
            }

            var b = reader.ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }
    }

    internal static int ReadVarInt(BinaryReader reader)
    {
        var value = ReadVarUInt(reader);
        return (int)((value >> 1) ^ (uint)-(int)(value & 1));
    }

    internal static long ReadVarLong(BinaryReader reader)
    {
        var value = ReadVarULong(reader);
        return (long)((value >> 1) ^ (~(value & 1) + 1));
    }
}
