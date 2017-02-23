using NadekoBot.DataStructures;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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

        private const string basePath = "data/images/";

        private const string headsPath = basePath + "coins/heads.png";
        private const string tailsPath = basePath + "coins/tails.png";

        private const string currencyImagesPath = basePath + "currency";
        private const string diceImagesPath = basePath + "dice";

        private const string slotBackgroundPath = basePath + "slots/background.png";
        private const string slotNumbersPath = basePath + "slots/numbers/";
        private const string slotEmojisPath = basePath + "slots/emojis/";

        private const string _wifeMatrixPath = basePath + "rategirl/wifematrix.png";
        private const string _rategirlDot = basePath + "rategirl/dot.png";


        public ImmutableArray<byte> Heads { get; private set; }
        public ImmutableArray<byte> Tails { get; private set; }

        //todo C#7
        public ImmutableArray<KeyValuePair<string, ImmutableArray<byte>>> Currency { get; private set; }

        public ImmutableArray<KeyValuePair<string, ImmutableArray<byte>>> Dice { get; private set; }

        public ImmutableArray<byte> SlotBackground { get; private set; }
        public ImmutableArray<ImmutableArray<byte>> SlotNumbers { get; private set; }
        public ImmutableArray<ImmutableArray<byte>> SlotEmojis { get; private set; }

        public ImmutableArray<byte> WifeMatrix { get; private set; }
        public ImmutableArray<byte> RategirlDot { get; private set; }

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

        public Task<TimeSpan> Reload() => Task.Run(() =>
        {
            try
            {
                _log.Info("Loading images...");
                var sw = Stopwatch.StartNew();
                Heads = File.ReadAllBytes(headsPath).ToImmutableArray();
                Tails = File.ReadAllBytes(tailsPath).ToImmutableArray();

                Currency = Directory.GetFiles(currencyImagesPath)
                    .Select(x => new KeyValuePair<string, ImmutableArray<byte>>(
                                        Path.GetFileName(x), 
                                        File.ReadAllBytes(x).ToImmutableArray()))
                    .ToImmutableArray();

                Dice = Directory.GetFiles(diceImagesPath)
                                .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                                .Select(x => new KeyValuePair<string, ImmutableArray<byte>>(x, 
                                                    File.ReadAllBytes(x).ToImmutableArray()))
                                .ToImmutableArray();
                
                SlotBackground = File.ReadAllBytes(slotBackgroundPath).ToImmutableArray();

                SlotNumbers = Directory.GetFiles(slotNumbersPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                    .ToImmutableArray();

                SlotEmojis = Directory.GetFiles(slotEmojisPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                    .ToImmutableArray();

                WifeMatrix = File.ReadAllBytes(_wifeMatrixPath).ToImmutableArray();
                RategirlDot = File.ReadAllBytes(_rategirlDot).ToImmutableArray();

                sw.Stop();
                _log.Info($"Images loaded after {sw.Elapsed.TotalSeconds:F2}s!");
                return sw.Elapsed;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        });
    }
}