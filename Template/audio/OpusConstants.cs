namespace BinderDyn.audio;

public static class OpusConstants
{
    public const int SampleRate = 48000;
    public const int Channels = 1;
    public const int FrameSizeMs = 20;
    public const int SamplesPerPacket = SampleRate * FrameSizeMs / 1000;
    public const int MinimumBufferedAudioMs = 1000;
}
