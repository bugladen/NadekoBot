using Discord;
using Discord.API;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands.OMDB
{
    public static class OmdbProvider
    {
        private const string queryUrl =  "http://www.omdbapi.com/?t={0}&y=&plot=full&r=json";

        public static async Task<OmdbMovie> FindMovie(string name)
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync(String.Format(queryUrl,name.Trim().Replace(' ','+'))).ConfigureAwait(false);
                var movie = JsonConvert.DeserializeObject<OmdbMovie>(res);

                movie.Poster = await NadekoBot.Google.ShortenUrl(movie.Poster);
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

        public Embed GetEmbed() =>
            new EmbedBuilder().WithColor(NadekoBot.OkColor)
                              .WithTitle(Title)
                              .WithUrl($"http://www.imdb.com/title/{ImdbId}/")
                              .WithDescription(Plot)
                              .AddField(efb => efb.WithName("Rating").WithValue(ImdbRating).WithIsInline(true))
                              .AddField(efb => efb.WithName("Genre").WithValue(Genre).WithIsInline(true))
                              .AddField(efb => efb.WithName("Year").WithValue(Year).WithIsInline(true))
                              .WithImage(eib => eib.WithUrl(Poster))
                              .Build();

        public override string ToString() =>
$@"`Title:` {Title}
`Year:` {Year}
`Rating:` {ImdbRating}
`Genre:` {Genre}
`Link:` http://www.imdb.com/title/{ImdbId}/
`Plot:` {Plot}";
    }
}
