using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IImagesService
    {
        Stream Heads { get; }
        Stream Tails { get; }

        IImmutableList<Tuple<string, Stream>> CurrencyImages { get; }

        Task Reload();
    }
}
