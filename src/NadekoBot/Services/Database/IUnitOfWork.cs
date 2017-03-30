using NadekoBot.Services.Database.Repositories;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database
{
    public interface IUnitOfWork : IDisposable
    {
        NadekoContext _context { get; }

        IQuoteRepository Quotes { get; }
        IGuildConfigRepository GuildConfigs { get; }
        IDonatorsRepository Donators { get; }
        IClashOfClansRepository ClashOfClans { get; }
        IReminderRepository Reminders { get; }
        ISelfAssignedRolesRepository SelfAssignedRoles { get; }
        IBotConfigRepository BotConfig { get; }
        IUnitConverterRepository ConverterUnits { get; }
        ICustomReactionRepository CustomReactions { get; }
        ICurrencyRepository Currency { get; }
        ICurrencyTransactionsRepository CurrencyTransactions { get; }
        IMusicPlaylistRepository MusicPlaylists { get; }
        IPokeGameRepository PokeGame { get; }
        IWaifuRepository Waifus { get; }
        IDiscordUserRepository DiscordUsers { get; }
        IWarningsRepository Warnings { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
