using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

/*******************************************************************************
* Free ASP.net IMDb Scraper API for the new IMDb Template.
* Author: Abhinay Rathore
* Website: http://www.AbhinayRathore.com
* Blog: http://web3o.blogspot.com
* More Info: http://web3o.blogspot.com/2010/11/aspnetc-imdb-scraping-api.html

* Updated By: Gergo Torcsvari
* Last Updated: Feb, 2016
*******************************************************************************/

namespace NadekoBot.Modules.Searches.Commands.IMDB
{
    public static class ImdbScraper
    {
        //Search Engine URLs
        private static string GoogleSearch = "https://www.google.com/search?q=imdb+";
        private static string BingSearch = "http://www.bing.com/search?q=imdb+";
        private static string AskSearch = "http://www.ask.com/web?q=imdb+";
        //Constructor
        public static ImdbMovie ImdbScrape(string MovieName, bool GetExtraInfo = true)
        {
            ImdbMovie mov = new ImdbMovie();
            string imdbUrl = GetIMDbUrl(System.Uri.EscapeUriString(MovieName));
            mov.Status = false;
            if (!string.IsNullOrWhiteSpace(imdbUrl))
            {
                ParseIMDbPage(imdbUrl, GetExtraInfo, mov);
            }

            return mov;
        }

        public static ImdbMovie ImdbScrapeFromId(string imdbId, bool GetExtraInfo = true)
        {
            ImdbMovie mov = new ImdbMovie();
            string imdbUrl = "http://www.imdb.com/title/" + imdbId + "/";
            mov.Status = false;
            ParseIMDbPage(imdbUrl, GetExtraInfo, mov);
            return mov;
        }

