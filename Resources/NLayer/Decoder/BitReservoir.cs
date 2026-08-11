//This File includes code from:
//Copyright(c) 2018 Mark Heath, Andrew Ward & Contributors
//Licensed under the MIT License (see LICENSE (Nlayer).txt)
using System;

namespace NLayer.Decoder
{
    class BitReservoir
    {
        byte[] _buf = new byte[8192];
        int _start = 0, _end = -1, _bitsLeft = 0;
        long _bitsRead = 0L;

        static int GetSlots(IMpegFrame frame)
        {
            var cnt = frame.FrameLength - 4;
            if (frame.HasCrc) cnt -= 2;

            if (frame.Version == MpegVersion.Version1 && frame.ChannelMode != MpegChannelMode.Mono) return cnt - 32;
            if (frame.Version > MpegVersion.Version1 && frame.ChannelMode == MpegChannelMode.Mono) return cnt - 9;
            return cnt - 17;

        }

        public bool AddBits(IMpegFrame frame, int overlap)
        {
            var originalEnd = _end;

            var slots = GetSlots(frame);
            while (--slots >= 0)
            {
                var temp = frame.ReadBits(8);
                if (temp == -1) throw new System.IO.InvalidDataException("Frame did not have enough bytes!");
                _buf[++_end] = (byte)temp;
                if (_end == _buf.Length - 1) _end = -1;
            }

            _bitsLeft = 8;
            if (originalEnd == -1)
            {
                return overlap == 0;
            }
            else
            {
                if ((originalEnd + 1 - _start + _buf.Length) % _buf.Length >= overlap)
                {
                    _start = (originalEnd + 1 - overlap + _buf.Length) % _buf.Length;
                    return true;
                }
                else
                {
                    _start = originalEnd + overlap;
                    return false;
                }
            }
        }

        public int GetBits(int count)
        {
            int bitsRead;
            var bits = TryPeekBits(count, out bitsRead);
            if (bitsRead < count) throw new System.IO.InvalidDataException("Reservoir did not have enough bytes!");

            SkipBits(count);

            return bits;
        }

        public int Get1Bit()
        {
            if (_bitsLeft == 0) throw new System.IO.InvalidDataException("Reservoir did not have enough bytes!");

            --_bitsLeft;
            ++_bitsRead;
            var val = (_buf[_start] >> _bitsLeft) & 1;

            if (_bitsLeft == 0 && (_start = (_start + 1) % _buf.Length) != _end + 1)
            {
                _bitsLeft = 8;
            }

            return val;
        }

        public int TryPeekBits(int count, out int readCount)
        {
            if (count < 0 || count > 32) throw new ArgumentOutOfRangeException("count", "Must return between 0 and 32 bits!");

            if (_bitsLeft == 0 || count == 0)
            {
                readCount = 0;
                return 0;
            }

            var bits = (int)_buf[_start];
            if (count < _bitsLeft)
            {
                bits >>= _bitsLeft - count;
                bits &= ((1 << count) - 1);
                readCount = count;
                return bits;
            }

            bits &= ((1 << _bitsLeft) - 1);
            count -= _bitsLeft;
            readCount = _bitsLeft;

            var resStart = _start;

            while (count > 0)
            {
                if ((resStart = (resStart + 1) % _buf.Length) == _end + 1)
                {
                    break;
                }

                var bitsToRead = Math.Min(count, 8);

                bits <<= bitsToRead;
                bits |= (_buf[resStart] >> ((8 - bitsToRead) % 8));

                count -= bitsToRead;

                readCount += bitsToRead;
            }

            return bits;
        }

        public int BitsAvailable
        {
            get
            {
                if (_bitsLeft > 0)
                {
                    return (((_end + _buf.Length) - _start) % _buf.Length) * 8 + _bitsLeft;
                }
                return 0;
            }
        }

        public long BitsRead
        {
            get { return _bitsRead; }
        }

        public void SkipBits(int count)
        {
            if (count > 0)
            {
                if (count > BitsAvailable) throw new ArgumentOutOfRangeException("count");

                var offset = (8 - _bitsLeft) + count;
                _start = ((offset / 8) + _start) % _buf.Length;
                _bitsLeft = 8 - (offset % 8);

                _bitsRead += count;
            }
        }

        public void RewindBits(int count)
        {
            _bitsLeft += count;
            _bitsRead -= count;
            while (_bitsLeft > 8)
            {
                --_start;
                _bitsLeft -= 8;
            }
            while (_start < 0)
            {
                _start += _buf.Length;
            }
        }

        public void FlushBits()
        {
            if (_bitsLeft < 8)
            {
                SkipBits(_bitsLeft);
            }
        }

        public void Reset()
        {
            _start = 0;
            _end = -1;
            _bitsLeft = 0;
        }
    }
}
