using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class TypingArticle : DbEntity
    {
        public string Author { get; set; }
        public string Text { get; set; }
    }
}
