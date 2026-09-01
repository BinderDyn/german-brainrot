using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace BinderDyn.audio;

public sealed class AudioSender : IDisposable
{
    private readonly Action<OpusPacket> _sendPacket;
    private readonly OpusFileReader _reader;
    private bool _disposed;

    public AudioSender(Action<OpusPacket> sendPacket, OpusFileReader reader)
    {
        _sendPacket = sendPacket;
        _reader = reader;
    }

    public IEnumerator SendRoutine()
    {
        var stopwatch = Stopwatch.StartNew();
        var packetIndex = 0;
        var samples = _reader.Samples;
        var chunkSize = OpusConstants.SamplesPerPacket;

        while (!_disposed)
        {
            var sampleIndex = packetIndex * chunkSize;
            if (sampleIndex >= samples.Count)
            {
                yield break;
            }

            var count = Math.Min(chunkSize, samples.Count - sampleIndex);
            var packetSamples = new float[count];
            for (var i = 0; i < count; i++)
            {
                packetSamples[i] = samples[sampleIndex + i];
            }

            _sendPacket(new OpusPacket
            {
                SampleIndex = sampleIndex,
                SampleCount = count,
                Samples = packetSamples
            });

            packetIndex++;
            var targetMs = packetIndex * OpusConstants.FrameSizeMs;
            var delayMs = targetMs - stopwatch.ElapsedMilliseconds;
            if (delayMs > 0)
            {
                yield return new WaitForSeconds(delayMs / 1000f);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _reader.Dispose();
    }
}
