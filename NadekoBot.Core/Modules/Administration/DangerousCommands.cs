using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Core.Modules.Administration.Services;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public class DangerousCommands : NadekoSubmodule<DangerousCommandsService>
        {

            private async Task InternalExecSql(string sql, params object[] reps)
            {
                sql = string.Format(sql, reps);
                try
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("sql_confirm_exec"))
                        .WithDescription(Format.Code(sql));

                    if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        return;
                    }

                    var res = await _service.ExecuteSql(sql).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(res.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ExecSql([Remainder]string sql) =>
                InternalExecSql(sql);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifus() =>
                ExecSql(DangerousCommandsService.WaifusDeleteSql);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(IUser user) =>
                DeleteWaifu(user.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(ulong userId) =>
                InternalExecSql(DangerousCommandsService.WaifuDeleteSql, userId);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteCurrency() =>
                ExecSql(DangerousCommandsService.CurrencyDeleteSql);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeletePlaylists() =>
                ExecSql(DangerousCommandsService.MusicPlaylistDeleteSql);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteExp() =>
                ExecSql(DangerousCommandsService.XpDeleteSql);
        }
    }
}
#endif