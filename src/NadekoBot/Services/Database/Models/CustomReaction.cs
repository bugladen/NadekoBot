using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace NadekoBot.Services.Database.Models
{
    public class CustomReaction : DbEntity
    {
        public ulong? GuildId { get; set; }
        [NotMapped]
        public Regex Regex { get; set; }
        public string Response { get; set; }
        public string Trigger { get; set; }
        public bool IsRegex { get; set; }
        public bool OwnerOnly { get; set; }
        public override string ToString() => $"`#{Id}`  `Trigger:` {Trigger}\n `Response:` {Response}";
    }

    public class ReactionResponse : DbEntity
    {
        public bool OwnerOnly { get; set; }
        public string Text { get; set; }
    }
}