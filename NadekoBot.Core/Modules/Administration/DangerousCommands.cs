using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Threading.Tasks;
using Discord;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public class DangerousCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public DangerousCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ExecSql([Remainder]string sql)
            {
                try
                {

                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("sql_confirm_exec"))
                        .WithDescription(Format.Code(sql));

                    if (!await PromptUserConfirmAsync(embed))
                    {
                        return;
                    }

                    int res;
                    using (var uow = _db.UnitOfWork)
                    {
                        res = uow._context.Database.ExecuteSqlCommand(sql);
                    }

                    await Context.Channel.SendConfirmAsync(res.ToString());
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString());
                }
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifus() =>
                ExecSql(@"DELETE FROM WaifuUpdates;
DELETE FROM WaifuItem;
DELETE FROM WaifuInfo;");

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteCurrency() =>
                ExecSql("UPDATE DiscordUser SET CurrencyAmount=0; DELETE FROM CurrencyTransactions;");

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeletePlaylists() =>
                ExecSql("DELETE FROM MusicPlaylists;");

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteExp() =>
                ExecSql(@"DELETE FROM UserXpStats;
UPDATE DiscordUser
SET ClubId=NULL,
    IsClubAdmin=0,
    TotalXp=0;
DELETE FROM ClubApplicants;
DELETE FROM ClubBans;
DELETE FROM Clubs;");
        }
    }
}
#endif