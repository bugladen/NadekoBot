namespace NadekoBot.Modules.Searches.Common
{
    public class MangaResult
    {
        public int id;
        public string publishing_status;
        public string image_url_lge;
        public string title_english;
        public int total_chapters;
        public int total_volumes;
        public string description;
        public string[] Genres;
        public string average_score;
        public string Link => "http://anilist.co/manga/" + id;
        public string Synopsis => description?.Substring(0, description.Length > 500 ? 500 : description.Length) + "...";
    }
}