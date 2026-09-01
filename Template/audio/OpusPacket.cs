using System;
using Unity.Netcode;

namespace BinderDyn.audio;

public struct OpusPacket : INetworkSerializable, IEquatable<OpusPacket>
{
    public int SampleIndex;
    public int SampleCount;
    public float[] Samples;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (!serializer.IsReader && Samples != null)
        {
            SampleCount = Samples.Length;
        }

        serializer.SerializeValue(ref SampleIndex);
        serializer.SerializeValue(ref SampleCount);

        if (serializer.IsReader)
        {
            Samples = Samples is { Length: > 0 } ? Samples : new float[SampleCount];
        }
        else if (Samples == null)
        {
            Samples = Array.Empty<float>();
            SampleCount = 0;
        }

        for (var i = 0; i < SampleCount; i++)
        {
            serializer.SerializeValue(ref Samples[i]);
        }
    }

    public bool Equals(OpusPacket other)
    {
        if (SampleIndex != other.SampleIndex || SampleCount != other.SampleCount)
        {
            return false;
        }

        for (var i = 0; i < SampleCount; i++)
        {
            if (Math.Abs(Samples[i] - other.Samples[i]) > 0.0001f)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is OpusPacket other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SampleIndex, SampleCount);
}
