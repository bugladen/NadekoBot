using NadekoBot.Modules.Searches.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*******************************************************************************
* Free ASP.net IMDb Scraper API for the new IMDb Template.
* Author: Abhinay Rathore
* Website: http://www.AbhinayRathore.com
* Blog: http://web3o.blogspot.com
* More Info: http://web3o.blogspot.com/2010/11/aspnetc-imdb-scraping-api.html

* Updated By: Gergo Torcsvari
* Last Updated: Feb, 2016
*******************************************************************************/

namespace NadekoBot.Modules.Searches.IMDB
{
    public static class ImdbScraper
    {
        //Search Engine URLs
        private static string GoogleSearch = "https://www.google.com/search?q=imdb+";
        //Constructor
        public static async Task<ImdbMovie> ImdbScrape(string MovieName, bool GetExtraInfo = true)
        {
            ImdbMovie mov = new ImdbMovie();
            string imdbUrl = await GetIMDbUrlAsync(System.Uri.EscapeUriString(MovieName));
            mov.Status = false;
            if (!string.IsNullOrWhiteSpace(imdbUrl))
            {
                await ParseIMDbPage(imdbUrl, GetExtraInfo, mov);
            }

            return mov;
        }

        public static async Task<ImdbMovie> ImdbScrapeFromId(string imdbId, bool GetExtraInfo = true)
        {
            ImdbMovie mov = new ImdbMovie();
            string imdbUrl = "http://www.imdb.com/title/" + imdbId + "/";
            mov.Status = false;
            await ParseIMDbPage(imdbUrl, GetExtraInfo, mov);
            return mov;
        }

