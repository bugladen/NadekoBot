using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;

namespace NadekoBot.Core.Modules.Administration.Services
{
    public class DangerousCommandsService : INService
    {
        public const string WaifusDeleteSql = @"DELETE FROM WaifuUpdates;
DELETE FROM WaifuItem;
DELETE FROM WaifuInfo;";
        public const string CurrencyDeleteSql = "UPDATE DiscordUser SET CurrencyAmount=0; DELETE FROM CurrencyTransactions;";
        public const string MusicPlaylistDeleteSql = "DELETE FROM MusicPlaylists;";
        public const string XpDeleteSql = @"DELETE FROM UserXpStats;
UPDATE DiscordUser
SET ClubId=NULL,
    IsClubAdmin=0,
    TotalXp=0;
DELETE FROM ClubApplicants;
DELETE FROM ClubBans;
DELETE FROM Clubs;";

        private readonly DbService _db;

        public DangerousCommandsService(DbService db)
        {
            _db = db;
        }

        public async Task<int> ExecuteSql(string sql)
        {
            int res;
            using (var uow = _db.UnitOfWork)
            {
                res = await uow._context.Database.ExecuteSqlCommandAsync(sql);
            }
            return res;
        }
    }
}
