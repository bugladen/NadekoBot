using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ShardCom
{
    public class ShardComMessage
    {
        public int ShardId { get; set; }
        public ConnectionState ConnectionState { get; set; }
        public int Guilds { get; set; }
    }
}
