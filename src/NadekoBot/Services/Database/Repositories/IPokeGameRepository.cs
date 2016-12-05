using NadekoBot.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IPokeGameRepository : IRepository<UserPokeTypes>
    {
        //List<UserPokeTypes> GetAllPokeTypes();
    }
}
