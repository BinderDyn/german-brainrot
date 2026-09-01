using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BinderDyn.audio;

public sealed class AudioReceiver : IDisposable
{
    private readonly AudioSource _audioSource;
    private readonly int _totalSamples;
    private readonly CancellationToken _cancellationToken;
    private readonly AudioClip _clip;
    private readonly object _bufferLock = new();
    private int _receivedSamples;
    private int _bufferedMs;
    private bool _isPlaying;
    private bool _disposed;

    public AudioReceiver(AudioSource audioSource, int totalSamples, CancellationToken cancellationToken)
    {
        _audioSource = audioSource;
        _totalSamples = totalSamples;
        _cancellationToken = cancellationToken;
        _clip = AudioClip.Create(
            $"GermanBrainrot_{Guid.NewGuid():N}",
            Mathf.Max(totalSamples, OpusConstants.SamplesPerPacket),
            OpusConstants.Channels,
            OpusConstants.SampleRate,
            false);
        _audioSource.clip = _clip;
    }

    public void ReceivePacket(OpusPacket packet)
    {
        if (_disposed || packet.Samples == null || packet.SampleCount == 0)
        {
            return;
        }

        lock (_bufferLock)
        {
            _clip.SetData(packet.Samples, packet.SampleIndex);
            _receivedSamples = Math.Max(_receivedSamples, packet.SampleIndex + packet.SampleCount);
            _bufferedMs += OpusConstants.FrameSizeMs;
        }

        TryStartPlayback();
    }

    private void TryStartPlayback()
    {
        if (_isPlaying || _bufferedMs < OpusConstants.MinimumBufferedAudioMs)
        {
            return;
        }

        _isPlaying = true;
        _audioSource.Play();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        if (_clip != null)
        {
            UnityEngine.Object.Destroy(_clip);
        }
    }
}
