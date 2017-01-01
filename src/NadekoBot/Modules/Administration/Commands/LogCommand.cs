using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LogCommands : ModuleBase
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
                var sw = Stopwatch.StartNew();

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

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
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
                _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated_TTS;
                _client.UserUpdated += _client_UserUpdated;

                _client.ChannelCreated += _client_ChannelCreated;
                _client.ChannelDestroyed += _client_ChannelDestroyed;
                _client.ChannelUpdated += _client_ChannelUpdated;

                MuteCommands.UserMuted += MuteCommands_UserMuted;
                MuteCommands.UserUnmuted += MuteCommands_UserUnmuted;
            }

            private async void _client_UserVoiceStateUpdated_TTS(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
            {
                try
                {
                    var usr = iusr as IGuildUser;
                    if (usr == null)
                        return;

                    var beforeVch = before.VoiceChannel;
                    var afterVch = after.VoiceChannel;

                    if (beforeVch == afterVch)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.LogVoicePresenceTTSId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresenceTTS)) == null)
                        return;

                    string str = null;
                    if (beforeVch?.Guild == afterVch?.Guild)
                    {
                        str = $"{usr.Username} moved from {beforeVch.Name} to {afterVch.Name}";
                    }
                    else if (beforeVch == null)
                    {
                        str = $"{usr.Username} has joined {afterVch.Name}";
                    }
                    else if (afterVch == null)
                    {
                        str = $"{usr.Username} has left {beforeVch.Name}";
                    }
                    var toDelete = await logChannel.SendMessageAsync(str, true).ConfigureAwait(false);
                    toDelete.DeleteAfter(5);
                }
                catch { }
            }

            private async void MuteCommands_UserMuted(IGuildUser usr, MuteCommands.MuteType muteType)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.UserMutedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) == null)
                        return;
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
                    await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🔇 **| User muted from the {mutes}. |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void MuteCommands_UserUnmuted(IGuildUser usr, MuteCommands.MuteType muteType)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.UserMutedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserMuted)) == null)
                        return;

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
                    await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🔊 **| User unmuted from the {mutes}. |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            public static async Task TriggeredAntiProtection(IGuildUser[] users, PunishmentAction action, ProtectionType protection)
            {
                try
                {
                    if (users.Length == 0)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(users.First().Guild.Id, out logSetting)
                        || (logSetting.LogOtherId == null))
                        return;
                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(users.First().Guild, logSetting, LogType.Other)) == null)
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
                    await logChannel.SendMessageAsync(String.Join("\n", users.Select(user => $"‼️ {Format.Bold(user.ToString())} got **{punishment}** due to __**{protection}**__ protection on **{user.Guild.Name}** server.")))
                                    //await logChannel.SendMessageAsync(String.Join("\n",users.Select(user=>$"{Format.Bold(user.ToString())} was **{punishment}** due to `{protection}` protection on **{user.Guild.Name}** server.")))
                                    .ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_UserUpdated(SocketUser uBefore, SocketUser uAfter)
            {
                try
                {
                    var before = uBefore as SocketGuildUser;
                    if (before == null)
                        return;
                    var after = uAfter as SocketGuildUser;
                    if (after == null)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                        || (logSetting.UserUpdatedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.UserUpdated)) == null)
                        return;
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
                    else if (!before.RoleIds.SequenceEqual(after.RoleIds))
                    {
                        if (before.RoleIds.Count < after.RoleIds.Count)
                        {
                            var diffRoles = after.RoleIds.Where(r => !before.RoleIds.Contains(r)).Select(r => "**" + before.Guild.GetRole(r).Name + "**");
                            //str += $"**User's Roles changed ⚔➕**👤`{before.ToString()}`\n\tNow has {string.Join(", ", diffRoles)} role.";
                            str += $"👤__**{before.ToString()}**__ **| User's Role Added |** 🆔 `{before.Id}`\n\t✅ {string.Join(", ", diffRoles).SanitizeMentions()}\n\t\t⚔ **`{string.Join(", ", after.GetRoles().Select(r => r.Name)).SanitizeMentions()}`** ⚔";
                        }
                        else if (before.RoleIds.Count > after.RoleIds.Count)
                        {
                            var diffRoles = before.RoleIds.Where(r => !after.RoleIds.Contains(r)).Select(r => "**" + before.Guild.GetRole(r).Name + "**");
                            //str += $"**User's Roles changed **`{before.ToString()}`\n\tNo longer has {string.Join(", ", diffRoles)} role.";
                            str += $"👤__**{before.ToString()}**__ **| User's Role Removed |** 🆔 `{before.Id}`\n\t🚮 {string.Join(", ", diffRoles).SanitizeMentions()}\n\t\t⚔ **`{string.Join(", ", after.GetRoles().Select(r => r.Name)).SanitizeMentions()}`** ⚔";
                        }
                    }
                    else
                        return;
                    try { await logChannel.SendMessageAsync(str).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
            {
                try
                {
                    var before = cbefore as IGuildChannel;
                    if (before == null)
                        return;
                    var after = (IGuildChannel)cafter;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                        || (logSetting.ChannelUpdatedId == null)
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == after.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.ChannelUpdated)) == null)
                        return;
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
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_ChannelDestroyed(IChannel ich)
            {
                try
                {
                    var ch = ich as IGuildChannel;
                    if (ch == null)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out logSetting)
                        || (logSetting.ChannelDestroyedId == null)
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == ch.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelDestroyed)) == null)
                        return;

                    await logChannel.SendMessageAsync($"🕕`{prettyCurrentTime}`🗑 **| {(ch is IVoiceChannel ? "Voice" : "Text")} Channel Deleted #⃣ {ch.Name}** `({ch.Id})`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_ChannelCreated(IChannel ich)
            {
                try
                {
                    var ch = ich as IGuildChannel;
                    if (ch == null)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(ch.Guild.Id, out logSetting)
                        || (logSetting.ChannelCreatedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(ch.Guild, logSetting, LogType.ChannelCreated)) == null)
                        return;

                    await logChannel.SendMessageAsync($"🕓`{prettyCurrentTime}`🆕 **| {(ch is IVoiceChannel ? "Voice" : "Text")} Channel Created: #⃣ {ch.Name}** `({ch.Id})`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
            {
                try
                {
                    var usr = iusr as IGuildUser;
                    if (usr == null)
                        return;

                    var beforeVch = before.VoiceChannel;
                    var afterVch = after.VoiceChannel;

                    if (beforeVch == afterVch)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.LogVoicePresenceId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.VoicePresence)) == null)
                        return;

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
                    if (str != null)
                        UserPresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }

            private async void _client_UserPresenceUpdated(Optional<SocketGuild> optGuild, SocketUser usr, SocketPresence before, SocketPresence after)
            {
                try
                {
                    var guild = optGuild.IsSpecified ? optGuild.Value : null;

                    if (guild == null)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(guild.Id, out logSetting)
                        || (logSetting.LogUserPresenceId == null)
                        || before.Status == after.Status)
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserPresence)) == null)
                        return;
                    string str;
                    if (before.Status != after.Status)
                        str = $"🔵`{prettyCurrentTime}`👤__**{usr.Username}**__ is now **{after.Status}**.";
                    else
                        str = $"👾`{prettyCurrentTime}`👤__**{usr.Username}**__ is now playing **{after.Game}**.";

                    UserPresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch { }
            }

            private async void _client_UserLeft(IGuildUser usr)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.UserLeftId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserLeft)) == null)
                        return;
                    await logChannel.SendMessageAsync($"❗️🕛`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__❌ **| USER LEFT |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch { }
            }

            private async void _client_UserJoined(IGuildUser usr)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(usr.Guild.Id, out logSetting)
                        || (logSetting.UserJoinedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(usr.Guild, logSetting, LogType.UserJoined)) == null)
                        return;

                    await logChannel.SendMessageAsync($"❕🕓`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__✅ **| USER JOINED |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_UserUnbanned(IUser usr, IGuild guild)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(guild.Id, out logSetting)
                        || (logSetting.UserUnbannedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserUnbanned)) == null)
                        return;

                    await logChannel.SendMessageAsync($"❕🕘`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__♻️ **| USER UN-BANNED |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_UserBanned(IUser usr, IGuild guild)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(guild.Id, out logSetting)
                        || (logSetting.UserBannedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(guild, logSetting, LogType.UserBanned)) == null)
                        return;
                    await logChannel.SendMessageAsync($"‼️🕕`{prettyCurrentTime}`👤__**{usr.Username}#{usr.Discriminator}**__🚫 **| USER BANNED |** 🆔 `{usr.Id}`").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_MessageDeleted(ulong arg1, Optional<SocketMessage> imsg)
            {

                try
                {
                    var msg = (imsg.IsSpecified ? imsg.Value : null) as IUserMessage;
                    if (msg == null || msg.IsAuthor())
                        return;

                    var channel = msg.Channel as ITextChannel;
                    if (channel == null)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                        || (logSetting.MessageDeletedId == null)
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageDeleted)) == null || logChannel.Id == msg.Id)
                        return;
                    var str = $@"🕔`{prettyCurrentTime}`👤__**{msg.Author.Username}#{msg.Author.Discriminator}**__ **| Deleted Message |** 🆔 `{msg.Author.Id}` #⃣ `{channel.Name}`
