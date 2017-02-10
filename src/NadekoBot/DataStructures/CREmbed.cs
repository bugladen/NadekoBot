using Discord;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures
{
    public class CREmbed
    {
        private static readonly Logger _log;
        public string PlainText { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public CREmbedFooter Footer { get; set; }
        public string Thumbnail { get; set; }
        public string Image { get; set; }
        public CREmbedField[] Fields { get; set; }
        public uint Color { get; set; } = 7458112;

        static CREmbed()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Title) ||
            !string.IsNullOrWhiteSpace(Description) ||
            !string.IsNullOrWhiteSpace(Thumbnail) ||
            !string.IsNullOrWhiteSpace(Image) ||
            (Footer != null && (!string.IsNullOrWhiteSpace(Footer.Text) || !string.IsNullOrWhiteSpace(Footer.IconUrl))) ||
            (Fields != null && Fields.Length > 0);

        public EmbedBuilder ToEmbed()
        {
            var embed = new EmbedBuilder()
                .WithTitle(Title)
                .WithDescription(Description)
                .WithColor(new Discord.Color(Color));
            if (Footer != null)
                embed.WithFooter(efb => efb.WithIconUrl(Footer.IconUrl).WithText(Footer.Text));
            embed.WithThumbnailUrl(Thumbnail)
                .WithImageUrl(Image);

            if (Fields != null)
                foreach (var f in Fields)
                {
                    embed.AddField(efb => efb.WithName(f.Name).WithValue(f.Value).WithIsInline(f.Inline));
                }

            return embed;
        }

        public static bool TryParse(string input, out CREmbed embed)
        {
            embed = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                var crembed = JsonConvert.DeserializeObject<CREmbed>(input);

                if (!crembed.IsValid)
                    return false;

                embed = crembed;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class CREmbedField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Inline { get; set; }
    }

    public class CREmbedFooter {
        public string Text { get; set; }
        public string IconUrl { get; set; }
    }
}
