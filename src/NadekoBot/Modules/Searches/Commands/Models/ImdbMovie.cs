using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Net;

namespace NadekoBot.Modules.Searches.Models
{
    public class ImdbMovie
    {
        public bool Status { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Year { get; set; }
        public string Rating { get; set; }
        public string Plot { get; set; }
        public string Poster { get; set; }
        public List<string> Genres { get; set; }
        public string ImdbURL { get; set; }

        public Dictionary<string, string> Aka { get; set; }

        public override string ToString() =>
$@"`Title:` {WebUtility.HtmlDecode(Title)} {(string.IsNullOrEmpty(OriginalTitle) ? "" : $"({OriginalTitle})")}
`Year:` {Year}
`Rating:` {Rating}
`Genre:` {GenresAsString}
`Link:` <{ImdbURL}>
`Plot:` {System.Net.WebUtility.HtmlDecode(Plot.TrimTo(500))}
`Poster:` " + NadekoBot.Google.ShortenUrl(Poster).GetAwaiter().GetResult();
        public string GenresAsString =>
                string.Join(", ", Genres);
    }
}