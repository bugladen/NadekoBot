using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Core.Common.TypeReaders.Models;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands : NadekoSubmodule<MuteService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public async Task SetMuteRole([Remainder] string name)
            {
                name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return;

                await _service.SetMuteRoleAsync(Context.Guild.Id, name).ConfigureAwait(false);

                await ReplyConfirmLocalized("mute_role_set").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public Task SetMuteRole([Remainder] IRole role)
                => SetMuteRole(role.Name);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            [Priority(0)]
            public async Task Mute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, Context.User).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_muted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            [Priority(1)]
            public async Task Mute(StoopidTime time, IGuildUser user)
            {
                if (time.Time < TimeSpan.FromMinutes(1) || time.Time > TimeSpan.FromDays(1))
                    return;
                try
                {
                    await _service.TimedMute(user, Context.User, time.Time).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_muted_time", Format.Bold(user.ToString()), (int)time.Time.TotalMinutes).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Unmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, Context.User).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ChatMute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, Context.User, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_chat_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ChatUnmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.Guild.Id, user.Id, Context.User, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_chat_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceMute([Remainder] IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, Context.User, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_voice_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceUnmute([Remainder] IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user.GuildId, user.Id, Context.User, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_voice_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }
        }
    }
}
