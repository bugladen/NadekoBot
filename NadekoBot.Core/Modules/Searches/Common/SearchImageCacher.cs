using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NadekoBot.Common;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;

namespace NadekoBot.Modules.Searches.Common
{
    public class SearchImageCacher
    {
        private readonly NadekoRandom _rng;
        private readonly ConcurrentDictionary<DapiSearchType, SemaphoreSlim> _locks = new ConcurrentDictionary<DapiSearchType, SemaphoreSlim>();

        private readonly SortedSet<ImageCacherObject> _cache;
        private readonly Logger _log;
        private readonly IHttpClientFactory _httpFactory;

        public SearchImageCacher(IHttpClientFactory factory)
        {
            _log = LogManager.GetCurrentClassLogger();
            _rng = new NadekoRandom();
            _cache = new SortedSet<ImageCacherObject>();
            _httpFactory = factory;
        }

        public async Task<ImageCacherObject> GetImage(string tag, bool forceExplicit, DapiSearchType type,
            HashSet<string> blacklistedTags = null)
        {
            tag = tag?.ToLowerInvariant();

            blacklistedTags = blacklistedTags ?? new HashSet<string>();

            if (type == DapiSearchType.E621)
                tag = tag?.Replace("yuri", "female/female", StringComparison.InvariantCulture);

            var _lock = GetLock(type);
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                ImageCacherObject[] imgs;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    imgs = _cache.Where(x => x.Tags.IsSupersetOf(tag.Split('+')) && x.SearchType == type && (!forceExplicit || x.Rating == "e")).ToArray();
                }
                else
                {
                    tag = null;
                    imgs = _cache.Where(x => x.SearchType == type).ToArray();
                }
                imgs = imgs.Where(x => x.Tags.All(t => !blacklistedTags.Contains(t))).ToArray();
                ImageCacherObject img;
                if (imgs.Length == 0)
                    img = null;
                else
                    img = imgs[_rng.Next(imgs.Length)];

                if (img != null)
                {
                    _cache.Remove(img);
                    return img;
                }
                else
                {
                    var images = await DownloadImages(tag, forceExplicit, type).ConfigureAwait(false);
                    images = images
                        .Where(x => x.Tags.All(t => !blacklistedTags.Contains(t)))
                        .ToArray();
                    if (images.Length == 0)
                        return null;
                    var toReturn = images[_rng.Next(images.Length)];
#if !GLOBAL_NADEKO
                    foreach (var dledImg in images)
                    {
                        if (dledImg != toReturn)
                            _cache.Add(dledImg);
                    }
#endif
                    return toReturn;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private SemaphoreSlim GetLock(DapiSearchType type)
        {
            return _locks.GetOrAdd(type, _ => new SemaphoreSlim(1, 1));
        }

        public async Task<ImageCacherObject[]> DownloadImages(string tag, bool isExplicit, DapiSearchType type)
        {
            tag = tag?.Replace(" ", "_", StringComparison.InvariantCulture).ToLowerInvariant();
            if (isExplicit)
                tag = "rating%3Aexplicit+" + tag;
            var website = "";
            switch (type)
            {
                case DapiSearchType.Safebooru:
                    website = $"https://safebooru.org/index.php?page=dapi&s=post&q=index&limit=1000&tags={tag}";
                    break;
                case DapiSearchType.E621:
                    website = $"https://e621.net/post/index.json?limit=1000&tags={tag}";
                    break;
                case DapiSearchType.Danbooru:
                    website = $"http://danbooru.donmai.us/posts.json?limit=100&tags={tag}";
                    break;
                case DapiSearchType.Gelbooru:
                    website = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Rule34:
                    website = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Konachan:
                    website = $"https://konachan.com/post.json?s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Yandere:
                    website = $"https://yande.re/post.json?limit=100&tags={tag}";
                    break;
                case DapiSearchType.Derpibooru:
                    website = $"https://derpibooru.org/search.json?q={tag?.Replace('+', ',')}&perpage=49";
                    break;
            }

            try
            {
                if (type == DapiSearchType.Konachan || type == DapiSearchType.Yandere ||
                    type == DapiSearchType.E621 || type == DapiSearchType.Danbooru)
                {
                    using (var http = _httpFactory.CreateClient().AddFakeHeaders())
                    {
                        var data = await http.GetStringAsync(website).ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<DapiImageObject[]>(data)
                            .Where(x => x.FileUrl != null)
                            .Select(x => new ImageCacherObject(x, type))
                            .ToArray();
                    }
                }

                if (type == DapiSearchType.Derpibooru)
                {
                    using (var http = _httpFactory.CreateClient().AddFakeHeaders())
                    {
                        var data = await http.GetStringAsync(website).ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<DerpiContainer>(data)
                            .Search
                            .Where(x => !string.IsNullOrWhiteSpace(x.Image))
                            .Select(x => new ImageCacherObject("https:" + x.Image,
                                type, x.Tags, x.Score))
                            .ToArray();
                    }
                }

                return (await LoadXmlAsync(website, type).ConfigureAwait(false)).ToArray();
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message);
                return Array.Empty<ImageCacherObject>();
            }
        }

