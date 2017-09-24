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
        private readonly HttpClient _http;

        public SearchImageCacher()
        {
            _http = new HttpClient();
            _http.AddFakeHeaders();

            _log = LogManager.GetCurrentClassLogger();
            _rng = new NadekoRandom();
            _cache = new SortedSet<ImageCacherObject>();
        }

        public async Task<ImageCacherObject> GetImage(string tag, bool forceExplicit, DapiSearchType type,
            HashSet<string> blacklistedTags = null)
        {
            tag = tag?.ToLowerInvariant();

            blacklistedTags = blacklistedTags ?? new HashSet<string>();

            if (type == DapiSearchType.E621)
                tag = tag?.Replace("yuri", "female/female");

            var _lock = GetLock(type);
            await _lock.WaitAsync();
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
                        if(dledImg != toReturn)
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
            tag = tag?.Replace(" ", "_").ToLowerInvariant();
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
            }
                
            if (type == DapiSearchType.Konachan || type == DapiSearchType.Yandere || 
                type == DapiSearchType.E621 || type == DapiSearchType.Danbooru)
            {
                var data = await _http.GetStringAsync(website).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<DapiImageObject[]>(data)
                    .Where(x => x.File_Url != null)
                    .Select(x => new ImageCacherObject(x, type))
                    .ToArray();
            }

            return (await LoadXmlAsync(website, type)).ToArray();
        }

        private async Task<ImageCacherObject[]> LoadXmlAsync(string website, DapiSearchType type)
        {
            var list = new List<ImageCacherObject>();
            using (var reader = XmlReader.Create(await _http.GetStreamAsync(website), new XmlReaderSettings()
            {
                Async = true,
            }))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element &&
                        reader.Name == "post")
                    {
                        list.Add(new ImageCacherObject(new DapiImageObject()
                        {
                            File_Url = reader["file_url"],
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
            if (type == DapiSearchType.Danbooru)
                this.FileUrl = "https://danbooru.donmai.us" + obj.File_Url;
            else
                this.FileUrl = obj.File_Url.StartsWith("http") ? obj.File_Url : "https:" + obj.File_Url;
            this.SearchType = type;
            this.Rating = obj.Rating;
            this.Tags = new HashSet<string>((obj.Tags ?? obj.Tag_String).Split(' '));
        }

        public override string ToString()
        {
            return FileUrl;
        }

        public int CompareTo(ImageCacherObject other)
        {
            return FileUrl.CompareTo(other.FileUrl);
        }
    }

    public class DapiImageObject
    {
        public string File_Url { get; set; }
        public string Tags { get; set; }
        public string Tag_String { get; set; }
        public string Rating { get; set; }
    }

    public enum DapiSearchType
    {
        Safebooru,
        E621,
        Gelbooru,
        Konachan,
        Rule34,
        Yandere,
        Danbooru
    }
}
