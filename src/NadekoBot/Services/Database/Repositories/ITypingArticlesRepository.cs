using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ITypingArticlesRepository : IRepository <TypingArticle>
    {
        TypingArticle GetRandom();
    }
}
