using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Music.Classes
{
    /// <summary>
    /// Create a buffer for a song file. It will create multiples files to ensure, that radio don't fill up disk space.
    /// It also help for large music by deleting files that are already seen.
    /// </summary>
    class SongBuffer
    {

        public SongBuffer(string basename, SongInfo songInfo, int skipTo)
        {
            Basename = basename;
            SongInfo = songInfo;
            SkipTo = skipTo;
        }

        private string Basename;

        private SongInfo SongInfo;

        private int SkipTo;

        private static int MAX_FILE_SIZE = 20.MiB();

        private long FileNumber = -1;

        private long NextFileToRead = 0;

        public bool BufferingCompleted { get; private set;} = false;

        private ulong CurrentBufferSize = 0;

        public Task BufferSong(CancellationToken cancelToken) =>
           Task.Factory.StartNew(async () =>
           {
               Process p = null;
               FileStream outStream = null;
               try
               {
                   p = Process.Start(new ProcessStartInfo
                   {
                       FileName = "ffmpeg",
                       Arguments = $"-ss {SkipTo} -i {SongInfo.Uri} -f s16le -ar 48000 -ac 2 pipe:1 -loglevel quiet",
                       UseShellExecute = false,
                       RedirectStandardOutput = true,
                       RedirectStandardError = false,
                       CreateNoWindow = true,
                   });

                   byte[] buffer = new byte[81920];
                   int currentFileSize = 0;
                   ulong prebufferSize = 100ul.MiB();

                   outStream = new FileStream(Basename + "-" + ++FileNumber, FileMode.Append, FileAccess.Write, FileShare.Read);
                   while (!p.HasExited) //Also fix low bandwidth
                   {
                       int bytesRead = await p.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false);
                       if (currentFileSize >= MAX_FILE_SIZE)
                       {
                           try
                           {
                               outStream.Dispose();
                           }catch { }
                           outStream = new FileStream(Basename + "-" + ++FileNumber, FileMode.Append, FileAccess.Write, FileShare.Read);
                           currentFileSize = bytesRead;
                       }
                       else
                       {
                           currentFileSize += bytesRead;
                       }
                       CurrentBufferSize += Convert.ToUInt64(bytesRead);
                       await outStream.WriteAsync(buffer, 0, bytesRead, cancelToken).ConfigureAwait(false);
                       while (CurrentBufferSize > prebufferSize)
                           await Task.Delay(100, cancelToken);
                   }
                   BufferingCompleted = true;
               }
               catch (System.ComponentModel.Win32Exception)
               {
                   var oldclr = Console.ForegroundColor;
                   Console.ForegroundColor = ConsoleColor.Red;
                   Console.WriteLine(@"You have not properly installed or configured FFMPEG. 
Please install and configure FFMPEG to play music. 
Check the guides for your platform on how to setup ffmpeg correctly:
    Windows Guide: https://goo.gl/SCv72y
    Linux Guide:  https://goo.gl/rRhjCp");
                   Console.ForegroundColor = oldclr;
               }
               catch (Exception ex)
               {
                   Console.WriteLine($"Buffering stopped: {ex.Message}");
               }
               finally
               {
                   if(outStream != null)
                        outStream.Dispose();
                   Console.WriteLine($"Buffering done.");
                   if (p != null)
                   {
                       try
                       {
                           p.Kill();
                       }
                       catch { }
                       p.Dispose();
                   }
               }
           }, TaskCreationOptions.LongRunning);

        /// <summary>
        /// Return the next file to read, and delete the old one
        /// </summary>
        /// <returns>Name of the file to read</returns>
        public string GetNextFile()
        {
            string filename = Basename + "-" + NextFileToRead;
            
            if (NextFileToRead != 0)
            {
                try
                {
                    CurrentBufferSize -= Convert.ToUInt64(new FileInfo(Basename + "-" + (NextFileToRead - 1)).Length);
                    File.Delete(Basename + "-" + (NextFileToRead - 1));
                }
                catch { }
            }
            NextFileToRead++;
            return filename;
        }

        public bool IsNextFileReady()
        {
            return NextFileToRead <= FileNumber;
        }

        public void CleanFiles()
        {
            for (long i = NextFileToRead - 1 ; i <= FileNumber; i++)
            {
                try
                {
                    File.Delete(Basename + "-" + i);
                }
                catch { }
            }
        }
    }
}
