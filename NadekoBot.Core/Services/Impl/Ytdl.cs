using NLog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Impl
{
    public class YtdlOperation : IDisposable
    {
        private readonly Logger _log;

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
                    Arguments = $"-4 --geo-bypass -f bestaudio -e --get-url --get-id --get-thumbnail --get-duration --no-check-certificate --default-search \"ytsearch:\" \"{url}\"",
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
                if(!string.IsNullOrEmpty(err))
                    _log.Warn(err);
                return str;
            }
        }

        public void Dispose()
        {

        }
    }
}
