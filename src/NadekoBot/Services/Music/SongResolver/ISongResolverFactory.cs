using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Music.SongResolver.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Music.SongResolver
{
    public interface ISongResolverFactory
    {
        Task<IResolveStrategy> GetResolveStrategy(string query, MusicType? musicType);
    }
}
