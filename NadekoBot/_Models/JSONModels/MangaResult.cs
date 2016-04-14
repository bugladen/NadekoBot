namespace NadekoBot.Classes.JSONModels
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

        public override string ToString() =>
            "`Title:` **" + title_english +
            "**\n`Status:` " + publishing_status +
            "\n`Chapters:` " + total_chapters +
            "\n`Volumes:` " + total_volumes +
            "\n`Link:` http://anilist.co/manga/" + id +
            "\n`Synopsis:` " + description.Substring(0, description.Length > 500 ? 500 : description.Length) + "..." +
            "\n`img:` " + image_url_lge;
    }
}