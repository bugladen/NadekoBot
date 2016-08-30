using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
