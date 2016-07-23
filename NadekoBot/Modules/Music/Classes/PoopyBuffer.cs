using System;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Music.Classes
{

    /// <summary>
    /// 💩
    /// </summary>
    public class PoopyBuffer
    {

        private readonly byte[] ringBuffer;

        public int WritePosition { get; private set; } = 0;
        public int ReadPosition { get; private set; } = 0;

        public int ContentLength => (WritePosition >= ReadPosition ?
                                     WritePosition - ReadPosition :
                                     (BufferSize - ReadPosition) + WritePosition);

        public int BufferSize { get; }

        private readonly SemaphoreSlim readWriteLock = new SemaphoreSlim(1, 1);

        public PoopyBuffer(int size)
        {
            if (size <= 0)
                throw new ArgumentException();
            BufferSize = size;
            ringBuffer = new byte[size];
        }

        public Task<int> ReadAsync(byte[] buffer, int count)
        {
            return Task.Run(async () =>
            {
                if (buffer.Length < count)
                    throw new ArgumentException();
                //Console.WriteLine($"***\nRead: {ReadPosition}\nWrite: {WritePosition}\nContentLength:{ContentLength}\n***");
                await readWriteLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    //read as much as you can if you're reading too much
                    if (count > ContentLength)
                        count = ContentLength;
                    //if nothing to read, return 0
                    if (WritePosition == ReadPosition)
                        return 0;
                    // if buffer is in the "normal" state, just read
                    if (WritePosition > ReadPosition)
                    {
                        Buffer.BlockCopy(ringBuffer, ReadPosition, buffer, 0, count);
                        ReadPosition += count;
                        //Console.WriteLine($"Read only normally1 {count}[{ReadPosition - count} to {ReadPosition}]");
                        return count;
                    }
                    //else ReadPos <Writepos
                    // buffer is in its inverted state
                    // A: if i can read as much as possible without hitting the buffer.length, read that

                    if (count + ReadPosition <= BufferSize)
                    {
                        Buffer.BlockCopy(ringBuffer, ReadPosition, buffer, 0, count);
                        ReadPosition += count;
                        //Console.WriteLine($"Read only normally2 {count}[{ReadPosition - count} to {ReadPosition}]");
                        return count;
                    }
                    // B: if i can't read as much, read to the end,
                    var readNormaly = BufferSize - ReadPosition;
                    Buffer.BlockCopy(ringBuffer, ReadPosition, buffer, 0, readNormaly);

                    //Console.WriteLine($"Read normaly {count}[{ReadPosition} to {ReadPosition + readNormaly}]");
                    //then read the remaining amount from the start

                    var readFromStart = count - readNormaly;
                    Buffer.BlockCopy(ringBuffer, 0, buffer, readNormaly, readFromStart);
                    //Console.WriteLine($"Read From start {readFromStart}[{0} to {readFromStart}]");
                    ReadPosition = readFromStart;
                    return count;
                }
                finally { readWriteLock.Release(); }
            });

        }

        public async Task WriteAsync(byte[] buffer, int count, CancellationToken cancelToken)
        {
            if (count > buffer.Length)
                throw new ArgumentException();
            while (ContentLength + count > BufferSize)
            {
                await Task.Delay(20, cancelToken).ConfigureAwait(false);
                if (cancelToken.IsCancellationRequested)
                    return;
            }
            await Task.Run(async () =>
            {
                //the while above assures that i cannot write past readposition with my write, so i don't have to check
                // *unless its multithreaded or task is not awaited
                await readWriteLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    // if i can just write without hitting buffer.length, do it
                    if (WritePosition + count < BufferSize)
                    {
                        Buffer.BlockCopy(buffer, 0, ringBuffer, WritePosition, count);
                        WritePosition += count;
                        //Console.WriteLine($"Wrote only normally {count}[{WritePosition - count} to {WritePosition}]");
                        return;
                    }
                    // otherwise, i have to write to the end, then write the rest from the start

                    var wroteNormaly = BufferSize - WritePosition;
                    Buffer.BlockCopy(buffer, 0, ringBuffer, WritePosition, wroteNormaly);

                    //Console.WriteLine($"Wrote normally {wroteNormaly}[{WritePosition} to {BufferSize}]");

                    var wroteFromStart = count - wroteNormaly;
                    Buffer.BlockCopy(buffer, wroteNormaly, ringBuffer, 0, wroteFromStart);

                    //Console.WriteLine($"and from start {wroteFromStart} [0 to {wroteFromStart}");

                    WritePosition = wroteFromStart;
                }
                finally { readWriteLock.Release(); }
            });
        }
    }
}
