using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Donator : DbEntity
    {
        public ulong UserId { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; } = 0;
    }
}
