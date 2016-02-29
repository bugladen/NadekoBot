namespace NadekoBot.Classes.JSONModels
{
    public class AnimeResult
    {
        public int id;
        public string airing_status;
        public string title_english;
        public int total_episodes;
        public string description;
        public string image_url_lge;

        public override string ToString() =>
            "`Title:` **" + title_english +
            "**\n`Status:` " + airing_status +
            "\n`Episodes:` " + total_episodes +
            "\n`Link:` http://anilist.co/anime/" + id +
            "\n`Synopsis:` " + description.Substring(0, description.Length > 500 ? 500 : description.Length) + "..." +
            "\n`img:` " + image_url_lge;
    }
}