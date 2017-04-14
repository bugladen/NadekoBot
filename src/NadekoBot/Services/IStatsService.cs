using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public interface IStatsService
    {
        Task<string> Print();
    }
}
