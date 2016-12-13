using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LogCommands
        {
            private static ShardedDiscordClient _client { get; }
            private static Logger _log { get; }

            private static string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";

            public static ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

            private static ConcurrentDictionary<ITextChannel, List<string>> UserPresenceUpdates { get; } = new ConcurrentDictionary<ITextChannel, List<string>>();
            private static Timer timerReference { get; }
            private IGoogleApiService _google { get; }

            static LogCommands()
            {
                _client = NadekoBot.Client;
                _log = LogManager.GetCurrentClassLogger();

                using (var uow = DbHandler.UnitOfWork())
                {
                    GuildLogSettings = new ConcurrentDictionary<ulong, LogSetting>(NadekoBot.AllGuildConfigs
                                                                                      .ToDictionary(g => g.GuildId, g => g.LogSetting));
                }

                timerReference = new Timer(async (state) =>
                {
                    try
                    {
                        var keys = UserPresenceUpdates.Keys.ToList();

                        await Task.WhenAll(keys.Select(async key =>
                        {
                            List<string> messages;
                            if (UserPresenceUpdates.TryRemove(key, out messages))
                                try { await key.SendMessageAsync(string.Join(Environment.NewLine, messages)); } catch { }
                        }));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            }

            public LogCommands()
            {
                //_client.MessageReceived += _client_MessageReceived;
                _client.MessageUpdated += _client_MessageUpdated;
                _client.MessageDeleted += _client_MessageDeleted;
                _client.UserBanned += _client_UserBanned;
                _client.UserUnbanned += _client_UserUnbanned;
                _client.UserJoined += _client_UserJoined;
                _client.UserLeft += _client_UserLeft;
                _client.UserPresenceUpdated += _client_UserPresenceUpdated;
                _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
                _client.UserUpdated += _client_UserUpdated;

                _client.ChannelCreated += _client_ChannelCreated;
                _client.ChannelDestroyed += _client_ChannelDestroyed;
                _client.ChannelUpdated += _client_ChannelUpdated;

                MuteCommands.UserMuted += MuteCommands_UserMuted;
                MuteCommands.UserUnmuted += MuteCommands_UserUnmuted;
            }

            private Task MuteCommands_UserMuted(IGuildUser usr, MuteCommands.MuteType muteType)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.IsLogging)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    string mutes = "";
                    switch (muteType)
                    {
                        case MuteCommands.MuteType.Voice:
                            mutes = "voice chat";
                            break;
                        case MuteCommands.MuteType.Chat:
                            mutes = "text chat";
                            break;
                        case MuteCommands.MuteType.All:
                            mutes = "text and voice chat";
                            break;
                    }
                    try { await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🔇 **| User muted from the {mutes}. |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                });

                return Task.CompletedTask;
            }

            private Task MuteCommands_UserUnmuted(IGuildUser usr, MuteCommands.MuteType muteType)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                    || !logSetting.IsLogging)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    string mutes = "";
                    switch (muteType)
                    {
                        case MuteCommands.MuteType.Voice:
                            mutes = "voice chat";
                            break;
                        case MuteCommands.MuteType.Chat:
                            mutes = "text chat";
                            break;
                        case MuteCommands.MuteType.All:
                            mutes = "text and voice chat";
                            break;
                    }
                    try { await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🔊 **| User unmuted from the {mutes}. |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                });

                return Task.CompletedTask;
            }

            public static async Task TriggeredAntiProtection(IGuildUser[] users, PunishmentAction action, ProtectionType protection)
            {
                if (users.Length == 0)
                    return;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(users.First().Guild.Id, out logSetting)
                    || !logSetting.IsLogging)
                    return;
                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(users.First().Guild, logSetting)) == null)
                    return;

                var punishment = "";
                if (action == PunishmentAction.Mute)
                {
                    punishment = "🔇 MUTED";
                    //punishment = "MUTED";
                }
                else if (action == PunishmentAction.Kick)
                {
                    punishment = "☣ SOFT-BANNED (KICKED)";
                    //punishment = "KICKED";
                }
                else if (action == PunishmentAction.Ban)
                {
                    punishment = "⛔️ BANNED";
                    //punishment = "BANNED";
                }
                await logChannel.SendMessageAsync(String.Join("\n",users.Select(user=>$"‼️ {Format.Bold(user.ToString())} got **{punishment}** due to __**{protection}**__ protection on **{user.Guild.Name}** server.")))
                //await logChannel.SendMessageAsync(String.Join("\n",users.Select(user=>$"{Format.Bold(user.ToString())} was **{punishment}** due to `{protection}` protection on **{user.Guild.Name}** server.")))
                                .ConfigureAwait(false);
            }

            private Task _client_UserUpdated(IGuildUser before, IGuildUser after)
            {
                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.UserUpdated)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(before.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        string str = $"🕔`{prettyCurrentTime}`";
                        if (before.Username != after.Username)
                            //str += $"**Name Changed**`{before.Username}#{before.Discriminator}`\n\t\t`New:`{after.ToString()}`";
                            str += $"👤__**{before.Username}#{before.Discriminator}**__ **| Name Changed |** 🆔 `{before.Id}`\n\t\t`New:` **{after.ToString()}**";
                        else if (before.Nickname != after.Nickname)
                            str += $"👤__**{before.Username}#{before.Discriminator}**__ **| Nickname Changed |** 🆔 `{before.Id}`\n\t\t`Old:` **{before.Nickname}#{before.Discriminator}**\n\t\t`New:` **{after.Nickname}#{after.Discriminator}**";
                            //str += $"**Nickname Changed**`{before.Username}#{before.Discriminator}`\n\t\t`Old:` {before.Nickname}#{before.Discriminator}\n\t\t`New:` {after.Nickname}#{after.Discriminator}";
                        else if (before.AvatarUrl != after.AvatarUrl)
                            //str += $"**Avatar Changed**👤`{before.Username}#{before.Discriminator}`\n\t {await _google.ShortenUrl(before.AvatarUrl)} `=>` {await _google.ShortenUrl(after.AvatarUrl)}";
                            str += $"👤__**{before.Username}#{before.Discriminator}**__ **| Avatar Changed |** 🆔 `{before.Id}`\n\t🖼 {await _google.ShortenUrl(before.AvatarUrl)} `=>` {await _google.ShortenUrl(after.AvatarUrl)}";
                        else if (!before.Roles.SequenceEqual(after.Roles))
                        {
                            if (before.Roles.Count() < after.Roles.Count())
                            {
                                var diffRoles = after.Roles.Where(r => !before.Roles.Contains(r)).Select(r => "**" + r.Name + "**");
                                //str += $"**User's Roles changed ⚔➕**👤`{before.ToString()}`\n\tNow has {string.Join(", ", diffRoles)} role.";
                                str += $"👤__**{before.ToString()}**__ **| User's Role Added |** 🆔 `{before.Id}`\n\t✅ {string.Join(", ", diffRoles).SanitizeMentions()}\n\t\t⚔ **`{string.Join(", ", after.Roles.Select(r => r.Name)).SanitizeMentions()}`** ⚔";
                            }
                            else if (before.Roles.Count() > after.Roles.Count())
                            {
                                var diffRoles = before.Roles.Where(r => !after.Roles.Contains(r)).Select(r => "**" + r.Name + "**");
                                //str += $"**User's Roles changed **`{before.ToString()}`\n\tNo longer has {string.Join(", ", diffRoles)} role.";
                                str += $"👤__**{before.ToString()}**__ **| User's Role Removed |** 🆔 `{before.Id}`\n\t🚮 {string.Join(", ", diffRoles).SanitizeMentions()}\n\t\t⚔ **`{string.Join(", ", after.Roles.Select(r => r.Name)).SanitizeMentions()}`** ⚔";
                            }
                        }
                        else
                            return;
                        try { await logChannel.SendMessageAsync(str).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                    }
                    catch { }
                });

                return Task.CompletedTask;
            }

            private Task _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
            {
                var before = cbefore as IGuildChannel;
                if (before == null)
                    return Task.CompletedTask;
                var after = (IGuildChannel)cafter;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.ChannelUpdated
                    || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == after.Id))
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(before.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (before.Name != after.Name)
                            //await logChannel.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Name Changed** `#{after.Name}` ({after.Id})
                            await logChannel.SendMessageAsync($@"🕓`{prettyCurrentTime}`ℹ️ **| Channel Name Changed |** #⃣ `{after.Name} ({after.Id})`
    `Old:` {before.Name}
    **`New:`** {after.Name}").ConfigureAwait(false);
                        else if ((before as ITextChannel).Topic != (after as ITextChannel).Topic)
                            //await logChannel.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Topic Changed** `#{after.Name}` ({after.Id})
                            await logChannel.SendMessageAsync($@"🕘`{prettyCurrentTime}`ℹ️ **| Channel Topic Changed |** #⃣ `{after.Name} ({after.Id})`
    `Old:` {((ITextChannel)before).Topic}
    **`New:`** {((ITextChannel)after).Topic}").ConfigureAwait(false);
                    }
                    catch { }
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
                    || !logSetting.ChannelDestroyed
                    || logSetting.IgnoredChannels.Any(ilc=>ilc.ChannelId == ch.Id))
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(ch.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try { await logChannel.SendMessageAsync($"🕕`{prettyCurrentTime}`🗑 **| {(ch is IVoiceChannel ? "Voice" : "Text")} Channel Deleted #⃣ {ch.Name}** `({ch.Id})`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                if ((logChannel = TryGetLogChannel(ch.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try { await logChannel.SendMessageAsync($"🕓`{prettyCurrentTime}`🆕 **| {(ch is IVoiceChannel ? "Voice" : "Text")} Channel Created: #⃣ {ch.Name}** `({ch.Id})`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                    || !logSetting.LogVoicePresence)
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting, LogChannelType.Voice)) == null)
                    return Task.CompletedTask;

                    string str = null;
                    if (beforeVch?.Guild == afterVch?.Guild)
                    {
                        str = $"🎙`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__ moved from **{beforeVch.Name}** to **{afterVch.Name}** voice channel.";
                    }
                    else if (beforeVch == null)
                    {
                        str = $"🎙`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__ has joined **{afterVch.Name}** voice channel.";
                    }
                    else if (afterVch == null)
                    {
                        str = $"🎙`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__ has left **{beforeVch.Name}** voice channel.";
                    }
                    if(str != null)
                        UserPresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });

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
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting, LogChannelType.UserPresence)) == null)
                    return Task.CompletedTask;
                string str;
                if (before.Status != after.Status)
                    str = $"🔵`{prettyCurrentTime}`👤__**{usr.Username}**__ is now **{after.Status}**.";
                else
                    str = $"👾`{prettyCurrentTime}`👤__**{usr.Username}**__ is now playing **{after.Game}**.";

                UserPresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });

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
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try { await logChannel.SendMessageAsync($"❗️🕛`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__❌ **| USER LEFT |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                if ((logChannel = TryGetLogChannel(usr.Guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try { await logChannel.SendMessageAsync($"❕🕓`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__✅ **| USER JOINED |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                if ((logChannel = TryGetLogChannel(guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                   try { await logChannel.SendMessageAsync($"❕🕘`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__♻️ **| USER UN-BANNED |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                if ((logChannel = TryGetLogChannel(guild, logSetting)) == null)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try { await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🚫 **| USER BANNED |** 🆔 `{usr.Id}`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                });

                return Task.CompletedTask;
            }

            private Task _client_MessageDeleted(ulong arg1, Optional<IMessage> imsg)
            {
                var msg = (imsg.IsSpecified ? imsg.Value : null) as IUserMessage;
                if (msg == null || msg.IsAuthor())
                    return Task.CompletedTask;

                var channel = msg.Channel as ITextChannel;
                if (channel == null)
                    return Task.CompletedTask;

                LogSetting logSetting;
                if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                    || !logSetting.IsLogging
                    || !logSetting.MessageDeleted
                    || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(channel.Guild, logSetting)) == null || logChannel.Id == msg.Id)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var str = $@"🕔`{prettyCurrentTime}`👤__**{msg.Author.Username}#{msg.Author.Discriminator}**__ **| Deleted Message |** 🆔 `{msg.Author.Id}` #⃣ `{channel.Name}`
