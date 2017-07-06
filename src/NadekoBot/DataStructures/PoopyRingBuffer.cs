using System;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures
{
    public class PoopyRingBuffer : IDisposable
    {
        // readpos == writepos means empty
        // writepos == readpos - 1 means full 

        private byte[] buffer;
        public int Capacity { get; }

        private int _readPos = 0;
        private int ReadPos
        {
            get => _readPos;
            set => _readPos = value;
        }
        private int _writePos = 0;
        private int WritePos
        {
            get => _writePos;
            set => _writePos = value;
        }
        public int Length => ReadPos <= WritePos 
            ? WritePos - ReadPos 
            : Capacity - (ReadPos - WritePos);

        public int RemainingCapacity
        {
            get => Capacity - Length - 1;
        }

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        public PoopyRingBuffer(int capacity = 81920 * 100)
        {
            this.Capacity = capacity + 1;
            this.buffer = new byte[this.Capacity];
        }

        public int Read(byte[] b, int offset, int toRead)
        {
            if (WritePos == ReadPos)
                return 0;

            if (toRead > Length)
                toRead = Length;

            if (WritePos > ReadPos)
            {
                Array.Copy(buffer, ReadPos, b, offset, toRead);
                ReadPos += toRead;
            }
            else
            {
                var toEnd = Capacity - ReadPos;
                var firstRead = toRead > toEnd ?
                    toEnd :
                    toRead;
                Array.Copy(buffer, ReadPos, b, offset, firstRead);
                ReadPos += firstRead;
                var secondRead = toRead - firstRead;
                if (secondRead > 0)
                {
                    Array.Copy(buffer, 0, b, offset + firstRead, secondRead);
                    ReadPos = secondRead;
                }
            }
            return toRead;
        }

        public bool Write(byte[] b, int offset, int toWrite)
        {
            while (toWrite > RemainingCapacity)
                return false;

            if (toWrite == 0)
                return true;

            if (WritePos < ReadPos)
            {
                Array.Copy(b, offset, buffer, WritePos, toWrite);
                WritePos += toWrite;
            }
            else
            {
                var toEnd = Capacity - WritePos;
                var firstWrite = toWrite > toEnd ?
                    toEnd :
                    toWrite;
                Array.Copy(b, offset, buffer, WritePos, firstWrite);
                var secondWrite = toWrite - firstWrite;
                if (secondWrite > 0)
                {
                    Array.Copy(b, offset + firstWrite, buffer, 0, secondWrite);
                    WritePos = secondWrite;
                }
                else
                {
                    WritePos += firstWrite;
                    if (WritePos == Capacity)
                        WritePos = 0;
                }
            }
            return true;
        }

        public void Dispose()
        {
            buffer = null;
        }
    }
}
