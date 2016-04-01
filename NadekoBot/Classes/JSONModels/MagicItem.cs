using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes.JSONModels
{
    class MagicItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public override string ToString() =>
            $"✨`{Name}`\n\t*{Description}*";
    }
}
