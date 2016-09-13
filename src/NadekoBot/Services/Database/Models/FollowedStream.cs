using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class FollowedStream : DbEntity
    {
        public ulong ChannelId { get; set; }
        public string Username { get; set; }
        public FollowedStreamType Type { get; set; }
        public bool LastStatus { get; set; }
        public ulong GuildId { get; set; }

        public enum FollowedStreamType
        {
            Twitch, Hitbox, Beam
        }
    }
}
