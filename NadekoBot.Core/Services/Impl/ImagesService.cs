﻿using NadekoBot.Core.Common;
using NadekoBot.Core.Services.Common;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Impl
{
    public sealed class RedisImagesCache : IImageCache
    {
        private readonly ConnectionMultiplexer _con;
        private readonly IBotCredentials _creds;
        private readonly Logger _log;

        private IDatabase _db => _con.GetDatabase();

        private const string _basePath = "data/";
        private const string _oldBasePath = "data/images/";
        private const string _cardsPath = "data/images/cards";

        public ImageUrls ImageUrls { get; private set; }

        public IReadOnlyList<byte[]> Heads => GetByteArrayData(ImageKey.Coins_Heads);

        public IReadOnlyList<byte[]> Tails => GetByteArrayData(ImageKey.Coins_Tails);

        public IReadOnlyList<byte[]> Dice => GetByteArrayData(ImageKey.Dice);

        public IReadOnlyList<byte[]> SlotEmojis => GetByteArrayData(ImageKey.Slots_Emojis);

        public IReadOnlyList<byte[]> SlotNumbers => GetByteArrayData(ImageKey.Slots_Numbers);

        public IReadOnlyList<byte[]> Currency => GetByteArrayData(ImageKey.Currency);

        public byte[] SlotBackground => GetByteData(ImageKey.Slots_Bg);

        public byte[] RategirlMatrix => GetByteData(ImageKey.Rategirl_Matrix);

        public byte[] RategirlDot => GetByteData(ImageKey.Rategirl_Dot);

        public byte[] XpBackground => GetByteData(ImageKey.Xp_Bg);

        public byte[] Rip => GetByteData(ImageKey.Rip_Bg);

        public byte[] RipOverlay => GetByteData(ImageKey.Rip_Overlay);

        public byte[] GetCard(string key)
        {
            return _con.GetDatabase().StringGet(GetKey("card_" + key));
        }

        public enum ImageKey
        {
            Coins_Heads,
            Coins_Tails,
            Dice,
            Slots_Bg,
            Slots_Numbers,
            Slots_Emojis,
            Rategirl_Matrix,
            Rategirl_Dot,
            Xp_Bg,
            Rip_Bg,
            Rip_Overlay,
            Currency,
        }

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
            Migrate2();
            Migrate3();
        }

        private void Migrate1()
        {
            if (!File.Exists(Path.Combine(_oldBasePath, "images.json")))
                return;
            _log.Info("Migrating images v0 to images v1.");
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

        private void Migrate2()
        {
            // load new images
            var urls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

            if (urls.Version >= 2)
                return;
            _log.Info("Migrating images v1 to images v2.");
            urls.Version = 2;

            var prefix = $"{_creds.RedisKey()}_localimg_";
            _db.KeyDelete(new[] {
                prefix + "heads",
                prefix + "tails",
                prefix + "dice",
                prefix + "slot_background",
                prefix + "slotnumbers",
                prefix + "slotemojis",
                prefix + "wife_matrix",
                prefix + "rategirl_dot",
                prefix + "xp_card",
                prefix + "rip",
                prefix + "rip_overlay" }
            .Select(x => (RedisKey)x).ToArray());

            File.WriteAllText(Path.Combine(_basePath, "images.json"), JsonConvert.SerializeObject(urls, Formatting.Indented));
        }

        private void Migrate3()
        {
            var urls = JsonConvert.DeserializeObject<ImageUrls>(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

            if (urls.Version >= 3)
                return;
            urls.Version = 3;
            _log.Info("Migrating images v2 to images v3.");

            var baseStr = "https://nadeko-pictures.nyc3.digitaloceanspaces.com/other/currency/";

            var replacementTable = new Dictionary<Uri, Uri>()
            {
                {new Uri(baseStr + "0.jpg"), new Uri(baseStr + "0.png") },
                {new Uri(baseStr + "1.jpg"), new Uri(baseStr + "1.png") },
                {new Uri(baseStr + "2.jpg"), new Uri(baseStr + "2.png") }
            };

            if (replacementTable.Keys.Any(x => urls.Currency.Contains(x)))
            {
                urls.Currency = urls.Currency.Select(x => replacementTable.TryGetValue(x, out var newUri)
                    ? newUri
                    : x).Append(new Uri(baseStr + "3.png"))
                    .ToArray();
            }

            File.WriteAllText(Path.Combine(_basePath, "images.json"), JsonConvert.SerializeObject(urls, Formatting.Indented));
        }

        public async Task<bool> AllKeysExist()
        {
            try
            {
                var results = await Task.WhenAll(Enum.GetNames(typeof(ImageKey))
                    .Select(x => x.ToLowerInvariant())
                    .Select(x => _db.KeyExistsAsync(GetKey(x))))
                    .ConfigureAwait(false);

                var cardsExist = await Task.WhenAll(GetAllCardNames()
                    .Select(x => "card_" + x)
                    .Select(x => _db.KeyExistsAsync(GetKey(x))))
                    .ConfigureAwait(false);

                var num = results.Where(x => !x).Count();

                return results.All(x => x) && cardsExist.All(x => x);
            }
            catch (Exception ex)
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
                var obj = JObject.Parse(
                    File.ReadAllText(Path.Combine(_basePath, "images.json")));

                ImageUrls = obj.ToObject<ImageUrls>();
                using (var http = new HttpClient())
                {
                    var t = new ImageLoader(http, _con, GetKey)
                        .LoadAsync(obj);

                    var loadCards = Task.Run(async () =>
                    {
                        await _db.StringSetAsync(Directory.GetFiles(_cardsPath)
                            .ToDictionary(
                                x => GetKey("card_" + Path.GetFileNameWithoutExtension(x)),
                                x => (RedisValue)File.ReadAllBytes(x)) // loads them and creates <name, bytes> pairs to store in redis
                            .ToArray())
                            .ConfigureAwait(false);
                    });

                    await Task.WhenAll(t, loadCards).ConfigureAwait(false);
                }

                sw.Stop();
                _log.Info($"Images reloaded in {sw.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

        private IEnumerable<string> GetAllCardNames(bool showExtension = false)
        {
            return Directory.GetFiles(_cardsPath) // gets all cards from the cards folder
                           .Select(x => showExtension
                                ? Path.GetFileName(x)
                                : Path.GetFileNameWithoutExtension(x)); // gets their names
        }

        public RedisKey GetKey(string key)
        {
            return $"{_creds.RedisKey()}_localimg_{key.ToLowerInvariant()}";
        }

        public byte[] GetByteData(string key)
        {
            return _db.StringGet(GetKey(key));
        }

        public byte[] GetByteData(ImageKey key) => GetByteData(key.ToString());

        public RedisImageArray GetByteArrayData(string key)
        {
            return new RedisImageArray(GetKey(key), _con);
        }

        public RedisImageArray GetByteArrayData(ImageKey key) => GetByteArrayData(key.ToString());
    }
}