using NadekoBot.Services.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class RepeaterRepository : Repository<Repeater>, IRepeaterRepository
    {
        public RepeaterRepository(DbContext context) : base(context)
        {
        }
    }
}
