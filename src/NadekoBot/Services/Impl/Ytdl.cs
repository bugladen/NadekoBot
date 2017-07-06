using NLog;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class YtdlOperation : IDisposable
    {
        private readonly TaskCompletionSource<string> _endedCompletionSource;
        private string output { get; set; }

        public YtdlOperation()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public async Task<string> GetDataAsync(string url)
        {
            using (Process process = new Process()
            {

                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"-f bestaudio -e --get-url --get-id --get-thumbnail --get-duration \"ytsearch:{url}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {
                process.Start();
                var str = await process.StandardOutput.ReadToEndAsync();
                var err = await process.StandardError.ReadToEndAsync();
                _log.Info(str);
                _log.Info(err);
                return str;
            }
        }

        private int cnt;
        private Timer _timeoutTimer;
        private Process p;
        private readonly Logger _log;

        public void Dispose()
        {
            //_timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            //_timeoutTimer.Dispose();
            try { this.p?.Kill(); } catch { }
        }
    }
}
