using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Quote : DbEntity
    {
        public string UserName { get; set; }
        public string Keyword { get; set; }
        public string Text { get; set; }
    }
}
