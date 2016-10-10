using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IMusicPlaylistRepository : IRepository<MusicPlaylist>
    {
        List<MusicPlaylist> GetPlaylistsOnPage(int num);
        MusicPlaylist GetWithSongs(int id);
    }
}
