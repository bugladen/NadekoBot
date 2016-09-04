using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class TypingArticlesRepository : Repository<TypingArticle>, ITypingArticlesRepository
    {
        private Random _rand = null;
        private Random rand => _rand ?? (_rand = new Random());
        public TypingArticlesRepository(DbContext context) : base(context)
        {
        }

        public TypingArticle GetRandom()
        {
            var skip = (int)(rand.NextDouble() * _set.Count());
            return _set.Skip(skip).FirstOrDefault();
        }
    }
}
