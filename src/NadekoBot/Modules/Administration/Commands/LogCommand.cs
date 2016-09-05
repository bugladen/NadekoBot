using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LogCommands
        {
            private DiscordSocketClient _client { get; }
            private Logger _log { get; }

            private string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";

            public ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

            public LogCommands(DiscordSocketClient client)
            {
                _client = client;
                _log = LogManager.GetCurrentClassLogger();

                using (var uow = DbHandler.UnitOfWork())
                {
                    GuildLogSettings = new ConcurrentDictionary<ulong, LogSetting>(uow.GuildConfigs
                                                                                      .GetAll()
                                                                                      .ToDictionary(g => g.GuildId, g => g.LogSetting));
                }

                _client.MessageReceived += _client_MessageReceived;
                _client.MessageUpdated += _client_MessageUpdated;
                _client.MessageDeleted += _client_MessageDeleted;
                _client.UserBanned += _client_UserBanned;
                _client.UserUnbanned += _client_UserUnbanned;
                _client.UserJoined += _client_UserJoined;
                _client.UserLeft += _client_UserLeft;
                _client.UserPresenceUpdated += _client_UserPresenceUpdated;
                _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            }

            private Task _client_UserVoiceStateUpdated(IUser iusr, IVoiceState before, IVoiceState after)
            {
                var usr = iusr as IGuildUser;
                if (usr == null)
                    return Task.CompletedTask;

                var beforeVch = before.VoiceChannel;
                var afterVch = after.VoiceChannel;

                if (beforeVch == afterVch)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.LogVoicePresence
                    || !logSetting.IgnoredChannels.Any(ic => ic.ChannelId == after.VoiceChannel.Id))
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = usr.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    if (beforeVch?.Guild == afterVch?.Guild)
                    {
                        await logChannel.SendMessageAsync($"🎼`{prettyCurrentTime}` {usr.Username} moved from **{beforeVch.Name}** to **{afterVch.Name}** voice channel.").ConfigureAwait(false);
                    }
                    else if (beforeVch == null)
                    {
                        await logChannel.SendMessageAsync($"🎼`{prettyCurrentTime}` {usr.Username} has joined **{afterVch.Name}** voice channel.").ConfigureAwait(false);
                    }
                    else if (afterVch == null)
                    {
                        await logChannel.SendMessageAsync($"🎼`{prettyCurrentTime}` {usr.Username} has left **{beforeVch.Name}** voice channel.").ConfigureAwait(false);
                    }
                });

                return Task.CompletedTask;
            }

            private Task _client_UserPresenceUpdated(IGuildUser usr, IPresence before, IPresence after)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.LogUserPresence
                    || before.Status == after.Status)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = usr.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                return Task.CompletedTask;
            }

            private Task _client_UserLeft(IGuildUser usr)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserLeft)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_UserJoined(IGuildUser usr)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserJoined)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_UserUnbanned(IUser usr, IGuild guild)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserUnbanned)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_UserBanned(IUser usr, IGuild guild)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserBanned)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_MessageDeleted(ulong arg1, Optional<IMessage> imsg)
            {
                var msg = (imsg.IsSpecified ? imsg.Value : null) as IUserMessage;
                if (msg == null)
                    return Task.CompletedTask;

                var channel = msg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.MessageDeleted)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_MessageUpdated(Optional<IMessage> optmsg, IMessage imsg2)
            {
                var after = imsg2 as IUserMessage;
                if (after == null)
                    return Task.CompletedTask;

                var channel = after.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.MessageUpdated)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {

                });

                return Task.CompletedTask;
            }

            private Task _client_MessageReceived(IMessage imsg)
            {
                var msg = imsg as IUserMessage;
                if (msg == null)
                    return Task.CompletedTask;

                var channel = msg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting) 
                    || !logSetting.IsLogging
                    || !logSetting.MessageReceived)
                    return Task.CompletedTask;

                var task = Task.Run(() =>
                {

                });
                
                return Task.CompletedTask;
            }

            private enum LogChannelType { Text, Voice, UserPresence };
            private ITextChannel TryGetLogChannel(IGuild guild, LogSetting logSetting, LogChannelType logChannelType = LogChannelType.Text)
            {
                ulong id = 0;
                switch (logChannelType)
                {
                    case LogChannelType.Text:
                        id = logSetting.ChannelId;
                        break;
                    case LogChannelType.Voice:
                        id = logSetting.VoicePresenceChannelId;
                        break;
                    case LogChannelType.UserPresence:
                        id = logSetting.UserPresenceChannelId;
                        break;
                }
                var channel = guild.GetTextChannel(id);

                if (channel == null)
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        switch (logChannelType)
                        {
                            case LogChannelType.Text:
                                logSetting.IsLogging = false;
                                break;
                            case LogChannelType.Voice:
                                logSetting.LogVoicePresence = false;
                                break;
                            case LogChannelType.UserPresence:
                                logSetting.LogUserPresence = false;
                                break;
                        }
                        uow.Complete();
                        return null;
                    }
                else
                    return channel;
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task LogServer(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;
                GuildConfig config;
                using (var uow = DbHandler.UnitOfWork())
                {
                    config = uow.GuildConfigs.For(channel.Guild.Id);
                    LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
                    logSetting.IsLogging = !logSetting.IsLogging;
                    config.LogSetting = logSetting;
                    await uow.CompleteAsync();
                }

                if (config.LogSetting.IsLogging)
                    await channel.SendMessageAsync("`Logging enabled.`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("`Logging disabled.`").ConfigureAwait(false);
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task LogIgnore(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
                    removed = logSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    if (removed == 0)
                        logSetting.IgnoredChannels.Add(new IgnoredLogChannel { ChannelId = channel.Id });
                    config.LogSetting = logSetting;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                    await channel.SendMessageAsync($"`Logging will now ignore {channel.Name} ({channel.Id}) channel.`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"`Logging will no longer ignore {channel.Name} ({channel.Id}) channel.`").ConfigureAwait(false);
            }

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //public async Task LogAdd(IUserMessage msg, string eventName)
            //{
            //    var channel = (ITextChannel)msg.Channel;
            //    eventName = eventName.ToLowerInvariant();

            //    switch (eventName)
            //    {
            //        case "messagereceived":
            //        case "messageupdated":
            //        case "messagedeleted":
            //        case "userjoined":
            //        case "userleft":
            //        case "userbanned":
            //        case "userunbanned":
            //        case "channelcreated":
            //        case "channeldestroyed":
            //        case "channelupdated":
            //            using (var uow = DbHandler.UnitOfWork())
            //            {
            //                var config = uow.GuildConfigs.For(channel.Guild.Id);
            //                LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
            //                logSetting.GetType().GetProperty(eventName).SetValue(logSetting, true);
            //                config.LogSetting = logSetting;
            //                await uow.CompleteAsync().ConfigureAwait(false);
            //            }
            //            await channel.SendMessageAsync($"`Now logging {eventName} event.`").ConfigureAwait(false);
            //            break;
            //        default:
            //            await channel.SendMessageAsync($"`Event \"{eventName}\" not found.`").ConfigureAwait(false);
            //            break;
            //    }
            //}

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //public async Task LogRemove(IUserMessage msg, string eventName)
            //{
            //    var channel = (ITextChannel)msg.Channel;
            //    eventName = eventName.ToLowerInvariant();

            //    switch (eventName)
            //    {
            //        case "messagereceived":
            //        case "messageupdated":
            //        case "messagedeleted":
            //        case "userjoined":
            //        case "userleft":
            //        case "userbanned":
            //        case "userunbanned":
            //        case "channelcreated":
            //        case "channeldestroyed":
            //        case "channelupdated":
            //            using (var uow = DbHandler.UnitOfWork())
            //            {
            //                var config = uow.GuildConfigs.For(channel.Guild.Id);
            //                LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
            //                logSetting.GetType().GetProperty(eventName).SetValue(logSetting, false);
            //                config.LogSetting = logSetting;
            //                await uow.CompleteAsync().ConfigureAwait(false);
            //            }
            //            await channel.SendMessageAsync($"`No longer logging {eventName} event.`").ConfigureAwait(false);
            //            break;
            //        default:
            //            await channel.SendMessageAsync($"`Event \"{eventName}\" not found.`").ConfigureAwait(false);
            //            break;
            //    }
            //}

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //public async Task UserPresence(IUserMessage imsg)
            //{
            //    var channel = (ITextChannel)imsg.Channel;
            //    bool enabled;
            //    using (var uow = DbHandler.UnitOfWork())
            //    {
            //        var config = uow.GuildConfigs.For(channel.Guild.Id);
            //        LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
            //        enabled = logSetting.LogUserPresence = !config.LogSetting.LogUserPresence;
            //        config.LogSetting = logSetting;
            //        await uow.CompleteAsync().ConfigureAwait(false);
            //    }

            //    if (enabled)
            //        await channel.SendMessageAsync($"`Logging user presence updates in {channel.Name} ({channel.Id}) channel.`").ConfigureAwait(false);
            //    else
            //        await channel.SendMessageAsync($"`Stopped logging user presence updates.`").ConfigureAwait(false);
            //}

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //public async Task VoicePresence(IUserMessage imsg)
            //{
            //    var channel = (ITextChannel)imsg.Channel;
            //    bool enabled;
            //    using (var uow = DbHandler.UnitOfWork())
            //    {
            //        var config = uow.GuildConfigs.For(channel.Guild.Id);
            //        LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
            //        enabled = config.LogSetting.LogVoicePresence = !config.LogSetting.LogVoicePresence;
            //        config.LogSetting = logSetting;
            //        await uow.CompleteAsync().ConfigureAwait(false);
            //    }

            //    if (enabled)
            //        await channel.SendMessageAsync($"`Logging voice presence updates in {channel.Name} ({channel.Id}) channel.`").ConfigureAwait(false);
            //    else
            //        await channel.SendMessageAsync($"`Stopped logging voice presence updates.`").ConfigureAwait(false);
            //}

            //[LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //public async Task VoiPresIgnore(IUserMessage imsg, IVoiceChannel voiceChannel)
            //{
            //    var channel = (ITextChannel)imsg.Channel;
            //    int removed;
            //    using (var uow = DbHandler.UnitOfWork())
            //    {
            //        var config = uow.GuildConfigs.For(channel.Guild.Id);
            //        LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
            //        removed = logSetting.IgnoredVoicePresenceChannelIds.RemoveWhere(ivpc => ivpc.ChannelId == voiceChannel.Id);
            //        if (removed == 0)
            //            logSetting.IgnoredVoicePresenceChannelIds.Add(new IgnoredVoicePresenceChannel { ChannelId = voiceChannel.Id });
            //        config.LogSetting = logSetting;
            //        await uow.CompleteAsync().ConfigureAwait(false);
            //    }

            //    if (removed == 0)
            //        await channel.SendMessageAsync($"`Enabled logging voice presence updates for {voiceChannel.Name} ({voiceChannel.Id}) channel.`").ConfigureAwait(false);
            //    else
            //        await channel.SendMessageAsync($"`Disabled logging voice presence updates for {voiceChannel.Name} ({voiceChannel.Id}) channel.`").ConfigureAwait(false);
            //}
        }
    }
}