using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IStatsService
    {
        Task<string> Print();
        Task Reset();
    }
}
