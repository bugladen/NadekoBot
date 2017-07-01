using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures
{
    public class PoopyRingBuffer : IDisposable
    {
        // readpos == writepos means empty
        // writepos == readpos - 1 means full 

        private readonly byte[] buffer;
        private readonly object posLock = new object();
        public int Capacity { get; }

        private volatile int _readPos = 0;
        private int ReadPos
        {
            get => _readPos;
            set { lock (posLock) _readPos = value; }
        }
        private volatile int _writePos = 0;
        private int WritePos
        {
            get => _writePos;
            set { lock (posLock) _writePos = value; }
        }
        private int Length
        {
            get
            {
                lock (posLock)
                {
                    return ReadPos <= WritePos ?
                        WritePos - ReadPos :
                        Capacity - (ReadPos - WritePos);
                }
            }
        }

        public int RemainingCapacity
        {
            get { lock (posLock) return Capacity - Length - 1; }
        }

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        public PoopyRingBuffer(int capacity = 3640 * 200)
        {
            this.Capacity = capacity + 1;
            this.buffer = new byte[this.Capacity];
        }

        public Task<int> ReadAsync(byte[] b, int offset, int toRead, CancellationToken cancelToken) => Task.Run(async () =>
        {
            await _locker.WaitAsync(cancelToken);
            try
            {
                Console.WriteLine("Reading {0}", toRead);
                if (WritePos == ReadPos)
                    return 0;

                if (toRead > Length)
                    toRead = Length;

                if (WritePos > ReadPos)
                {
                    Buffer.BlockCopy(buffer, ReadPos, b, offset, toRead);
                    ReadPos += toRead;
                }
                else
                {
                    var toEnd = Capacity - ReadPos;
                    var firstRead = toRead > toEnd ?
                        toEnd :
                        toRead;
                    Buffer.BlockCopy(buffer, ReadPos, b, offset, firstRead);
                    ReadPos += firstRead;
                    var secondRead = toRead - firstRead;
                    if (secondRead > 0)
                    {
                        Buffer.BlockCopy(buffer, 0, b, offset + firstRead, secondRead);
                        ReadPos = secondRead;
                    }
                }
                Console.WriteLine("Readpos: {0} WritePos: {1}", ReadPos, WritePos);
                return toRead;
            }
            finally
            {
                _locker.Release();
            }
        });

        public Task WriteAsync(byte[] b, int offset, int toWrite, CancellationToken cancelToken) => Task.Run(async () =>
        {
            while (toWrite > RemainingCapacity)
                await Task.Delay(100, cancelToken);

            await _locker.WaitAsync(cancelToken);
            try
            {
                Console.WriteLine("Writing {0}", toWrite);
                if (WritePos < ReadPos)
                {
                    Buffer.BlockCopy(b, offset, buffer, WritePos, toWrite);
                    WritePos += toWrite;
                }
                else
                {
                    var toEnd = Capacity - WritePos;
                    var firstWrite = toWrite > toEnd ?
                        toEnd :
                        toWrite;
                    Buffer.BlockCopy(b, offset, buffer, WritePos, firstWrite);
                    var secondWrite = toWrite - firstWrite;
                    if (secondWrite > 0)
                    {
                        Buffer.BlockCopy(b, offset + firstWrite, buffer, 0, secondWrite);
                        WritePos = secondWrite;
                    }
                    else
                    {
                        WritePos += firstWrite;
                        if (WritePos == Capacity)
                            WritePos = 0;
                    }
                }
                Console.WriteLine("Readpos: {0} WritePos: {1}", ReadPos, WritePos);
                return toWrite;
            }
            finally
            {
                _locker.Release();
            }
        });

        public void Dispose()
        {
        }
    }
}
