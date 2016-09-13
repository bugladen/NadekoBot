using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Repeater :DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Message { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
