using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class PollsRepository : Repository<Poll>, IPollsRepository
    {
        public PollsRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Poll> GetAllPolls()
        {
            return _set.Include(x => x.Answers)
                .Include(x => x.Votes)
                .ToArray();
        }

        public void RemovePoll(int id)
        {
            var p = _set
                .Include(x => x.Answers)
                .Include(x => x.Votes)
                .FirstOrDefault(x => x.Id == id);
            p.Votes.Clear();
            p.Answers.Clear();
            _set.Remove(p);
        }
    }
}
