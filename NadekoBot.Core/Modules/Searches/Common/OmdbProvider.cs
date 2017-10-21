using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
{
    public static class OmdbProvider
    {
        private const string queryUrl = "https://omdbapi.nadekobot.me/?t={0}&y=&plot=full&r=json";

        public static async Task<OmdbMovie> FindMovie(string name, IGoogleApiService google)
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync(String.Format(queryUrl,name.Trim().Replace(' ','+'))).ConfigureAwait(false);
                var movie = JsonConvert.DeserializeObject<OmdbMovie>(res);
                if (movie?.Title == null)
                    return null;
                movie.Poster = await google.ShortenUrl(movie.Poster);
                return movie;
            }
        }
    }

    public class OmdbMovie
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string ImdbRating { get; set; }
        public string ImdbId { get; set; }
        public string Genre { get; set; }
        public string Plot { get; set; }
        public string Poster { get; set; }

        public EmbedBuilder GetEmbed() =>
            new EmbedBuilder().WithOkColor()
                              .WithTitle(Title)
                              .WithUrl($"http://www.imdb.com/title/{ImdbId}/")
                              .WithDescription(Plot.TrimTo(1000))
                              .AddField(efb => efb.WithName("Rating").WithValue(ImdbRating).WithIsInline(true))
                              .AddField(efb => efb.WithName("Genre").WithValue(Genre).WithIsInline(true))
                              .AddField(efb => efb.WithName("Year").WithValue(Year).WithIsInline(true))
                              .WithImageUrl(Poster);

        public override string ToString() =>
$@"`Title:` {Title}
`Year:` {Year}
`Rating:` {ImdbRating}
`Genre:` {Genre}
`Link:` http://www.imdb.com/title/{ImdbId}/
`Plot:` {Plot}";
    }
}
