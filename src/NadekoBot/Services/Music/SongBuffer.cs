using NadekoBot.DataStructures;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music
{
    public class SongBuffer : IDisposable
    {
        const int readSize = 38400;
        private Process p;
        private PoopyRingBuffer _outStream = new PoopyRingBuffer();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public string SongUri { get; private set; }

        public SongBuffer(string songUri, string skipTo)
        {
            this.SongUri = songUri;

            this.p = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {songUri} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel quiet -nostdin",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
            });
        }

        public Task<bool> StartBuffering(CancellationToken cancelToken)
        {
            var toReturn = new TaskCompletionSource<bool>();
            var _ = Task.Run(async () =>
            {
                try
                {
                    byte[] buffer = new byte[readSize];
                    while (!cancelToken.IsCancellationRequested)
                    {
                        var toRead = buffer.Length;
                        var remCap = _outStream.RemainingCapacity;
                        if (remCap < readSize)
                        {
                            if (_outStream.RemainingCapacity == 0)
                            {
                                Console.WriteLine("Buffer full, not gonnna read from ffmpeg");
                                await Task.Delay(20);
                                continue;
                            }
                            toRead = remCap;
                        }

                        int bytesRead = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, toRead, cancelToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("I'm not reading anything from ffmpeg");
                            await Task.Delay(20);
                            continue;
                        }
                        await _outStream.WriteAsync(buffer, 0, bytesRead, cancelToken);

                        if (_outStream.RemainingCapacity < _outStream.Capacity * 0.9f)
                            if (toReturn.TrySetResult(true))
                                Console.WriteLine("Prebuffering finished");
                    }
                    Console.WriteLine("FFMPEG killed or song canceled");
                }
                catch
                {
                    if(toReturn.TrySetResult(false))
                        Console.WriteLine("Prebuffering failed");
                    //ignored
                }
            }, cancelToken);

            return toReturn.Task;
        }

        public Task<int> ReadAsync(byte[] b, int offset, int toRead, CancellationToken cancelToken)
        {
            return _outStream.ReadAsync(b, offset, toRead, cancelToken);
        }

        public void Dispose()
        {
            try { this.p.Kill(); }
            catch { }
            _outStream.Dispose();
            this.p.Dispose();
        }
    }
}

//namespace NadekoBot.Services.Music
//{
//    /// <summary>
//    /// Create a buffer for a song file. It will create multiples files to ensure, that radio don't fill up disk space.
//    /// It also help for large music by deleting files that are already seen.
//    /// </summary>
//    class SongBuffer : Stream
//    {
//        public SongBuffer(MusicPlayer musicPlayer, string basename, SongInfo songInfo, int skipTo, int maxFileSize)
//        {
//            MusicPlayer = musicPlayer;
//            Basename = basename;
//            SongInfo = songInfo;
//            SkipTo = skipTo;
//            MaxFileSize = maxFileSize;
//            CurrentFileStream = new FileStream(this.GetNextFile(), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);
//            _log = LogManager.GetCurrentClassLogger();
//        }

//        MusicPlayer MusicPlayer { get; }

//        private string Basename { get; }

//        private SongInfo SongInfo { get; }

//        private int SkipTo { get; }

//        private int MaxFileSize { get; } = 2.MiB();

//        private long FileNumber = -1;

//        private long NextFileToRead = 0;

//        public bool BufferingCompleted { get; private set; } = false;

//        private ulong CurrentBufferSize = 0;

//        private FileStream CurrentFileStream;
//        private Logger _log;

//        public Task BufferSong(CancellationToken cancelToken) =>
//           Task.Run(async () =>
//           {
//               Process p = null;
//               FileStream outStream = null;
//               try
//               {
//                   p = Process.Start(new ProcessStartInfo
//                   {
//                       FileName = "ffmpeg",
//                       Arguments = $"-ss {SkipTo} -i {SongInfo.Uri} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel quiet",
//                       UseShellExecute = false,
//                       RedirectStandardOutput = true,
//                       RedirectStandardError = false,
//                       CreateNoWindow = true,
//                   });

//                   byte[] buffer = new byte[81920];
//                   int currentFileSize = 0;
//                   ulong prebufferSize = 100ul.MiB();

