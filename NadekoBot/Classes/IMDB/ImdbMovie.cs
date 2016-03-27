using System.Collections;
using System.Collections.Generic;

namespace NadekoBot.Classes.IMDB
{
    public class ImdbMovie
    {
        public bool status { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Year { get; set; }
        public string Rating { get; set; }
        public List<string> Genres { get; set; }
        public ArrayList Directors { get; set; }
        public ArrayList Writers { get; set; }
        public ArrayList Cast { get; set; }
        public ArrayList Producers { get; set; }
        public ArrayList Musicians { get; set; }
        public ArrayList Cinematographers { get; set; }
        public ArrayList Editors { get; set; }
        public string MpaaRating { get; set; }
        public string ReleaseDate { get; set; }
        public string Plot { get; set; }
        public ArrayList PlotKeywords { get; set; }
        public string Poster { get; set; }
        public string PosterLarge { get; set; }
        public string PosterFull { get; set; }
        public string Runtime { get; set; }
        public string Top250 { get; set; }
        public string Oscars { get; set; }
        public string Awards { get; set; }
        public string Nominations { get; set; }
        public string Storyline { get; set; }
        public string Tagline { get; set; }
        public string Votes { get; set; }
        public ArrayList Languages { get; set; }
        public ArrayList Countries { get; set; }
        public Dictionary<string, string> ReleaseDates { get; set; }
        public ArrayList MediaImages { get; set; }
        public ArrayList RecommendedTitles { get; set; }
        public string ImdbURL { get; set; }

        public Dictionary<string, string> Aka { get; set; }

        public override string ToString()
        {
            return "`Title:` **" + EnglishTitle + " (" + OriginalTitle + ")" +
            "**\n`Year:` " + Year +
            "**\n`Rating:` " + Rating +
            "**\n`Genre:` " + GenresAsString +
            "\n`Link:` " + ImdbURL +
            "\n`Plot:` " + Plot.Substring(0, Plot.Length > 500 ? 500 : Plot.Length) + "..."
            //"\n`img:` " + Poster //imdb url do it for us I think its a discord auto thing
            ;
        }

        public string EnglishTitle
        {
            get
            {
                return Aka.ContainsKey("USA") ? Aka["USA"] :
                    (Aka.ContainsKey("UK") ? Aka["UK"] :
                    (Aka.ContainsKey("(original title)") ? Aka["(original title)"] :
                    (Aka.ContainsKey("(original)") ? Aka["(original)"] : OriginalTitle)));
            }
        }
        public string GenresAsString
        {
            get
            {
                string ret = "";
                Genres.ForEach(g => ret = ret + " " + g);
                return ret;
            }
        }
    }
}