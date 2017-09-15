using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NadekoBot.Modules.Music.Common
{
    public class SongBuffer : IDisposable
    {
        const int readSize = 81920;
        private Process p;
        private Stream _outStream;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Logger _log;

        public string SongUri { get; private set; }

        public SongBuffer(string songUri, string skipTo, bool isLocal)
        {
            _log = LogManager.GetCurrentClassLogger();
            this.SongUri = songUri;
            this._isLocal = isLocal;

            try
            {
                this.p = StartFFmpegProcess(SongUri, 0);
                this._outStream = this.p.StandardOutput.BaseStream;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _log.Error(@"You have not properly installed or configured FFMPEG. 
Please install and configure FFMPEG to play music. 
Check the guides for your platform on how to setup ffmpeg correctly:
    Windows Guide: https://goo.gl/OjKk8F
    Linux Guide:  https://goo.gl/ShjCUo");
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { } // when ffmpeg is disposed
            catch (Exception ex)
            {
                _log.Info(ex);
            }
        }

        private Process StartFFmpegProcess(string songUri, float skipTo = 0)
        {
            var args = $"-err_detect ignore_err -i {songUri} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error";
            if (!_isLocal)
                args = "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " + args;

            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
            });
        }

        private readonly object locker = new object();
        private readonly bool _isLocal;

        public int Read(byte[] b, int offset, int toRead)
        {
            lock (locker)
                return _outStream.Read(b, offset, toRead);
        }

        public void Dispose()
        {
            try
            {
                this.p.StandardOutput.Dispose();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            try
            {
                if(!this.p.HasExited)
                    this.p.Kill();
            }
            catch
            {
            }
            _outStream.Dispose();
            this.p.Dispose();
        }
    }
}