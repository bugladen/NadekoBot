using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Common.Attributes
{
    public class NadekoOptions : Attribute
    {
        public Type OptionType { get; set; }

        public NadekoOptions(Type t)
        {
            this.OptionType = t;
        }
    }
}
