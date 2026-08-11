//This File includes code from:
//Copyright(c) 2018 Mark Heath, Andrew Ward & Contributors
//Licensed under the MIT License (see LICENSE (Nlayer).txt)
namespace NLayer
{
    public interface IMpegFrame
    {
        int SampleRate { get; }

        int SampleRateIndex { get; }

        int FrameLength { get; }

        int BitRate { get; }

        MpegVersion Version { get; }

        MpegLayer Layer { get; }

        MpegChannelMode ChannelMode { get; }

        int ChannelModeExtension { get; }

        int SampleCount { get; }

        int BitRateIndex { get; }

        bool IsCopyrighted { get; }

        bool HasCrc { get; }

        bool IsCorrupted { get; }

        void Reset();

        int ReadBits(int bitCount);
    }
}
