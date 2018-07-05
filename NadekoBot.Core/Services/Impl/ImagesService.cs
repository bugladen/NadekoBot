using NadekoBot.Core.Common;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Diagnostics;
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

        private const string _basePath = "data/";
        private const string _oldBasePath = "data/images/";

        public ImageUrls ImageUrls { get; private set; }

        private const string _ripPath = _basePath + "rip/rip.png";
        private const string _ripFlowersPath = _basePath + "rip/rose_overlay.png";

        #region Getters and Setters
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
        public byte[] RipOverlay
        {
            get
            {
                return Get("rip_overlay");
            }
            set
            {
                Set("rip_overlay", value);
            }
        }
        #endregion

        private static readonly HttpClient _http = new HttpClient();

        public RedisImagesCache(ConnectionMultiplexer con, IBotCredentials creds)
        {
            _con = con;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();

            Migrate();
            ImageUrls = JsonConvert.DeserializeObject<ImageUrls>(
                        File.ReadAllText(Path.Combine(_basePath, "images.json")));
        }

        private void Migrate()
        {
            Migrate1();
        }

        private void Migrate1()
        {
            if (!File.Exists(Path.Combine(_oldBasePath, "images.json")))
                return;
            // load old images
            var oldUrls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_oldBasePath, "images.json")));
            // load new images
            var newUrls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

            //swap new links with old ones if set. Also update old links.
            newUrls.Coins = oldUrls.Coins;

            newUrls.Currency = oldUrls.Currency;
            newUrls.Dice = oldUrls.Dice;
            newUrls.Rategirl = oldUrls.Rategirl;
            newUrls.Xp = oldUrls.Xp;
            newUrls.Version = 1;

            File.WriteAllText(Path.Combine(_basePath, "images.json"), JsonConvert.SerializeObject(newUrls, Formatting.Indented));
            File.Delete((Path.Combine(_oldBasePath, "images.json")));
        }

        public async Task<bool> AllKeysExist()
        {
            try
            {
                var prefix = $"{_creds.RedisKey()}_localimg_";
                var results = await Task.WhenAll(_db.KeyExistsAsync(prefix + "heads"),
                    _db.KeyExistsAsync(prefix + "tails"),
                    _db.KeyExistsAsync(prefix + "dice"),
                    _db.KeyExistsAsync(prefix + "slot_background"),
                    _db.KeyExistsAsync(prefix + "slotnumbers"),
                    _db.KeyExistsAsync(prefix + "slotemojis"),
                    _db.KeyExistsAsync(prefix + "wife_matrix"),
                    _db.KeyExistsAsync(prefix + "rategirl_dot"),
                    _db.KeyExistsAsync(prefix + "xp_card"),
                    _db.KeyExistsAsync(prefix + "rip"),
                    _db.KeyExistsAsync(prefix + "rip_overlay"));

                return results.All(x => x);
            }
            catch(Exception ex)
            {
                _log.Warn(ex);
                return false;
            }
        }

        public async Task Reload()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                ImageUrls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

                byte[][] _heads = null;
                byte[][] _tails = null;
                byte[][] _dice = null;

                var loadCoins = Task.Run(async () =>
                {
                    _heads = await Task.WhenAll(ImageUrls.Coins.Heads
                        .Select(x => _http.GetByteArrayAsync(x))).ConfigureAwait(false);
                    _tails = await Task.WhenAll(ImageUrls.Coins.Tails
                        .Select(x => _http.GetByteArrayAsync(x))).ConfigureAwait(false);
                });

                var loadDice = Task.Run(async () =>
                    _dice = (await Task.WhenAll(ImageUrls.Dice
                        .Select(x => _http.GetByteArrayAsync(x))).ConfigureAwait(false))
                        .ToArray());

                byte[][] _slotNumbers = null;
                byte[][] _slotEmojis = null;
                byte[] _slotBackground = null;
                var loadSlot = Task.Run(async () =>
                {
                    _slotNumbers = (await Task.WhenAll(ImageUrls.Slots.Numbers
                        .Select(x => _http.GetByteArrayAsync(x))).ConfigureAwait(false))
                        .ToArray();

                    _slotEmojis = (await Task.WhenAll(ImageUrls.Slots.Emojis
                        .Select(x => _http.GetByteArrayAsync(x))).ConfigureAwait(false))
                        .ToArray();

                    _slotBackground = await _http.GetByteArrayAsync(ImageUrls.Slots.Bg).ConfigureAwait(false);
                });

                byte[] _wifeMatrix = null;
                byte[] _rategirlDot = null;
                byte[] _xpCard = null;
                var loadRategirl = Task.Run(async () =>
                {
                    _wifeMatrix = await _http.GetByteArrayAsync(ImageUrls.Rategirl.Matrix).ConfigureAwait(false);
                    _rategirlDot = await _http.GetByteArrayAsync(ImageUrls.Rategirl.Dot).ConfigureAwait(false);
                });

                var loadXp = Task.Run(async () =>
                    _xpCard = await _http.GetByteArrayAsync(ImageUrls.Xp.Bg).ConfigureAwait(false)
                );

                byte[] _rip = null;
                byte[] _overlay = null;
                var loadOther = Task.Run(async () =>
                {
                    _rip = await _http.GetByteArrayAsync(ImageUrls.Rip.Bg).ConfigureAwait(false);
                    _overlay = await _http.GetByteArrayAsync(ImageUrls.Rip.Overlay).ConfigureAwait(false);
                });

                await Task.WhenAll(loadCoins, loadRategirl,
                    loadXp, loadDice, loadSlot, loadOther).ConfigureAwait(false);

                WifeMatrix = _wifeMatrix;
                RategirlDot = _rategirlDot;
                Heads = _heads;
                Tails = _tails;
                Dice = _dice;
                XpCard = _xpCard;
                SlotNumbers = _slotNumbers;
                SlotBackground = _slotBackground;
                SlotEmojis = _slotEmojis;
                Rip = _rip;
                RipOverlay = _overlay;
                sw.Stop();
                _log.Info($"Images reloaded in {sw.Elapsed.TotalSeconds:F2}s");
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