using System;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IStatsService : INService
    {
        string Author { get; }
        long CommandsRan { get; }
        string Heap { get; }
        string Library { get; }
        long MessageCounter { get; }
        double MessagesPerSecond { get; }
        long TextChannels { get; }
        long VoiceChannels { get; }
        int GuildCount { get; }

        TimeSpan GetUptime();
        string GetUptimeString(string separator = ", ");
        void Initialize();
        Task<string> Print();
    }
}
