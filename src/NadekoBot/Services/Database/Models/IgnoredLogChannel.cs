using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class IgnoredLogChannel : DbEntity
    {
        public LogSetting LogSetting { get; set; }
        public ulong ChannelId { get; set; }
    }
}
