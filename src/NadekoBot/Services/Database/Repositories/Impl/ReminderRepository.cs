using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class ReminderRepository : Repository<Reminder>, IReminderRepository
    {
        public ReminderRepository(DbContext context) : base(context)
        {
        }
    }
}
