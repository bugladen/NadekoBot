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
using System.Threading;
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

            private ConcurrentDictionary<ITextChannel, List<string>> UserPresenceUpdates { get; }
            private Timer t;

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

                t = new Timer(async (state) =>
                {
                    var keys = UserPresenceUpdates.Keys.ToList();
                    foreach (var key in keys)
                    {
                        List<string> messages;
                        UserPresenceUpdates.TryRemove(key, out messages);
                        foreach (var msg in messages)
                        {
                            await key.SendMessageAsync(msg);
                        }
                    }
                }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                

                _client.MessageReceived += _client_MessageReceived;
                _client.MessageUpdated += _client_MessageUpdated;
                _client.MessageDeleted += _client_MessageDeleted;
                _client.UserBanned += _client_UserBanned;
                _client.UserUnbanned += _client_UserUnbanned;
                _client.UserJoined += _client_UserJoined;
                _client.UserLeft += _client_UserLeft;
                _client.UserPresenceUpdated += _client_UserPresenceUpdated;
                _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;

                _client.ChannelCreated += _client_ChannelCreated;
                _client.ChannelDestroyed += _client_ChannelDestroyed;
                _client.ChannelUpdated += _client_ChannelUpdated;
            }

            private Task _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
            {
                var before = cbefore as ITextChannel;
                if (before == null)
                    return Task.CompletedTask;
                var after = (ITextChannel)cafter;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.ChannelUpdated)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = before.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    if (before.Name != after.Name)
                        await logChannel.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Name Changed** `#{after.Name}` ({after.Id})
    `Old:` {before.Name}
    `New:` {after.Name}").ConfigureAwait(false);
                    else if (before.Topic != after.Topic)
                        await logChannel.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Topic Changed** `#{after.Name}` ({after.Id})
    `Old:` {before.Topic}
    `New:` {after.Topic}").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            }

            private Task _client_ChannelDestroyed(IChannel ich)
            {
                var ch = ich as IGuildChannel;
                if (ch == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.ChannelDestroyed)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = ch.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"❗`{prettyCurrentTime}` `{(ch is IVoiceChannel ? "Voice" : "Text")} Channel Deleted:` **#{ch.Name}** ({ch.Id})").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            }

            private Task _client_ChannelCreated(IChannel ich)
            {
                var ch = ich as IGuildChannel;
                if (ch == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.ChannelCreated)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = ch.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"`{prettyCurrentTime}`🆕`{(ch is IVoiceChannel ? "Voice" : "Text")} Channel Created:` **#{ch.Name}** ({ch.Id})").ConfigureAwait(false);
                });

                return Task.CompletedTask;
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
                    _log.Info("Voice state");
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
                if ((logChannel = usr.Guild.GetTextChannel(logSetting.UserPresenceChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    if(before.Status != after.Status)
                        await logChannel.SendMessageAsync($"`{prettyCurrentTime}`**{usr.Username}** is now **{after.Status}**.");
                    else
                        await logChannel.SendMessageAsync($"`{prettyCurrentTime}`**{usr.Username}** is now playing **{after.Status}**.");
                });

                return Task.CompletedTask;
            }

            private Task _client_UserLeft(IGuildUser usr)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserLeft)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = usr.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"`{prettyCurrentTime}`❗`User left:` **{usr.Username}** ({usr.Id})").ConfigureAwait(false);
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

                ITextChannel logChannel;
                if ((logChannel = usr.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"`{prettyCurrentTime}`❗`User joined:` **{usr.Username}** ({usr.Id})").ConfigureAwait(false);
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

                ITextChannel logChannel;
                if ((logChannel = guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"`{prettyCurrentTime}`♻`User unbanned:` **{usr.Username}** ({usr.Id})").ConfigureAwait(false);
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

                ITextChannel logChannel;
                if ((logChannel = guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($"❗`{prettyCurrentTime}`❌`User banned:` **{usr.Username}** ({usr.Id})").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            }

            private Task _client_MessageDeleted(ulong arg1, Optional<IMessage> imsg)
            {
                var msg = (imsg.IsSpecified ? imsg.Value : null) as IUserMessage;
                if (msg == null)
                    return Task.CompletedTask;

                var ch = msg.Channel as ITextChannel;

                var channel = msg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.MessageDeleted)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = channel.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($@"🕔`{prettyCurrentTime}` **Message** 🚮 `#{ch.Name}`
👤`{msg.Author.Username}`: {msg.Content}
    `Attachements`: {string.Join(", ", msg.Attachments.Select(a=>a.ProxyUrl))}").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            }

            private Task _client_MessageUpdated(Optional<IMessage> optmsg, IMessage imsg2)
            {
                var after = imsg2 as IUserMessage;
                if (after == null)
                    return Task.CompletedTask;

                var before = (optmsg.IsSpecified ? optmsg.Value : null) as IUserMessage;
                if (before == null)
                    return Task.CompletedTask;

                var channel = after.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.MessageUpdated)
                    return Task.CompletedTask;
                
                ITextChannel logChannel;
                if ((logChannel = channel.Guild.GetTextChannel(logSetting.ChannelId)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    await logChannel.SendMessageAsync($@"🕔`{prettyCurrentTime}` **Message** 📝 `#{channel.Name}`
👤`{before.Author.Username}`
        `Old:` {before.Content}
        `New:` {after.Content}").ConfigureAwait(false);
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
                    _log.Info("Message received");
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
                        var newLogSetting = uow.GuildConfigs.For(guild.Id).LogSetting;
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
                        GuildLogSettings.AddOrUpdate(guild.Id, newLogSetting, (gid, old) => newLogSetting);
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
                LogSetting logSetting;
                using (var uow = DbHandler.UnitOfWork())
                {
                    logSetting = uow.GuildConfigs.For(channel.Guild.Id).LogSetting;
                    GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    logSetting.IsLogging = !logSetting.IsLogging;
                    if (logSetting.IsLogging)
                        logSetting.ChannelId = channel.Id;
                    await uow.CompleteAsync();
                }

                if (logSetting.IsLogging)
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

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task LogAdd(IUserMessage msg, [Remainder] string eventName)
            {
                var channel = (ITextChannel)msg.Channel;
                //eventName = eventName?.Replace(" ","").ToLowerInvariant();

                switch (eventName.ToLowerInvariant())
                {
                    case "messagereceived":
                    case "messageupdated":
                    case "messagedeleted":
                    case "userjoined":
                    case "userleft":
                    case "userbanned":
                    case "userunbanned":
                    case "channelcreated":
                    case "channeldestroyed":
                    case "channelupdated":
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            var logSetting = uow.GuildConfigs.For(channel.Guild.Id).LogSetting;
                            GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                            var prop = logSetting.GetType().GetProperty(eventName);
                            prop.SetValue(logSetting, true);
                            await uow.CompleteAsync().ConfigureAwait(false);
                        }
                        await channel.SendMessageAsync($"`Now logging {eventName} event.`").ConfigureAwait(false);
                        break;
                    default:
                        await channel.SendMessageAsync($"`Event \"{eventName}\" not found.`").ConfigureAwait(false);
                        break;
                }
            }

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

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task UserPresence(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var logSetting = uow.GuildConfigs.For(channel.Guild.Id).LogSetting;
                    GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    enabled = logSetting.LogUserPresence = !logSetting.LogUserPresence;
                    if(enabled)
                        logSetting.UserPresenceChannelId = channel.Id;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                    await channel.SendMessageAsync($"`Logging user presence updates in {channel.Name} ({channel.Id}) channel.`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"`Stopped logging user presence updates.`").ConfigureAwait(false);
            }

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