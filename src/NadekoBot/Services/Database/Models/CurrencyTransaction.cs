using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class CurrencyTransaction : DbEntity
    {
        public long Amount { get; set; }
        public string Reason { get; set; }
        public ulong UserId { get; set; }
    }
}
