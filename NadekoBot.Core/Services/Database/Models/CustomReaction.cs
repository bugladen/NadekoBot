using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace NadekoBot.Core.Services.Database.Models
{
    public class CustomReaction : DbEntity
    {
        public ulong? GuildId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Regex Regex { get; set; }
        public string Response { get; set; }
        public string Trigger { get; set; }

        public bool IsRegex { get; set; }
        public bool OwnerOnly { get; set; }
        public bool AutoDeleteTrigger { get; set; }
        public bool DmResponse { get; set; }

        [JsonIgnore]
        public bool IsGlobal => !GuildId.HasValue;

        public bool ContainsAnywhere { get; set; }
    }

    public class ReactionResponse : DbEntity
    {
        public bool OwnerOnly { get; set; }
        public string Text { get; set; }
    }
}