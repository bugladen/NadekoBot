using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    class UserPokeTypes : DbEntity
    {
        public long UserId { get; set; }
        public string type { get; set; }
    }
}
