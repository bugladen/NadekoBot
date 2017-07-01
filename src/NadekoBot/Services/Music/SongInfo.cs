using Discord;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music
{
    public class SongInfo
    {
        public string Provider { get; set; }
        public MusicType ProviderType { get; set; }
        public string Query { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
        public string AlbumArt { get; set; }
        public string QueuerName { get; set; }
        public TimeSpan TotalTime = TimeSpan.Zero;

        public string PrettyProvider => (Provider ?? "???");
        //public string PrettyFullTime => PrettyCurrentTime + " / " + PrettyTotalTime;
        public string PrettyName => $"**[{Title.TrimTo(65)}]({SongUrl})**";
        public string PrettyInfo => $"{PrettyTotalTime} | {PrettyProvider} | {QueuerName}";
        public string PrettyFullName => $"{PrettyName}\n\t\t`{PrettyTotalTime} | {PrettyProvider} | {Format.Sanitize(QueuerName.TrimTo(15))}`";
        public string PrettyTotalTime
        {
            get
            {
                if (TotalTime == TimeSpan.Zero)
                    return "(?)";
                if (TotalTime == TimeSpan.MaxValue)
                    return "∞";
                var time = TotalTime.ToString(@"mm\:ss");
                var hrs = (int)TotalTime.TotalHours;

                if (hrs > 0)
                    return hrs + ":" + time;
                return time;
            }
        }

        public string SongUrl
        {
            get
            {
                switch (ProviderType)
                {
                    case MusicType.Normal:
                        return Query;
                    case MusicType.Soundcloud:
                        return Query;
                    case MusicType.Local:
                        return $"https://google.com/search?q={ WebUtility.UrlEncode(Title).Replace(' ', '+') }";
                    case MusicType.Radio:
                        return $"https://google.com/search?q={Title}";
                    default:
                        return "";
                }
            }
        }
        private readonly Regex videoIdRegex = new Regex("<=v=[a-zA-Z0-9-]+(?=&)|(?<=[0-9])[^&\n]+|(?<=v=)[^&\n]+", RegexOptions.Compiled);
        public string Thumbnail
        {
            get
            {
                switch (ProviderType)
                {
                    case MusicType.Radio:
                        return "https://cdn.discordapp.com/attachments/155726317222887425/261850925063340032/1482522097_radio.png"; //test links
                    case MusicType.Normal:
                        //todo have videoid in songinfo from the start
                        var videoId = videoIdRegex.Match(Query);
                        return $"https://img.youtube.com/vi/{ videoId }/0.jpg";
                    case MusicType.Local:
                        return "https://cdn.discordapp.com/attachments/155726317222887425/261850914783100928/1482522077_music.png"; //test links
                    case MusicType.Soundcloud:
                        return AlbumArt;
                    default:
                        return "";
                }
            }
        }
    }
}