🗑 {msg.Resolve(userHandling: UserMentionHandling.NameAndDiscriminator)}";
                        if (msg.Attachments.Any())
                            str += $"{Environment.NewLine}📎 {string.Join(", ", msg.Attachments.Select(a => a.ProxyUrl))}";
                        await logChannel.SendMessageAsync(str.SanitizeMentions()).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }
                });

                return Task.CompletedTask;
            }

            private Task _client_MessageUpdated(Optional<IMessage> optmsg, IMessage imsg2)
            {
                var after = imsg2 as IUserMessage;
                if (after == null || after.IsAuthor())
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
                    || !logSetting.MessageUpdated
                    || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                    return Task.CompletedTask;

                ITextChannel logChannel;
                if ((logChannel = TryGetLogChannel(channel.Guild, logSetting)) == null || logChannel.Id == after.Channel.Id)
                    return Task.CompletedTask;

                var task = Task.Run(async () =>
                {
                    //try { await logChannel.SendMessageAsync($@"🕔`{prettyCurrentTime}` **Message** 📝 `#{channel.Name}`
//👤`{before.Author.Username}`
                    try { await logChannel.SendMessageAsync($@"🕔`{prettyCurrentTime}`👤__**{before.Author.Username}#{before.Author.Discriminator}**__ **| 📝 Edited Message |** 🆔 `{before.Author.Id}` #⃣ `{channel.Name}`
        `Old:` {before.Resolve(userHandling: UserMentionHandling.NameAndDiscriminator).SanitizeMentions()}
        **`New:`** {after.Resolve(userHandling: UserMentionHandling.NameAndDiscriminator).SanitizeMentions()}").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                });

                return Task.CompletedTask;
            }

            private enum LogChannelType { Text, Voice, UserPresence };
            private static ITextChannel TryGetLogChannel(IGuild guild, LogSetting logSetting, LogChannelType logChannelType = LogChannelType.Text)
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
            [OwnerOnly]
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
                    await channel.SendMessageAsync("✅ **Logging enabled.**").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ **Logging disabled.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogIgnore(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    LogSetting logSetting = GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
                    removed = logSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    config.LogSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    if (removed == 0)
                    {
                        var toAdd = new IgnoredLogChannel { ChannelId = channel.Id };
                        logSetting.IgnoredChannels.Add(toAdd);
                        config.LogSetting.IgnoredChannels.Add(toAdd);
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                    await channel.SendMessageAsync($"🆗 Logging will **now ignore** #⃣ `{channel.Name} ({channel.Id})`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"ℹ️ Logging will **no longer ignore** #⃣ `{channel.Name} ({channel.Id})`").ConfigureAwait(false);
            }

            //[LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
            //[RequireContext(ContextType.Guild)]
            //[OwnerOnly]
            //public async Task LogAdd(IUserMessage msg, [Remainder] string eventName)
            //{
            //    var channel = (ITextChannel)msg.Channel;
            //    //eventName = eventName?.Replace(" ","").ToLowerInvariant();

            //    switch (eventName.ToLowerInvariant())
            //    {
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
            //                var logSetting = uow.GuildConfigs.For(channel.Guild.Id).LogSetting;
            //                GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
            //                var prop = logSetting.GetType().GetProperty(eventName);
            //                prop.SetValue(logSetting, true);
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
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
                    await channel.SendMessageAsync($"✅ Logging **user presence** updates in #⃣ `{channel.Name} ({channel.Id})`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"ℹ️ Stopped logging **user presence** updates.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
            public async Task VoicePresence(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                bool enabled;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var logSetting = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.LogSetting)
                                                                                      .ThenInclude(ls => ls.IgnoredVoicePresenceChannelIds))
                                                                                            .LogSetting;
                    GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    enabled = logSetting.LogVoicePresence = !logSetting.LogVoicePresence;
                    if (enabled)
                        logSetting.VoicePresenceChannelId = channel.Id;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (enabled)
                    await channel.SendMessageAsync($"✅ Logging **voice presence** updates in #⃣ `{channel.Name} ({channel.Id})`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"ℹ️ Stopped logging **voice presence** updates.").ConfigureAwait(false);
            }

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