//                   outStream = new FileStream(Basename + "-" + ++FileNumber, FileMode.Append, FileAccess.Write, FileShare.Read);
//                   while (!p.HasExited) //Also fix low bandwidth
//                   {
//                       int bytesRead = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false);
//                       if (currentFileSize >= MaxFileSize)
//                       {
//                           try
//                           {
//                               outStream.Dispose();
//                           }
//                           catch { }
//                           outStream = new FileStream(Basename + "-" + ++FileNumber, FileMode.Append, FileAccess.Write, FileShare.Read);
//                           currentFileSize = bytesRead;
//                       }
//                       else
//                       {
//                           currentFileSize += bytesRead;
//                       }
//                       CurrentBufferSize += Convert.ToUInt64(bytesRead);
//                       await outStream.WriteAsync(buffer, 0, bytesRead, cancelToken).ConfigureAwait(false);
//                       while (CurrentBufferSize > prebufferSize)
//                           await Task.Delay(100, cancelToken);
//                   }
//                   BufferingCompleted = true;
//               }
//               catch (System.ComponentModel.Win32Exception)
//               {
//                   var oldclr = Console.ForegroundColor;
//                   Console.ForegroundColor = ConsoleColor.Red;
//                   Console.WriteLine(@"You have not properly installed or configured FFMPEG. 
//Please install and configure FFMPEG to play music. 
//Check the guides for your platform on how to setup ffmpeg correctly:
//    Windows Guide: https://goo.gl/OjKk8F
//    Linux Guide:  https://goo.gl/ShjCUo");
//                   Console.ForegroundColor = oldclr;
//               }
//               catch (Exception ex)
//               {
//                   Console.WriteLine($"Buffering stopped: {ex.Message}");
//               }
//               finally
//               {
//                   if (outStream != null)
//                       outStream.Dispose();
//                   Console.WriteLine($"Buffering done.");
//                   if (p != null)
//                   {
//                       try
//                       {
//                           p.Kill();
//                       }
//                       catch { }
//                       p.Dispose();
//                   }
//               }
//           });

//        /// <summary>
//        /// Return the next file to read, and delete the old one
//        /// </summary>
//        /// <returns>Name of the file to read</returns>
//        private string GetNextFile()
//        {
//            string filename = Basename + "-" + NextFileToRead;

//            if (NextFileToRead != 0)
//            {
//                try
//                {
//                    CurrentBufferSize -= Convert.ToUInt64(new FileInfo(Basename + "-" + (NextFileToRead - 1)).Length);
//                    File.Delete(Basename + "-" + (NextFileToRead - 1));
//                }
//                catch { }
//            }
//            NextFileToRead++;
//            return filename;
//        }

//        private bool IsNextFileReady()
//        {
//            return NextFileToRead <= FileNumber;
//        }

//        private void CleanFiles()
//        {
//            for (long i = NextFileToRead - 1; i <= FileNumber; i++)
//            {
//                try
//                {
//                    File.Delete(Basename + "-" + i);
//                }
//                catch { }
//            }
//        }

//        //Stream part

//        public override bool CanRead => true;

//        public override bool CanSeek => false;

//        public override bool CanWrite => false;

//        public override long Length => (long)CurrentBufferSize;

//        public override long Position { get; set; } = 0;

//        public override void Flush() { }

//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            int read = CurrentFileStream.Read(buffer, offset, count);
//            if (read < count)
//            {
//                if (!BufferingCompleted || IsNextFileReady())
//                {
//                    CurrentFileStream.Dispose();
//                    CurrentFileStream = new FileStream(GetNextFile(), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);
//                    read += CurrentFileStream.Read(buffer, read + offset, count - read);
//                }
//                if (read < count)
//                    Array.Clear(buffer, read, count - read);
//            }
//            return read;
//        }

//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            throw new NotImplementedException();
//        }

//        public override void SetLength(long value)
//        {
//            throw new NotImplementedException();
//        }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            throw new NotImplementedException();
//        }

//        public new void Dispose()
//        {
//            CurrentFileStream.Dispose();
//            MusicPlayer.SongCancelSource.Cancel();
//            CleanFiles();
//            base.Dispose();
//        }
//    }
//}