using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Music.Classes
{
    public static class MusicExtensions
    {
        public static EmbedAuthorBuilder WithMusicIcon(this EmbedAuthorBuilder eab) =>
            eab.WithIconUrl("http://i.imgur.com/nhKS3PT.png");
    }
}
