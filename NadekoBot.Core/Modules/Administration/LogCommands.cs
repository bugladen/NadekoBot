#if !GLOBAL_NADEKO
using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using static NadekoBot.Modules.Administration.Services.LogCommandService;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [NoPublicBot]
        public class LogCommands : NadekoSubmodule<LogCommandService>
        {
            public enum EnableDisable
            {
                Enable,
                Disable
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogServer(PermissionAction action)
            {
                await _service.LogServer(Context.Guild.Id, Context.Channel.Id, action.Value).ConfigureAwait(false);
                if (action.Value)
                    await ReplyConfirmLocalized("log_all").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_disabled").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogIgnore()
            {
                var channel = (ITextChannel)Context.Channel;

                var removed = _service.LogIgnore(Context.Guild.Id, Context.Channel.Id);

                if (!removed)
                    await ReplyConfirmLocalized("log_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_not_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogEvents()
            {
                _service.GuildLogSettings.TryGetValue(Context.Guild.Id, out LogSetting l);
                var str = string.Join("\n", Enum.GetNames(typeof(LogType))
                    .Select(x =>
                    {
                        var val = l == null ? null : GetLogProperty(l, Enum.Parse<LogType>(x));
                        if (val != null)
                            return $"{Format.Bold(x)} <#{val}>";
                        return Format.Bold(x);
                    }));

                await Context.Channel.SendConfirmAsync(Format.Bold(GetText("log_events")) + "\n" +
                    str)
                    .ConfigureAwait(false);
            }

            private static ulong? GetLogProperty(LogSetting l, LogType type)
            {
                switch (type)
                {
                    case LogType.Other:
                        return l.LogOtherId;
                    case LogType.MessageUpdated:
                        return l.MessageUpdatedId;
                    case LogType.MessageDeleted:
                        return l.MessageDeletedId;
                    case LogType.UserJoined:
                        return l.UserJoinedId;
                    case LogType.UserLeft:
                        return l.UserLeftId;
                    case LogType.UserBanned:
                        return l.UserBannedId;
                    case LogType.UserUnbanned:
                        return l.UserUnbannedId;
                    case LogType.UserUpdated:
                        return l.UserUpdatedId;
                    case LogType.ChannelCreated:
                        return l.ChannelCreatedId;
                    case LogType.ChannelDestroyed:
                        return l.ChannelDestroyedId;
                    case LogType.ChannelUpdated:
                        return l.ChannelUpdatedId;
                    case LogType.UserPresence:
                        return l.LogUserPresenceId;
                    case LogType.VoicePresence:
                        return l.LogVoicePresenceId;
                    case LogType.VoicePresenceTTS:
                        return l.LogVoicePresenceTTSId;
                    case LogType.UserMuted:
                        return l.UserMutedId;
                    default:
                        return null;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task Log(LogType type)
            {
                var val = _service.Log(Context.Guild.Id, Context.Channel.Id, type);

                if (val)
                    await ReplyConfirmLocalized("log", Format.Bold(type.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_stop", Format.Bold(type.ToString())).ConfigureAwait(false);
            }
        }
    }
}
#endif