using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands : NadekoSubmodule<MuteService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [Priority(0)]
            public async Task SetMuteRole([Leftover] string name)
            {
                name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return;

                await _service.SetMuteRoleAsync(ctx.Guild.Id, name).ConfigureAwait(false);

                await ReplyConfirmLocalizedAsync("mute_role_set").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [Priority(1)]
            public Task SetMuteRole([Leftover] IRole role)
                => SetMuteRole(role.Name);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [UserPerm(GuildPerm.MuteMembers)]
            [Priority(0)]
            public async Task Mute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, ctx.User).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_muted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [UserPerm(GuildPerm.MuteMembers)]
            [Priority(1)]
            public async Task Mute(StoopidTime time, IGuildUser user)
            {
                if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(1))
                    return;
                try
                {
                    await _service.TimedMute(user, ctx.User, time.Time).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_muted_time", Format.Bold(user.ToString()), (int)time.Time.TotalMinutes).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            [UserPerm(GuildPerm.MuteMembers)]
            public async Task Unmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, ctx.User).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ChatMute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, ctx.User, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_chat_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task ChatUnmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.Guild.Id, user.Id, ctx.User, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_chat_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MuteMembers)]
            public async Task VoiceMute([Leftover] IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, ctx.User, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_voice_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.MuteMembers)]
            public async Task VoiceUnmute([Leftover] IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, ctx.User, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalizedAsync("user_voice_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync("mute_error").ConfigureAwait(false);
                }
            }
        }
    }
}
