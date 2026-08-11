//This File includes code from:
//Copyright(c) 2018 Mark Heath, Andrew Ward & Contributors
//Licensed under the MIT License (see LICENSE (Nlayer).txt)
using System;

namespace NLayer.Decoder
{
    class LayerIIDecoder : LayerIIDecoderBase
    {
        static internal bool GetCRC(MpegFrame frame, ref uint crc)
        {
            return LayerIIDecoderBase.GetCRC(frame, SelectTable(frame), _allocLookupTable, true, ref crc);
        }

        static int[] SelectTable(IMpegFrame frame)
        {
            var bitRatePerChannel = (frame.BitRate / (frame.ChannelMode == MpegChannelMode.Mono ? 1 : 2)) / 1000;

            if (frame.Version == MpegVersion.Version1)
            {
                if ((bitRatePerChannel >= 56 && bitRatePerChannel <= 80) || (frame.SampleRate == 48000 && bitRatePerChannel >= 56))
                {
                    return _rateLookupTable[0];   
                }
                else if (frame.SampleRate != 48000 && bitRatePerChannel >= 96)
                {
                    return _rateLookupTable[1];  
                }
                else if (frame.SampleRate != 32000 && bitRatePerChannel <= 48)
                {
                    return _rateLookupTable[2];   
                }
                else
                {
                    return _rateLookupTable[3];  
                }
            }
            else
            {
                return _rateLookupTable[4];  
            }
        }

        static readonly int[][] _rateLookupTable = {
                                                       new int[] { 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },            
                                                       new int[] { 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },    
                                                       new int[] { 4, 4, 5, 5, 5, 5, 5, 5 },                                                                    
                                                       new int[] { 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 },                                                          
                                                       new int[] { 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 },    
                                                   };

        static readonly int[][] _allocLookupTable = {
                                                        new int[] { 2,  0, -5, -7, 16 },                                                 
                                                        new int[] { 3,  0, -5, -7,  3,-10,  4,  5, 16 },                                 
                                                        new int[] { 4,  0, -5, -7,  3,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 16 }, 
                                                        new int[] { 4,  0, -5,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16 }, 
                                                        new int[] { 4,  0, -5, -7,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 }, 
                                                        new int[] { 3,  0, -5, -7,-10,  4,  5,  6,  9 },                                 
                                                        new int[] { 4,  0, -5, -7,  3,-10,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14 }, 
                                                        new int[] { 2,  0, -5, -7,  3 },                                            
                                                    };

        internal LayerIIDecoder() : base(_allocLookupTable, 3) { }

        protected override int[] GetRateTable(IMpegFrame frame)
        {
            return SelectTable(frame);
        }

        protected override void ReadScaleFactorSelection(IMpegFrame frame, int[][] scfsi, int channels)
        {
            for (int sb = 0; sb < 30; sb++)
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    if (scfsi[ch][sb] == 2)
                    {
                        scfsi[ch][sb] = frame.ReadBits(2);
                    }
                }
            }
        }
    }
}