        public static async Task<string> GetIMDBId(string MovieName)
        {
            string imdbUrl = await GetIMDbUrlAsync(System.Uri.EscapeUriString(MovieName));
            return Match(@"http://www.imdb.com/title/(tt\d{7})", imdbUrl);
        }
        //Get IMDb URL from search results
        private static async Task<string> GetIMDbUrlAsync(string MovieName)
        {
            string url = GoogleSearch + MovieName;
            string html = await GetUrlDataAsync(url);
            List<string> imdbUrls = MatchAll(@"<a href=""(http://www.imdb.com/title/tt\d{7}/)"".*?>.*?</a>", html);
            if (imdbUrls.Count > 0)
                return (string)imdbUrls[0];
            else return String.Empty;
        }
        //Parse IMDb page data
        private static async Task ParseIMDbPage(string imdbUrl, bool GetExtraInfo, ImdbMovie mov)
        {
            string html = await GetUrlDataAsync(imdbUrl + "combined");
            mov.Id = Match(@"<link rel=""canonical"" href=""http://www.imdb.com/title/(tt\d{7})/combined"" />", html);
            if (!string.IsNullOrEmpty(mov.Id))
            {
                mov.Status = true;
                mov.Title = Match(@"<title>(IMDb \- )*(.*?) \(.*?</title>", html, 2);
                mov.OriginalTitle = Match(@"title-extra"">(.*?)<", html);
                mov.Year = Match(@"<title>.*?\(.*?(\d{4}).*?).*?</title>", Match(@"(<title>.*?</title>)", html));
                mov.Rating = Match(@"<b>(\d.\d)/10</b>", html);
                mov.Genres = MatchAll(@"<a.*?>(.*?)</a>", Match(@"Genre.?:(.*?)(</div>|See more)", html)).Cast<string>().ToList();
                mov.Plot = Match(@"Plot:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                mov.Poster = Match(@"<div class=""photo"">.*?<a name=""poster"".*?><img.*?src=""(.*?)"".*?</div>", html);
                if (!string.IsNullOrEmpty(mov.Poster) && mov.Poster.IndexOf("media-imdb.com") > 0)
                {
                    mov.Poster = Regex.Replace(mov.Poster, @"_V1.*?.jpg", "_V1._SY200.jpg");
                }
                else
                {
                    mov.Poster = string.Empty;
                }
                mov.ImdbURL = "http://www.imdb.com/title/" + mov.Id + "/";
                if (GetExtraInfo)
                {
                    string plotHtml = await GetUrlDataAsync(imdbUrl + "plotsummary");
                    await GetReleaseDatesAndAka(mov);
                }
            }
        }
        //Get all release dates and aka-s
        private static async Task GetReleaseDatesAndAka(ImdbMovie mov)
        {
            Dictionary<string, string> release = new Dictionary<string, string>();
            string releasehtml = await GetUrlDataAsync("http://www.imdb.com/title/" + mov.Id + "/releaseinfo");
            foreach (string r in MatchAll(@"<tr class="".*?"">(.*?)</tr>", Match(@"<table id=""release_dates"" class=""subpage_data spFirst"">\n*?(.*?)</table>", releasehtml)))
            {
                Match rd = new Regex(@"<td>(.*?)</td>\n*?.*?<td class=.*?>(.*?)</td>", RegexOptions.Multiline).Match(r);
                release[StripHTML(rd.Groups[1].Value.Trim())] = StripHTML(rd.Groups[2].Value.Trim());
            }
            //mov.ReleaseDates = release;

            Dictionary<string, string> aka = new Dictionary<string, string>();
            List<string> list = MatchAll(@".*?<tr class="".*?"">(.*?)</tr>", Match(@"<table id=""akas"" class=.*?>\n*?(.*?)</table>", releasehtml));
            foreach (string r in list)
            {
                Match rd = new Regex(@"\n*?.*?<td>(.*?)</td>\n*?.*?<td>(.*?)</td>", RegexOptions.Multiline).Match(r);
                aka[StripHTML(rd.Groups[1].Value.Trim())] = StripHTML(rd.Groups[2].Value.Trim());
            }
            mov.Aka = aka;



        }
        //Get all media images
        private static async Task<List<string>> GetMediaImages(ImdbMovie mov)
        {
            List<string> list = new List<string>();
            string mediaurl = "http://www.imdb.com/title/" + mov.Id + "/mediaindex";
            string mediahtml = await GetUrlDataAsync(mediaurl);
            int pagecount = MatchAll(@"<a href=""\?page=(.*?)"">", Match(@"<span style=""padding: 0 1em;"">(.*?)</span>", mediahtml)).Count;
            for (int p = 1; p <= pagecount + 1; p++)
            {
                mediahtml = await GetUrlDataAsync(mediaurl + "?page=" + p);
                foreach (Match m in new Regex(@"src=""(.*?)""", RegexOptions.Multiline).Matches(Match(@"<div class=""thumb_list"" style=""font-size: 0px;"">(.*?)</div>", mediahtml)))
                {
                    String image = m.Groups[1].Value;
                    list.Add(Regex.Replace(image, @"_V1\..*?.jpg", "_V1._SY0.jpg"));
                }
            }
            return list;
        }
        //Get Recommended Titles
        private static async Task<List<string>> GetRecommendedTitlesAsync(ImdbMovie mov)
        {
            List<string> list = new List<string>();
            string recUrl = "http://www.imdb.com/widget/recommendations/_ajax/get_more_recs?specs=p13nsims%3A" + mov.Id;
            string json = await GetUrlDataAsync(recUrl);
            return MatchAll(@"title=\\""(.*?)\\""", json);
        }
        /*******************************[ Helper Methods ]********************************/
        //Match single instance
        private static string Match(string regex, string html, int i = 1)
        {
            return new Regex(regex, RegexOptions.Multiline).Match(html).Groups[i].Value.Trim();
        }
        //Match all instances and return as List<string>
        private static List<string> MatchAll(string regex, string html, int i = 1)
        {
            List<string> list = new List<string>();
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
        private static Task<string> GetUrlDataAsync(string url)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
                return http.GetStringAsync(url);
            }
        }
    }
}