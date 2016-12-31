using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands.Models
{
    public struct GoogleSearchResult
    {
        public string Title { get; }
        public string Link { get; }
        public string Text { get; }

        public GoogleSearchResult(string title, string link, string text)
        {
            this.Title = title;
            this.Link = link;
            this.Text = text;
        }
    }
}
