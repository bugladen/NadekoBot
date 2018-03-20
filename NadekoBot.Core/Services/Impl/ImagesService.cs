using NadekoBot.Core.Common;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Impl
{
    public class RedisImagesCache : IImageCache
    {
        private readonly ConnectionMultiplexer _con;
        private readonly IBotCredentials _creds;
        private readonly Logger _log;

        private IDatabase _db => _con.GetDatabase();

        private const string _basePath = "data/images/";

        private const string _slotBackgroundPath = _basePath + "slots/background2.png";
        private const string _slotNumbersPath = _basePath + "slots/numbers/";
        private const string _slotEmojisPath = _basePath + "slots/emojis/";

        private const string _ripPath = _basePath + "rip/rip.png";
        private const string _ripFlowersPath = _basePath + "rip/rose_overlay.png";

        private static ImageUrls realImageUrls;
        public ImageUrls ImageUrls => realImageUrls;

        public byte[][] Heads
        {
            get
            {
                return Get<byte[][]>("heads");
            }
            set
            {
                Set("heads", value);
            }
        }

        public byte[][] Tails
        {
            get
            {
                return Get<byte[][]>("tails");
            }
            set
            {
                Set("tails", value);
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
                return Get("slot_background");
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
                return Get("wife_matrix");
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
                return Get("rategirl_dot");
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
                return Get("xp_card");
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
                return Get("rip");
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
                return Get("flower_circle");
            }
            set
            {
                Set("flower_circle", value);
            }
        }

        private static readonly HttpClient _http = new HttpClient();

        static RedisImagesCache()
        {
            realImageUrls = JsonConvert.DeserializeObject<ImageUrls>(
                        File.ReadAllText(Path.Combine(_basePath, "images.json")));

        }

        public RedisImagesCache(ConnectionMultiplexer con, IBotCredentials creds)
        {
            _con = con;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();
        }

        public async Task Reload()
        {
            try
            {
                realImageUrls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

                byte[][] _heads = null;
                byte[][] _tails = null;
                byte[][] _dice = null;

                var loadCoins = Task.Run(async () =>
                {
                    _heads = await Task.WhenAll(ImageUrls.Coins.Heads
                        .Select(x => _http.GetByteArrayAsync(x)));
                    _tails = await Task.WhenAll(ImageUrls.Coins.Tails
                        .Select(x => _http.GetByteArrayAsync(x)));
                });

                var loadDice = Task.Run(async () =>
                    _dice = (await Task.WhenAll(ImageUrls.Dice
                        .Select(x => _http.GetByteArrayAsync(x))))
                        .ToArray());

                SlotBackground = File.ReadAllBytes(_slotBackgroundPath);

                SlotNumbers = Directory.GetFiles(_slotNumbersPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x))
                    .ToArray();

                SlotEmojis = Directory.GetFiles(_slotEmojisPath)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                    .Select(x => File.ReadAllBytes(x))
                    .ToArray();

                byte[] _wifeMatrix = null;
                byte[] _rategirlDot = null;
                byte[] _xpCard = null;
                var loadRategirl = Task.Run(async () =>
                {
                    _wifeMatrix = await _http.GetByteArrayAsync(ImageUrls.Rategirl.Matrix);
                    _rategirlDot = await _http.GetByteArrayAsync(ImageUrls.Rategirl.Dot);
                });

                var loadXp = Task.Run(async () => 
                    _xpCard = await _http.GetByteArrayAsync(ImageUrls.Xp.Bg)
                );

                Rip = File.ReadAllBytes(_ripPath);
                FlowerCircle = File.ReadAllBytes(_ripFlowersPath);

                await Task.WhenAll(loadCoins, loadRategirl, 
                    loadXp, loadDice);

                WifeMatrix = _wifeMatrix;
                RategirlDot = _rategirlDot;
                Heads = _heads;
                Tails = _tails;
                Dice = _dice;
                XpCard = _xpCard;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

        private byte[] Get(string key)
        {
            return _db.StringGet($"{_creds.RedisKey()}_localimg_{key}");
        }

        private void Set(string key, byte[] bytes)
        {
            _db.StringSet($"{_creds.RedisKey()}_localimg_{key}", bytes);
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