using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Reminder : DbEntity
    {
        public DateTime When { get; set; }
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public string Message { get; set; }
        public bool IsPrivate { get; set; }
    }
}
