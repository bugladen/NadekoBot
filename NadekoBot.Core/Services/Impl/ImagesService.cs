using NLog;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace NadekoBot.Core.Services.Impl
{
    public class ImagesService : IImagesService
    {
        private readonly Logger _log;

        private const string _basePath = "data/images/";

        private const string _headsPath = _basePath + "coins/heads.png";
        private const string _tailsPath = _basePath + "coins/tails.png";

        private const string _currencyImagesPath = _basePath + "currency";
        private const string _diceImagesPath = _basePath + "dice";

        private const string _slotBackgroundPath = _basePath + "slots/background2.png";
        private const string _slotNumbersPath = _basePath + "slots/numbers/";
        private const string _slotEmojisPath = _basePath + "slots/emojis/";

        private const string _wifeMatrixPath = _basePath + "rategirl/wifematrix.png";
        private const string _rategirlDot = _basePath + "rategirl/dot.png";

        private const string _xpCardPath = _basePath + "xp/xp.png";

        private const string _ripPath = _basePath + "rip/rip.png";
        private const string _ripFlowersPath = _basePath + "rip/rose_overlay.png";


        public ImmutableArray<byte> Heads { get; private set; }
        public ImmutableArray<byte> Tails { get; private set; }
        
        public ImmutableArray<(string, ImmutableArray<byte>)> Currency { get; private set; }

        public ImmutableArray<ImmutableArray<byte>> Dice { get; private set; }

        public ImmutableArray<byte> SlotBackground { get; private set; }
        public ImmutableArray<ImmutableArray<byte>> SlotNumbers { get; private set; }
        public ImmutableArray<ImmutableArray<byte>> SlotEmojis { get; private set; }

        public ImmutableArray<byte> WifeMatrix { get; private set; }
        public ImmutableArray<byte> RategirlDot { get; private set; }

        public ImmutableArray<byte> XpCard { get; private set; }

        public ImmutableArray<byte> Rip { get; private set; }
        public ImmutableArray<byte> FlowerCircle { get; private set; }

        public ImagesService()
        {
            _log = LogManager.GetCurrentClassLogger();
            this.Reload();
        }

        public void Reload()
        {
            try
            {
                Heads = File.ReadAllBytes(_headsPath).ToImmutableArray();
                Tails = File.ReadAllBytes(_tailsPath).ToImmutableArray();

                Currency = Directory.GetFiles(_currencyImagesPath)
                    .Select(x => (Path.GetFileName(x), File.ReadAllBytes(x).ToImmutableArray()))
                    .ToImmutableArray();

                Dice = Directory.GetFiles(_diceImagesPath)
                                .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                                .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                                .ToImmutableArray();
                
                SlotBackground = File.ReadAllBytes(_slotBackgroundPath).ToImmutableArray();

                SlotNumbers = Directory.GetFiles(_slotNumbersPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                    .ToImmutableArray();

                SlotEmojis = Directory.GetFiles(_slotEmojisPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                    .ToImmutableArray();

                WifeMatrix = File.ReadAllBytes(_wifeMatrixPath).ToImmutableArray();
                RategirlDot = File.ReadAllBytes(_rategirlDot).ToImmutableArray();

                XpCard = File.ReadAllBytes(_xpCardPath).ToImmutableArray();

                Rip = File.ReadAllBytes(_ripPath).ToImmutableArray();
                FlowerCircle = File.ReadAllBytes(_ripFlowersPath).ToImmutableArray();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }
    }
}