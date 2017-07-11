using System.Threading.Tasks;

namespace NadekoBot.Services.Music.SongResolver.Strategies
{
    public interface IResolveStrategy
    {
        Task<SongInfo> ResolveSong(string query);
    }
}
