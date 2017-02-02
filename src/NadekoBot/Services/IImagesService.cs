using System;
using System.Collections.Generic;
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

        Task Reload();
    }
}
