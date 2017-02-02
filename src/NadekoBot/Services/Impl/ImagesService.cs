using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class ImagesService : IImagesService
    {
        private readonly Logger _log;

        private const string headsPath = "data/images/coins/heads.png";
        private const string tailsPath = "data/images/coins/tails.png";

        private byte[] heads;
        public Stream Heads => new MemoryStream(heads, false);

        private byte[] tails;
        public Stream Tails => new MemoryStream(tails, false);

        private ImagesService()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public static async Task<IImagesService> Create()
        {
            var srvc = new ImagesService();
            await srvc.Reload().ConfigureAwait(false);
            return srvc;
        }

        public Task Reload() => Task.Run(() =>
        {
            _log.Info("Loading images...");
            var sw = Stopwatch.StartNew();
            heads = File.ReadAllBytes(headsPath);
            tails = File.ReadAllBytes(tailsPath);
            _log.Info($"Images loaded after {sw.Elapsed.TotalSeconds:F2}s!");
        });
    }
}