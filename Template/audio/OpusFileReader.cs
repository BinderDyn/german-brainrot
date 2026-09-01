using System;
using System.Collections.Generic;
using System.IO;
using Concentus;
using Concentus.Oggfile;

namespace BinderDyn.audio;

public sealed class OpusFileReader : IDisposable
{
    private readonly List<float> _samples = new();
    private bool _disposed;

    public IReadOnlyList<float> Samples => _samples;

    public int TotalSamples => _samples.Count;

    public static OpusFileReader FromFile(string filePath)
    {
        var reader = new OpusFileReader();
        reader.Load(filePath);
        return reader;
    }

    private void Load(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
        {
            LoadWav(filePath);
            return;
        }

        LoadOpus(filePath);
    }

    private void LoadOpus(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        var decoder = OpusCodecFactory.CreateDecoder(OpusConstants.SampleRate, OpusConstants.Channels);
        var oggStream = new OpusOggReadStream(decoder, fileStream);

        while (oggStream.HasNextPacket)
        {
            var packet = oggStream.DecodeNextPacket();
            if (packet == null || packet.Length == 0)
            {
                continue;
            }

            foreach (var sample in packet)
            {
                _samples.Add(sample / 32768f);
            }
        }
    }

    private void LoadWav(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var reader = new BinaryReader(fileStream);

        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
        {
            throw new InvalidDataException($"Not a WAV file: {filePath}");
        }

        reader.ReadInt32();
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
        {
            throw new InvalidDataException($"Not a WAV file: {filePath}");
        }

        short channels = 1;
        var sampleRate = OpusConstants.SampleRate;
        short bitsPerSample = 16;
        var dataOffset = 0L;
        var dataSize = 0;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            switch (chunkId)
            {
                case "fmt ":
                    reader.ReadInt16();
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt16();
                    bitsPerSample = reader.ReadInt16();
                    if (chunkSize > 16)
                    {
                        reader.BaseStream.Seek(chunkSize - 16, SeekOrigin.Current);
                    }
                    break;
                case "data":
                    dataOffset = reader.BaseStream.Position;
                    dataSize = chunkSize;
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    break;
                default:
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    break;
            }
        }

        if (dataSize == 0)
        {
            throw new InvalidDataException($"WAV file has no data chunk: {filePath}");
        }

        reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
        var bytes = reader.ReadBytes(dataSize);
        AppendPcm(bytes, channels, bitsPerSample, sampleRate);
    }

    private void AppendPcm(byte[] bytes, short channels, short bitsPerSample, int sampleRate)
    {
        if (bitsPerSample != 16)
        {
            throw new NotSupportedException("Only 16-bit PCM WAV files are supported.");
        }

        var sampleCount = bytes.Length / (bitsPerSample / 8);
        for (var i = 0; i < sampleCount; i += channels)
        {
            var sample = BitConverter.ToInt16(bytes, i * sizeof(short));
            _samples.Add(sample / 32768f);
        }

        if (sampleRate != OpusConstants.SampleRate && _samples.Count > 0)
        {
            ResampleToTargetRate(sampleRate);
        }
    }

    private void ResampleToTargetRate(int sourceRate)
    {
        if (sourceRate == OpusConstants.SampleRate)
        {
            return;
        }

        var resampled = new List<float>();
        var ratio = (double)sourceRate / OpusConstants.SampleRate;
        var targetCount = (int)(_samples.Count / ratio);

        for (var i = 0; i < targetCount; i++)
        {
            var sourceIndex = (int)(i * ratio);
            if (sourceIndex >= _samples.Count)
            {
                sourceIndex = _samples.Count - 1;
            }

            resampled.Add(_samples[sourceIndex]);
        }

        _samples.Clear();
        _samples.AddRange(resampled);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _samples.Clear();
    }
}
