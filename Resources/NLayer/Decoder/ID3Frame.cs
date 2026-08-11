//This File includes code from:
//Copyright(c) 2018 Mark Heath, Andrew Ward & Contributors
//Licensed under the MIT License (see LICENSE (Nlayer).txt)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NLayer.Decoder
{
    class ID3Frame : FrameBase
    {
        internal static ID3Frame TrySync(uint syncMark)
        {
            if ((syncMark & 0xFFFFFF00U) == 0x49443300)
            {
                return new ID3Frame { _version = 2 };
            }

            if ((syncMark & 0xFFFFFF00U) == 0x54414700)
            {
                if ((syncMark & 0xFF) == 0x2B)
                {
                    return new ID3Frame { _version = 1 };
                }
                else
                {
                    return new ID3Frame { _version = 0 };
                }
            }

            return null;
        }

        int _version;

        ID3Frame()
        {

        }

        protected override int Validate()
        {
            switch (_version)
            {
                case 2:
                    var buf = new byte[7];
                    if (Read(3, buf) == 7)
                    {
                        byte flagsMask;
                        switch (buf[0])
                        {
                            case 2:
                                flagsMask = 0x3F;
                                break;
                            case 3:
                                flagsMask = 0x1F;
                                break;
                            case 4:
                                flagsMask = 0x0F;
                                break;
                            default:
                                return -1;
                        }

                        var size = (buf[3] << 21)
                                 | (buf[4] << 14)
                                 | (buf[5] << 7)
                                 | (buf[6]);

                        if (!(((buf[2] & flagsMask) | (buf[3] & 0x80) | (buf[4] & 0x80) | (buf[5] & 0x80) | (buf[6] & 0x80)) != 0 || buf[1] == 0xFF))
                        {
                            return size + 10;  
                        }
                    }
                    break;
                case 1:
                    return 227 + 128;
                case 0:
                    return 128;
            }

            return -1;
        }

        internal override void Parse()
        {
            switch (_version)
            {
                case 2:
                    ParseV2();
                    break;
                case 1:
                    ParseV1Enh();
                    break;
                case 0:
                    ParseV1(3);
                    break;
            }
        }

        void ParseV1(int offset)
        {
           
        }

        void ParseV1Enh()
        {
            ParseV1(230);

            
        }

        void ParseV2()
        {
        }

        internal int Version
        {
            get
            {
                if (_version == 0) return 1;
                return _version;
            }
        }


        internal void Merge(ID3Frame newFrame)
        {
        }
    }
}