        private async Task<ImageCacherObject[]> LoadXmlAsync(string website, DapiSearchType type)
        {
            var list = new List<ImageCacherObject>();
            using (var http = _httpFactory.CreateClient().AddFakeHeaders())
            using (var stream = await http.GetStreamAsync(website).ConfigureAwait(false))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings()
            {
                Async = true,
            }))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (reader.NodeType == XmlNodeType.Element &&
                        reader.Name == "post")
                    {
                        list.Add(new ImageCacherObject(new DapiImageObject()
                        {
                            FileUrl = reader["file_url"],
                            Tags = reader["tags"],
                            Rating = reader["rating"] ?? "e"

                        }, type));
                    }
                }
            }
            return list.ToArray();
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    public class ImageCacherObject : IComparable<ImageCacherObject>
    {
        public DapiSearchType SearchType { get; }
        public string FileUrl { get; }
        public HashSet<string> Tags { get; }
        public string Rating { get; }

        public ImageCacherObject(DapiImageObject obj, DapiSearchType type)
        {
            if (type == DapiSearchType.Danbooru && !Uri.IsWellFormedUriString(obj.FileUrl, UriKind.Absolute))
            {
                this.FileUrl = "https://danbooru.donmai.us" + obj.FileUrl;
            }
            else
            {
                this.FileUrl = obj.FileUrl.StartsWith("http", StringComparison.InvariantCulture) ? obj.FileUrl : "https:" + obj.FileUrl;
            }
            this.SearchType = type;
            this.Rating = obj.Rating;
            this.Tags = new HashSet<string>((obj.Tags ?? obj.TagString).Split(' '));
        }

        public ImageCacherObject(string url, DapiSearchType type, string tags, string rating)
        {
            this.SearchType = type;
            this.FileUrl = url;
            this.Tags = new HashSet<string>(tags.Split(' '));
            this.Rating = rating;
        }

        public override string ToString()
        {
            return FileUrl;
        }

        public int CompareTo(ImageCacherObject other)
        {
            return string.Compare(FileUrl, other.FileUrl, StringComparison.InvariantCulture);
        }
    }

    public class DapiImageObject
    {
        [JsonProperty("File_Url")]
        public string FileUrl { get; set; }
        public string Tags { get; set; }
        [JsonProperty("Tag_String")]
        public string TagString { get; set; }
        public string Rating { get; set; }
    }

    public class DerpiContainer
    {
        public DerpiImageObject[] Search { get; set; }
    }

    public class DerpiImageObject
    {
        public string Image { get; set; }
        public string Tags { get; set; }
        public string Score { get; set; }
    }

    public enum DapiSearchType
    {
        Safebooru,
        E621,
        Derpibooru,
        Gelbooru,
        Konachan,
        Rule34,
        Yandere,
        Danbooru,
    }
}
