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
            private const string clockEmojiUrl = "https://cdn.discordapp.com/attachments/155726317222887425/258309524966866945/clock.png";

            private static DiscordShardedClient _client { get; }
            private static Logger _log { get; }

            private static string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";
            private static string currentTime => $"{DateTime.Now:HH:mm:ss}";

            public static ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

            private static ConcurrentDictionary<ITextChannel, List<string>> PresenceUpdates { get; } = new ConcurrentDictionary<ITextChannel, List<string>>();
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
                        var keys = PresenceUpdates.Keys.ToList();

                        await Task.WhenAll(keys.Select(async key =>
                        {
                            List<string> messages;
                            if (PresenceUpdates.TryRemove(key, out messages))
                                try { await key.SendConfirmAsync("Presence Updates", string.Join(Environment.NewLine, messages)); } catch { }
                        }));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");

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
                _client.GuildMemberUpdated += _client_GuildUserUpdated;
#if !GLOBAL_NADEKO
                _client.UserUpdated += _client_UserUpdated;
#endif

                _client.ChannelCreated += _client_ChannelCreated;
                _client.ChannelDestroyed += _client_ChannelDestroyed;
                _client.ChannelUpdated += _client_ChannelUpdated;

                MuteCommands.UserMuted += MuteCommands_UserMuted;
                MuteCommands.UserUnmuted += MuteCommands_UserUnmuted;
            }

            private static async Task _client_UserUpdated(SocketUser before, SocketUser uAfter)
            {
                try
                {
                    var after = uAfter as SocketGuildUser;

                    if (after == null)
                        return;

                    var g = after.Guild;

                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(g.Id, out logSetting)
                        || (logSetting.UserUpdatedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(g, logSetting, LogType.UserUpdated)) == null)
                        return;

                    var embed = new EmbedBuilder();


                    if (before.Username != after.Username)
                    {
                        embed.WithTitle("👥 Username Changed")
                            .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .AddField(fb => fb.WithName("Old Name").WithValue($"{before.Username}").WithIsInline(true))
                            .AddField(fb => fb.WithName("New Name").WithValue($"{after.Username}").WithIsInline(true))
                            .WithFooter(fb => fb.WithText(currentTime))
                            .WithOkColor();
                    }
                    else if (before.AvatarUrl != after.AvatarUrl)
                    {
                        embed.WithTitle("👥 Avatar Changed")
                            .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .WithTitle($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .WithThumbnailUrl(before.AvatarUrl)
                            .WithImageUrl(after.AvatarUrl)
                            .WithFooter(fb => fb.WithText(currentTime))
                            .WithOkColor();
                    }
                    else
                    {
                        return;
                    }

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);

                    //var guildsMemberOf = NadekoBot.Client.GetGuilds().Where(g => g.Users.Select(u => u.Id).Contains(before.Id)).ToList();
                    //foreach (var g in guildsMemberOf)
                    //{
                    //    LogSetting logSetting;
                    //    if (!GuildLogSettings.TryGetValue(g.Id, out logSetting)
                    //        || (logSetting.UserUpdatedId == null))
                    //        return;

                    //    ITextChannel logChannel;
                    //    if ((logChannel = await TryGetLogChannel(g, logSetting, LogType.UserUpdated)) == null)
                    //        return;

                    //    try { await logChannel.SendMessageAsync(str).ConfigureAwait(false); } catch { }
                    //}
                }
                catch
                { }
            }

            private static async Task _client_UserVoiceStateUpdated_TTS(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
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

                    var str = "";
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

            private static async void MuteCommands_UserMuted(IGuildUser usr, MuteCommands.MuteType muteType)
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

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName("🔇 User Muted from " + mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText(currentTime))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
            }

            private static async void MuteCommands_UserUnmuted(IGuildUser usr, MuteCommands.MuteType muteType)
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

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName("🔊 User Unmuted from " + mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText($"{currentTime}"))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
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
                    }
                    else if (action == PunishmentAction.Kick)
                    {
                        punishment = "☣ SOFT-BANNED (KICKED)";
                    }
                    else if (action == PunishmentAction.Ban)
                    {
                        punishment = "⛔️ BANNED";
                    }

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName($"🛡 Anti-{protection}"))
                            .WithTitle($"Users " + punishment)
                            .WithDescription(String.Join("\n", users.Select(u => u.ToString())))
                            .WithFooter(fb => fb.WithText($"{currentTime}"))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_GuildUserUpdated(SocketGuildUser before, SocketGuildUser after)
            {
                try
                {
                    LogSetting logSetting;
                    if (!GuildLogSettings.TryGetValue(before.Guild.Id, out logSetting)
                        || (logSetting.UserUpdatedId == null))
                        return;

                    ITextChannel logChannel;
                    if ((logChannel = await TryGetLogChannel(before.Guild, logSetting, LogType.UserUpdated)) == null)
                        return;
                    var embed = new EmbedBuilder().WithOkColor().WithFooter(efb => efb.WithText(currentTime))
                        .WithTitle($"{before.Username}#{before.Discriminator} | {before.Id}");
                    if (before.Nickname != after.Nickname)
                    {
                        embed.WithAuthor(eab => eab.WithName("👥 Nickname Changed"))

                            .AddField(efb => efb.WithName("Old Nickname").WithValue($"{before.Nickname}#{before.Discriminator}"))
                            .AddField(efb => efb.WithName("New Nickname").WithValue($"{after.Nickname}#{after.Discriminator}"));
                    }
                    else if (!before.RoleIds.SequenceEqual(after.RoleIds))
                    {
                        if (before.RoleIds.Count < after.RoleIds.Count)
                        {
                            var diffRoles = after.RoleIds.Where(r => !before.RoleIds.Contains(r)).Select(r => before.Guild.GetRole(r).Name);
                            embed.WithAuthor(eab => eab.WithName("⚔ User's Role Added"))
                                .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                        }
                        else if (before.RoleIds.Count > after.RoleIds.Count)
                        {
                            var diffRoles = before.RoleIds.Where(r => !after.RoleIds.Contains(r)).Select(r => before.Guild.GetRole(r).Name);
                            embed.WithAuthor(eab => eab.WithName("⚔ User's Role Removed"))
                                .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                        }
                    }
                    else
                        return;
                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_ChannelUpdated(IChannel cbefore, IChannel cafter)
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

                    var embed = new EmbedBuilder().WithOkColor().WithFooter(efb => efb.WithText(currentTime));

                    var beforeTextChannel = cbefore as ITextChannel;
                    var afterTextChannel = cafter as ITextChannel;

                    if (before.Name != after.Name)
                    {
                        embed.WithTitle("ℹ️ Channel Name Changed")
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName("Old Name").WithValue(before.Name));
                    }
                    else if (beforeTextChannel?.Topic != afterTextChannel?.Topic)
                    {
                        embed.WithTitle("ℹ️ Channel Topic Changed")
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName("Old Topic").WithValue(beforeTextChannel.Topic))
                            .AddField(efb => efb.WithName("New Topic").WithValue(afterTextChannel.Topic));
                    }
                    else
                        return;

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_ChannelDestroyed(IChannel ich)
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

                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + (ch is IVoiceChannel ? "Voice" : "Text") + " Channel Destroyed")
                        .WithDescription($"{ch.Name} | {ch.Id}")
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_ChannelCreated(IChannel ich)
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

                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + (ch is IVoiceChannel ? "Voice" : "Text") + " Channel Created")
                        .WithDescription($"{ch.Name} | {ch.Id}")
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private static async Task _client_UserVoiceStateUpdated(SocketUser iusr, SocketVoiceState before, SocketVoiceState after)
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
                        PresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch { }
            }

            private static async Task _client_UserPresenceUpdated(Optional<SocketGuild> optGuild, SocketUser usr, SocketPresence before, SocketPresence after)
            {
                try
                {
                    var guild = optGuild.GetValueOrDefault() ?? (usr as SocketGuildUser)?.Guild;

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
                    string str = "";
                    if (before.Status != after.Status)
                        str = $"🎭`{prettyCurrentTime}`👤__**{usr.Username}**__ is now **{after.Status}**.";

                    //if (before.Game?.Name != after.Game?.Name)
                    //{
                    //    if (str != "")
                    //        str += "\n";
                    //    str += $"👾`{prettyCurrentTime}`👤__**{usr.Username}**__ is now playing **{after.Game?.Name}**.";
                    //}

                    PresenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch { }
            }

            private static async Task _client_UserLeft(IGuildUser usr)
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

                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("❌ User Left")
                        .WithThumbnailUrl(usr.AvatarUrl)
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_UserJoined(IGuildUser usr)
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

                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("✅ User Joined")
                        .WithThumbnailUrl(usr.AvatarUrl)
                        .WithDescription($"{usr}")
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private static async Task _client_UserUnbanned(IUser usr, IGuild guild)
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

                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("♻️ User Unbanned")
                        .WithThumbnailUrl(usr.AvatarUrl)
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private static async Task _client_UserBanned(IUser usr, IGuild guild)
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
                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🚫 User Banned")
                        .WithThumbnailUrl(usr.AvatarUrl)
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private static async Task _client_MessageDeleted(ulong arg1, Optional<SocketMessage> imsg)
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
                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle($"🗑 Message Deleted in {((ITextChannel)msg.Channel).Mention}")
                        .WithDescription($"{msg.Author}")
                        .AddField(efb => efb.WithName("Content").WithValue(msg.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(msg.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(currentTime));
                    if (msg.Attachments.Any())
                        embed.AddField(efb => efb.WithName("Attachments").WithValue(string.Join(", ", msg.Attachments.Select(a => a.ProxyUrl))).WithIsInline(false));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
            }

            private static async Task _client_MessageUpdated(Optional<SocketMessage> optmsg, SocketMessage imsg2)
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

                    var embed = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle($"📝 Message Updated in {((ITextChannel)after.Channel).Mention}")
                        .WithDescription(after.Author.ToString())
                        .AddField(efb => efb.WithName("Old Message").WithValue(before.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("New Message").WithValue(after.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(after.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(currentTime));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch { }
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
                    logSetting.UserMutedId =
                    logSetting.LogVoicePresenceTTSId = (action.Value ? channel.Id : (ulong?)null);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (action.Value)
                    await channel.SendConfirmAsync("Logging all events in this channel.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync("Logging disabled.").ConfigureAwait(false);
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
                    await channel.SendConfirmAsync($"Logging will IGNORE **{channel.Mention} ({channel.Id})**").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync($"Logging will NOT IGNORE **{channel.Mention} ({channel.Id})**").ConfigureAwait(false);
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
                    await channel.SendConfirmAsync($"Logging **{type}** event in this channel.").ConfigureAwait(false);
                else
                    await channel.SendConfirmAsync($"Stopped logging **{type}** event.").ConfigureAwait(false);
            }
        }
    }
}