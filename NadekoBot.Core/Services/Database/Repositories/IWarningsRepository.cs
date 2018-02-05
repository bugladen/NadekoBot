using NadekoBot.Core.Services.Database.Models;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IWarningsRepository : IRepository<Warning>
    {
        Warning[] For(ulong guildId, ulong userId);
        Task ForgiveAll(ulong guildId, ulong userId, string mod);
        bool Forgive(ulong guildId, ulong userId, string mod, int index);
        Warning[] GetForGuild(ulong id);
    }
}
