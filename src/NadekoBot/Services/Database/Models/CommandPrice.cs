using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class CommandPrice : DbEntity
    {
        public int Price { get; set; }
        //this is unique
        public string CommandName { get; set; }
    }
}