        public static string GetIMDBId(string MovieName)
        {
            string imdbUrl = GetIMDbUrl(System.Uri.EscapeUriString(MovieName));
            return match(@"http://www.imdb.com/title/(tt\d{7})", imdbUrl);
        }
        //Get IMDb URL from search results
        private static string GetIMDbUrl(string MovieName, string searchEngine = "google")
        {
            string url = GoogleSearch + MovieName; //default to Google search
            if (searchEngine.ToLower().Equals("bing")) url = BingSearch + MovieName;
            if (searchEngine.ToLower().Equals("ask")) url = AskSearch + MovieName;
            string html = GetUrlData(url);
            ArrayList imdbUrls = MatchAll(@"<a href=""(http://www.imdb.com/title/tt\d{7}/)"".*?>.*?</a>", html);
            if (imdbUrls.Count > 0)
                return (string)imdbUrls[0]; //return first IMDb result
            else if (searchEngine.ToLower().Equals("google")) //if Google search fails
                return GetIMDbUrl(MovieName, "bing"); //search using Bing
            else if (searchEngine.ToLower().Equals("bing")) //if Bing search fails
                return GetIMDbUrl(MovieName, "ask"); //search using Ask
            else //search fails
                return string.Empty;
        }
        //Parse IMDb page data
        private static void ParseIMDbPage(string imdbUrl, bool GetExtraInfo, ImdbMovie mov)
        {
            string html = GetUrlData(imdbUrl + "combined");
            mov.Id = match(@"<link rel=""canonical"" href=""http://www.imdb.com/title/(tt\d{7})/combined"" />", html);
            if (!string.IsNullOrEmpty(mov.Id))
            {
                mov.Status = true;
                mov.Title = match(@"<title>(IMDb \- )*(.*?) \(.*?</title>", html, 2);
                mov.OriginalTitle = match(@"title-extra"">(.*?)<", html);
                mov.Year = match(@"<title>.*?\(.*?(\d{4}).*?\).*?</title>", match(@"(<title>.*?</title>)", html));
                mov.Rating = match(@"<b>(\d.\d)/10</b>", html);
                mov.Genres = MatchAll(@"<a.*?>(.*?)</a>", match(@"Genre.?:(.*?)(</div>|See more)", html)).Cast<string>().ToList();
                mov.Plot = match(@"Plot:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                //mov.Directors = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Directed by</a></h5>(.*?)</table>", html));
                //mov.Writers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Writing credits</a></h5>(.*?)</table>", html));
                //mov.Producers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Produced by</a></h5>(.*?)</table>", html));
                //mov.Musicians = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Original Music by</a></h5>(.*?)</table>", html));
                //mov.Cinematographers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Cinematography by</a></h5>(.*?)</table>", html));
                //mov.Editors = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Film Editing by</a></h5>(.*?)</table>", html));
                //mov.Cast = matchAll(@"<td class=""nm""><a.*?href=""/name/.*?/"".*?>(.*?)</a>", match(@"<h3>Cast</h3>(.*?)</table>", html));
                //mov.PlotKeywords = matchAll(@"<a.*?>(.*?)</a>", match(@"Plot Keywords:</h5>.*?<div class=""info-content"">(.*?)</div", html));
                //mov.ReleaseDate = match(@"Release Date:</h5>.*?<div class=""info-content"">.*?(\d{1,2} (January|February|March|April|May|June|July|August|September|October|November|December) (19|20)\d{2})", html);
                //mov.Runtime = match(@"Runtime:</h5><div class=""info-content"">(\d{1,4}) min[\s]*.*?</div>", html);
                //mov.Top250 = match(@"Top 250: #(\d{1,3})<", html);
                //mov.Oscars = match(@"Won (\d+) Oscars?\.", html);
                //if (string.IsNullOrEmpty(mov.Oscars) && "Won Oscar.".Equals(match(@"(Won Oscar\.)", html))) mov.Oscars = "1";
                //mov.Awards = match(@"(\d{1,4}) wins", html);
                //mov.Nominations = match(@"(\d{1,4}) nominations", html);
                //mov.Tagline = match(@"Tagline:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                //mov.MpaaRating = match(@"MPAA</a>:</h5><div class=""info-content"">Rated (G|PG|PG-13|PG-14|R|NC-17|X) ", html);
                //mov.Votes = match(@">(\d+,?\d*) votes<", html);
                //mov.Languages = matchAll(@"<a.*?>(.*?)</a>", match(@"Language.?:(.*?)(</div>|>.?and )", html));
                //mov.Countries = matchAll(@"<a.*?>(.*?)</a>", match(@"Country:(.*?)(</div>|>.?and )", html));
                mov.Poster = match(@"<div class=""photo"">.*?<a name=""poster"".*?><img.*?src=""(.*?)"".*?</div>", html);
                if (!string.IsNullOrEmpty(mov.Poster) && mov.Poster.IndexOf("media-imdb.com") > 0)
                {
                    mov.Poster = Regex.Replace(mov.Poster, @"_V1.*?.jpg", "_V1._SY200.jpg");
                    //mov.PosterLarge = Regex.Replace(mov.Poster, @"_V1.*?.jpg", "_V1._SY500.jpg");
                    //mov.PosterFull = Regex.Replace(mov.Poster, @"_V1.*?.jpg", "_V1._SY0.jpg");
                }
                else
                {
                    mov.Poster = string.Empty;
                    //mov.PosterLarge = string.Empty;
                    //mov.PosterFull = string.Empty;
                }
                mov.ImdbURL = "http://www.imdb.com/title/" + mov.Id + "/";
                if (GetExtraInfo)
                {
                    string plotHtml = GetUrlData(imdbUrl + "plotsummary");
                    //mov.Storyline = match(@"<p class=""plotpar"">(.*?)(<i>|</p>)", plotHtml);
                    GetReleaseDatesAndAka(mov);
                    //mov.MediaImages = getMediaImages(mov);
                    //mov.RecommendedTitles = getRecommendedTitles(mov);
                }
            }
        }
        //Get all release dates and aka-s
        private static void GetReleaseDatesAndAka(ImdbMovie mov)
        {
            Dictionary<string, string> release = new Dictionary<string, string>();
            string releasehtml = GetUrlData("http://www.imdb.com/title/" + mov.Id + "/releaseinfo");
            foreach (string r in MatchAll(@"<tr class="".*?"">(.*?)</tr>", match(@"<table id=""release_dates"" class=""subpage_data spFirst"">\n*?(.*?)</table>", releasehtml)))
            {
                Match rd = new Regex(@"<td>(.*?)</td>\n*?.*?<td class=.*?>(.*?)</td>", RegexOptions.Multiline).Match(r);
                release[StripHTML(rd.Groups[1].Value.Trim())] = StripHTML(rd.Groups[2].Value.Trim());
            }
            //mov.ReleaseDates = release;

            Dictionary<string, string> aka = new Dictionary<string, string>();
            ArrayList list = MatchAll(@".*?<tr class="".*?"">(.*?)</tr>", match(@"<table id=""akas"" class=.*?>\n*?(.*?)</table>", releasehtml));
            foreach (string r in list)
            {
                Match rd = new Regex(@"\n*?.*?<td>(.*?)</td>\n*?.*?<td>(.*?)</td>", RegexOptions.Multiline).Match(r);
                aka[StripHTML(rd.Groups[1].Value.Trim())] = StripHTML(rd.Groups[2].Value.Trim());
            }
            mov.Aka = aka;



        }
        //Get all media images
        private static ArrayList GetMediaImages(ImdbMovie mov)
        {
            ArrayList list = new ArrayList();
            string mediaurl = "http://www.imdb.com/title/" + mov.Id + "/mediaindex";
            string mediahtml = GetUrlData(mediaurl);
            int pagecount = MatchAll(@"<a href=""\?page=(.*?)"">", match(@"<span style=""padding: 0 1em;"">(.*?)</span>", mediahtml)).Count;
            for (int p = 1; p <= pagecount + 1; p++)
            {
                mediahtml = GetUrlData(mediaurl + "?page=" + p);
                foreach (Match m in new Regex(@"src=""(.*?)""", RegexOptions.Multiline).Matches(match(@"<div class=""thumb_list"" style=""font-size: 0px;"">(.*?)</div>", mediahtml)))
                {
                    String image = m.Groups[1].Value;
                    list.Add(Regex.Replace(image, @"_V1\..*?.jpg", "_V1._SY0.jpg"));
                }
            }
            return list;
        }
        //Get Recommended Titles
        private static ArrayList GetRecommendedTitles(ImdbMovie mov)
        {
            ArrayList list = new ArrayList();
            string recUrl = "http://www.imdb.com/widget/recommendations/_ajax/get_more_recs?specs=p13nsims%3A" + mov.Id;
            string json = GetUrlData(recUrl);
            list = MatchAll(@"title=\\""(.*?)\\""", json);
            HashSet<String> set = new HashSet<string>();
            foreach (String rec in list) set.Add(rec);
            return new ArrayList(set.ToList());
        }
        /*******************************[ Helper Methods ]********************************/
        //Match single instance
        private static string match(string regex, string html, int i = 1)
        {
            return new Regex(regex, RegexOptions.Multiline).Match(html).Groups[i].Value.Trim();
        }
        //Match all instances and return as ArrayList
        private static ArrayList MatchAll(string regex, string html, int i = 1)
        {
            ArrayList list = new ArrayList();
            foreach (Match m in new Regex(regex, RegexOptions.Multiline).Matches(html))
                list.Add(m.Groups[i].Value.Trim());
            return list;
        }
        //Strip HTML Tags
        private static string StripHTML(string inputString)
        {
            return Regex.Replace(inputString, @"<.*?>", string.Empty);
        }
        //Get URL Data
        private static string GetUrlData(string url)
        {
            WebClient client = new WebClient();
            Random r = new Random();
            //Random IP Address
            //client.Headers["X-Forwarded-For"] = r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255);
            //Random User-Agent
            client.Headers["User-Agent"] = "Mozilla/" + r.Next(3, 5) + ".0 (Windows NT " + r.Next(3, 5) + "." + r.Next(0, 2) + "; rv:37.0) Gecko/20100101 Firefox/" + r.Next(30, 37) + "." + r.Next(0, 5);
            Stream datastream = client.OpenRead(url);
            StreamReader reader = new StreamReader(datastream);
            StringBuilder sb = new StringBuilder();

            //TODO: Coud be reader error must catch and drop!!!
            while (!reader.EndOfStream)
                sb.Append(reader.ReadLine());
            return sb.ToString();
        }
    }
}