🗑 {msg.Resolve(userHandling: TagHandling.FullName)}";
                    if (msg.Attachments.Any())
                        str += $"{Environment.NewLine}📎 {string.Join(", ", msg.Attachments.Select(a => a.ProxyUrl))}";
                    await logChannel.SendMessageAsync(str.SanitizeMentions()).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private async void _client_MessageUpdated(Optional<SocketMessage> optmsg, SocketMessage imsg2)
            {
                try
                {
                    var after = imsg2 as IUserMessage;
                    if (after == null || after.IsAuthor())
                        return;

                    var before = (optmsg.IsSpecified ? optmsg.Value : null) as IUserMessage;
                    if (before == null)
                        return;

                    var channel = after.Channel as ITextChannel;
                    if (channel == null)
                        return;

                    if (before.Content == after.Content)
                        return;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(channel.Guild.Id, out logSetting)
                        || (logSetting.MessageUpdatedId == null)
                        || logSetting.IgnoredChannels.Any(ilc => ilc.ChannelId == channel.Id))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(channel.Guild, logSetting, LogType.MessageUpdated)) == null || logChannel.Id == after.Channel.Id)
                        return;
                    await logChannel.SendMessageAsync($@"🕔`{prettyCurrentTime}`👤__**{before.Author.Username}#{before.Author.Discriminator}**__ **| 📝 Edited Message |** 🆔 `{before.Author.Id}` #⃣ `{channel.Name}`
        `Old:` {before.Resolve(userHandling: TagHandling.FullName).SanitizeMentions()}
        **`New:`** {after.Resolve(userHandling: TagHandling.FullName).SanitizeMentions()}").ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            public enum LogType
            {
                Other,
                MessageUpdated,
                MessageDeleted,
                UserJoined,
                UserLeft,
                UserBanned,
                UserUnbanned,
                UserUpdated,
                ChannelCreated,
                ChannelDestroyed,
                ChannelUpdated,
                UserPresence,
                VoicePresence,
                VoicePresenceTTS,
                UserMuted
            };

            private static async Task<ITextChannel> TryGetLogChannel(IGuild guild, LogSetting logSetting, LogType logChannelType)
            {
                ulong? id = null;
                switch (logChannelType)
                {
                    case LogType.Other:
                        id = logSetting.LogOtherId;
                        break;
                    case LogType.MessageUpdated:
                        id = logSetting.MessageUpdatedId;
                        break;
                    case LogType.MessageDeleted:
                        id = logSetting.MessageDeletedId;
                        break;
                    case LogType.UserJoined:
                        id = logSetting.UserJoinedId;
                        break;
                    case LogType.UserLeft:
                        id = logSetting.UserLeftId;
                        break;
                    case LogType.UserBanned:
                        id = logSetting.UserBannedId;
                        break;
                    case LogType.UserUnbanned:
                        id = logSetting.UserUnbannedId;
                        break;
                    case LogType.UserUpdated:
                        id = logSetting.UserUpdatedId;
                        break;
                    case LogType.ChannelCreated:
                        id = logSetting.ChannelCreatedId;
                        break;
                    case LogType.ChannelDestroyed:
                        id = logSetting.ChannelDestroyedId;
                        break;
                    case LogType.ChannelUpdated:
                        id = logSetting.ChannelUpdatedId;
                        break;
                    case LogType.UserPresence:
                        id = logSetting.LogUserPresenceId;
                        break;
                    case LogType.VoicePresence:
                        id = logSetting.LogVoicePresenceId;
                        break;
                    case LogType.VoicePresenceTTS:
                        id = logSetting.LogVoicePresenceTTSId;
                        break;
                    case LogType.UserMuted:
                        id = logSetting.UserMutedId;
                        break;
                    default:
                        break;
                }

                if (!id.HasValue)
                {
                    UnsetLogSetting(guild.Id, logChannelType);
                    return null;
                }
                var channel = await guild.GetTextChannelAsync(id.Value).ConfigureAwait(false);

                if (channel == null)
                {
                    UnsetLogSetting(guild.Id, logChannelType);
                    return null;
                }
                else
                    return channel;
            }

            private static void UnsetLogSetting(ulong guildId, LogType logChannelType)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var newLogSetting = uow.GuildConfigs.LogSettingsFor(guildId).LogSetting;
                    switch (logChannelType)
                    {
                        case LogType.Other:
                            newLogSetting.LogOtherId = null;
                            break;
                        case LogType.MessageUpdated:
                            newLogSetting.MessageUpdatedId = null;
                            break;
                        case LogType.MessageDeleted:
                            newLogSetting.MessageDeletedId = null;
                            break;
                        case LogType.UserJoined:
                            newLogSetting.UserJoinedId = null;
                            break;
                        case LogType.UserLeft:
                            newLogSetting.UserLeftId = null;
                            break;
                        case LogType.UserBanned:
                            newLogSetting.UserBannedId = null;
                            break;
                        case LogType.UserUnbanned:
                            newLogSetting.UserUnbannedId = null;
                            break;
                        case LogType.UserUpdated:
                            newLogSetting.UserUpdatedId = null;
                            break;
                        case LogType.UserMuted:
                            newLogSetting.UserMutedId = null;
                            break;
                        case LogType.ChannelCreated:
                            newLogSetting.ChannelCreatedId = null;
                            break;
                        case LogType.ChannelDestroyed:
                            newLogSetting.ChannelDestroyedId = null;
                            break;
                        case LogType.ChannelUpdated:
                            newLogSetting.ChannelUpdatedId = null;
                            break;
                        case LogType.UserPresence:
                            newLogSetting.LogUserPresenceId = null;
                            break;
                        case LogType.VoicePresence:
                            newLogSetting.LogVoicePresenceId = null;
                            break;
                        case LogType.VoicePresenceTTS:
                            newLogSetting.LogVoicePresenceTTSId = null;
                            break;
                        default:
                            break;
                    }
                    GuildLogSettings.AddOrUpdate(guildId, newLogSetting, (gid, old) => newLogSetting);
                    uow.Complete();
                }
            }

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
                var channel = (ITextChannel)Context.Channel;
                LogSetting logSetting;
                using (var uow = DbHandler.UnitOfWork())
                {
                    logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    logSetting.LogOtherId =
                    logSetting.MessageUpdatedId =
                    logSetting.MessageDeletedId =
                    logSetting.UserJoinedId =
                    logSetting.UserLeftId =
                    logSetting.UserBannedId =
                    logSetting.UserUnbannedId =
                    logSetting.UserUpdatedId =
                    logSetting.ChannelCreatedId =
                    logSetting.ChannelDestroyedId =
                    logSetting.ChannelUpdatedId =
                    logSetting.LogUserPresenceId =
                    logSetting.LogVoicePresenceId =
                    logSetting.LogVoicePresenceTTSId = (action.Value ? channel.Id : (ulong?)null);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (action.Value)
                    await channel.SendMessageAsync("✅ Logging all events on this channel.").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync("ℹ️ Logging disabled.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogIgnore()
            {
                var channel = (ITextChannel)Context.Channel;
                int removed;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id);
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogEvents()
            {
                await Context.Channel.SendConfirmAsync("Log events you can subscribe to:", String.Join(", ", Enum.GetNames(typeof(LogType)).Cast<string>()));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task Log(LogType type)
            {
                var channel = (ITextChannel)Context.Channel;
                ulong? channelId = null;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    switch (type)
                    {
                        case LogType.Other:
                            channelId = logSetting.LogOtherId = (logSetting.LogOtherId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.MessageUpdated:
                            channelId = logSetting.MessageUpdatedId = (logSetting.MessageUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.MessageDeleted:
                            channelId = logSetting.MessageDeletedId = (logSetting.MessageDeletedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserJoined:
                            channelId = logSetting.UserJoinedId = (logSetting.UserJoinedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserLeft:
                            channelId = logSetting.UserLeftId = (logSetting.UserLeftId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserBanned:
                            channelId = logSetting.UserBannedId = (logSetting.UserBannedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserUnbanned:
                            channelId = logSetting.UserUnbannedId = (logSetting.UserUnbannedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserUpdated:
                            channelId = logSetting.UserUpdatedId = (logSetting.UserUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserMuted:
                            channelId = logSetting.UserMutedId = (logSetting.UserMutedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.ChannelCreated:
                            channelId = logSetting.ChannelCreatedId = (logSetting.ChannelCreatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.ChannelDestroyed:
                            channelId = logSetting.ChannelDestroyedId = (logSetting.ChannelDestroyedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.ChannelUpdated:
                            channelId = logSetting.ChannelUpdatedId = (logSetting.ChannelUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.UserPresence:
                            channelId = logSetting.LogUserPresenceId = (logSetting.LogUserPresenceId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.VoicePresence:
                            channelId = logSetting.LogVoicePresenceId = (logSetting.LogVoicePresenceId == null ? channel.Id : default(ulong?));
                            break;
                        case LogType.VoicePresenceTTS:
                            channelId = logSetting.LogVoicePresenceTTSId = (logSetting.LogVoicePresenceTTSId == null ? channel.Id : default(ulong?));
                            break;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (channelId != null)
                    await channel.SendMessageAsync($"✅ Logging `{type}` event in #⃣ `{channel.Name} ({channel.Id})`").ConfigureAwait(false);
                else
                    await channel.SendMessageAsync($"ℹ️ Stopped logging `{type}` event.").ConfigureAwait(false);
            }
        }
    }
}