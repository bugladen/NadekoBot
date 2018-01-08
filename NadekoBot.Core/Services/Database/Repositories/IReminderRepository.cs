using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IReminderRepository : IRepository<Reminder>
    {
        IEnumerable<Reminder> GetIncludedReminders(IEnumerable<long> guildIds);
        IEnumerable<Reminder> RemindersFor(ulong userId, int page);
    }
}
