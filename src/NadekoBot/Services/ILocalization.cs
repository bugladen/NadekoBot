using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface ILocalization
    {
        string this[string key] { get; }
    }
}
