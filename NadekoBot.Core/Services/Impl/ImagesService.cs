using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Linq;

namespace NadekoBot.Core.Services.Impl
{
    public class RedisImagesCache : IImageCache
    {
        private readonly ConnectionMultiplexer _con;
        private readonly IBotCredentials _creds;
        private readonly Logger _log;

        private IDatabase _db => _con.GetDatabase();

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

        public byte[] Heads
        {
            get
            {
                return Get<byte[]>("heads");
            }
            set
            {
                Set("heads", value);
            }
        }

        public byte[] Tails
        {
            get
            {
                return Get<byte[]>("tails");
            }
            set
            {
                Set("tails", value);
            }
        }

        public byte[][] Currency
        {
            get
            {
                return Get<byte[][]>("currency");
            }
            set
            {
                Set("currency", value);
            }
        }

        public byte[][] Dice
        {
            get
            {
                return Get<byte[][]>("dice");
            }
            set
            {
                Set("dice", value);
            }
        }

        public byte[] SlotBackground
        {
            get
            {
                return Get<byte[]>("slot_background");
            }
            set
            {
                Set("slot_background", value);
            }
        }

        public byte[][] SlotNumbers
        {
            get
            {
                return Get<byte[][]>("slotnumbers");
            }
            set
            {
                Set("slotnumbers", value);
            }
        }
        public byte[][] SlotEmojis
        {
            get
            {
                return Get<byte[][]>("slotemojis");
            }
            set
            {
                Set("slotemojis", value);
            }
        }

        public byte[] WifeMatrix
        {
            get
            {
                return Get<byte[]>("wife_matrix");
            }
            set
            {
                Set("wife_matrix", value);
            }
        }
        public byte[] RategirlDot
        {
            get
            {
                return Get<byte[]>("rategirl_dot");
            }
            set
            {
                Set("rategirl_dot", value);
            }
        }

        public byte[] XpCard
        {
            get
            {
                return Get<byte[]>("xp_card");
            }
            set
            {
                Set("xp_card", value);
            }
        }

        public byte[] Rip
        {
            get
            {
                return Get<byte[]>("rip");
            }
            set
            {
                Set("rip", value);
            }
        }
        public byte[] FlowerCircle
        {
            get
            {
                return Get<byte[]>("flower_circle");
            }
            set
            {
                Set("flower_circle", value);
            }
        }

        public RedisImagesCache(ConnectionMultiplexer con, IBotCredentials creds)
        {
            _con = con;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();
        }

        public void Reload()
        {
            try
            {
                Heads = File.ReadAllBytes(_headsPath);
                Tails = File.ReadAllBytes(_tailsPath);

                Currency = Directory.GetFiles(_currencyImagesPath)
                    .Select(x => File.ReadAllBytes(x))
                    .ToArray();

                Dice = Directory.GetFiles(_diceImagesPath)
                                .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                                .Select(x => File.ReadAllBytes(x))
                                .ToArray();
                
                SlotBackground = File.ReadAllBytes(_slotBackgroundPath);

                SlotNumbers = Directory.GetFiles(_slotNumbersPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x))
                    .ToArray();

                SlotEmojis = Directory.GetFiles(_slotEmojisPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x))
                    .ToArray();

                WifeMatrix = File.ReadAllBytes(_wifeMatrixPath);
                RategirlDot = File.ReadAllBytes(_rategirlDot);

                XpCard = File.ReadAllBytes(_xpCardPath);

                Rip = File.ReadAllBytes(_ripPath);
                FlowerCircle = File.ReadAllBytes(_ripFlowersPath);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

        private T Get<T>(string key) where T : class
        {
            return JsonConvert.DeserializeObject<T>(_db.StringGet($"{_creds.RedisKey()}_localimg_{key}"));
        }

        private void Set(string key, object obj)
        {
            _db.StringSet($"{_creds.RedisKey()}_localimg_{key}", JsonConvert.SerializeObject(obj));
        }
    }
}