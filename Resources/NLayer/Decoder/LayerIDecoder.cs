//This File includes code from:
//Copyright(c) 2018 Mark Heath, Andrew Ward & Contributors
//Licensed under the MIT License (see LICENSE (Nlayer).txt)
using System;

namespace NLayer.Decoder
{
    class LayerIDecoder : LayerIIDecoderBase
    {
        static internal bool GetCRC(MpegFrame frame, ref uint crc)
        {
            return LayerIIDecoderBase.GetCRC(frame, _rateTable, _allocLookupTable, false, ref crc);
        }

        static readonly int[] _rateTable = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        static readonly int[][] _allocLookupTable = { new int[] { 4, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 } };

        internal LayerIDecoder() : base(_allocLookupTable, 1) { }

        protected override int[] GetRateTable(IMpegFrame frame)
        {
            return _rateTable;
        }

        protected override void ReadScaleFactorSelection(IMpegFrame frame, int[][] scfsi, int channels)
        {
        }
    }
}
