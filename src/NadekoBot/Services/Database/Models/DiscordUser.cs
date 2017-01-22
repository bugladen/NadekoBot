using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class DiscordUser : DbEntity
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
        public string AvatarId { get; set; }

        public override string ToString() => 
            Username + "#" + Discriminator;
    }
}
