using NadekoBot.Extensions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace NadekoBot.Services.Searches
{
    public class SearchesService
    {
        public async Task<string> DapiSearch(string tag, DapiSearchType type)
        {
            tag = tag?.Replace(" ", "_");
            var website = "";
            switch (type)
            {
                case DapiSearchType.Safebooru:
                    website = $"https://safebooru.org/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Gelbooru:
                    website = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Rule34:
                    website = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Konachan:
                    website = $"https://konachan.com/post.xml?s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Yandere:
                    website = $"https://yande.re/post.xml?limit=100&tags={tag}";
                    break;
            }
            try
            {
                var toReturn = await Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        http.AddFakeHeaders();
                        var data = await http.GetStreamAsync(website).ConfigureAwait(false);
                        var doc = new XmlDocument();
                        doc.Load(data);

                        var node = doc.LastChild.ChildNodes[new NadekoRandom().Next(0, doc.LastChild.ChildNodes.Count)];

                        var url = node.Attributes["file_url"].Value;
                        if (!url.StartsWith("http"))
                            url = "https:" + url;
                        return url;
                    }
                }).ConfigureAwait(false);
                return toReturn;
            }
            catch
            {
                return null;
            }
        }
    }

    public enum DapiSearchType
    {
        Safebooru,
        Gelbooru,
        Konachan,
        Rule34,
        Yandere
    }
}
