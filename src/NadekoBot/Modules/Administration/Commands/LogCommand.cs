using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        public class LogCommands : NadekoSubmodule
        {
            private static DiscordShardedClient client { get; }
            private new static Logger _log { get; }

            private static string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";
            private static string currentTime => $"{DateTime.Now:HH:mm:ss}";

            public static ConcurrentDictionary<ulong, LogSetting> GuildLogSettings { get; }

            private static ConcurrentDictionary<ITextChannel, List<string>> presenceUpdates { get; } = new ConcurrentDictionary<ITextChannel, List<string>>();
            private static readonly Timer _timerReference;

            static LogCommands()
            {
                client = NadekoBot.Client;
                _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
                
                GuildLogSettings = new ConcurrentDictionary<ulong, LogSetting>(NadekoBot.AllGuildConfigs
                    .ToDictionary(g => g.GuildId, g => g.LogSetting));

                _timerReference = new Timer(async (state) =>
                {
                    try
                    {
                        var keys = presenceUpdates.Keys.ToList();

                        await Task.WhenAll(keys.Select(async key =>
                        {
                            List<string> messages;
                            if (presenceUpdates.TryRemove(key, out messages))
                                try { await key.SendConfirmAsync(key.Guild.GetLogText("presence_updates"), string.Join(Environment.NewLine, messages)); }
                                catch
                                {
                                    // ignored
                                }
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
                client.MessageUpdated += _client_MessageUpdated;
                client.MessageDeleted += _client_MessageDeleted;
                client.UserBanned += _client_UserBanned;
                client.UserUnbanned += _client_UserUnbanned;
                client.UserJoined += _client_UserJoined;
                client.UserLeft += _client_UserLeft;
                client.UserPresenceUpdated += _client_UserPresenceUpdated;
                client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
                client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated_TTS;
                client.GuildMemberUpdated += _client_GuildUserUpdated;
#if !GLOBAL_NADEKO
                client.UserUpdated += _client_UserUpdated;
#endif
                client.ChannelCreated += _client_ChannelCreated;
                client.ChannelDestroyed += _client_ChannelDestroyed;
                client.ChannelUpdated += _client_ChannelUpdated;

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
                        embed.WithTitle("👥 " + g.GetLogText("username_changed"))
                            .WithDescription($"{before.Username}#{before.Discriminator} | {before.Id}")
                            .AddField(fb => fb.WithName("Old Name").WithValue($"{before.Username}").WithIsInline(true))
                            .AddField(fb => fb.WithName("New Name").WithValue($"{after.Username}").WithIsInline(true))
                            .WithFooter(fb => fb.WithText(currentTime))
                            .WithOkColor();
                    }
                    else if (before.AvatarUrl != after.AvatarUrl)
                    {
                        embed.WithTitle("👥" + g.GetLogText("avatar_changed"))
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
                {
                    // ignored
                }
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
                        str = logChannel.Guild.GetLogText("moved", usr.Username, beforeVch?.Name, afterVch?.Name);
                    }
                    else if (beforeVch == null)
                    {
                        str = logChannel.Guild.GetLogText("joined", usr.Username, afterVch.Name);
                    }
                    else if (afterVch == null)
                    {
                        str = logChannel.Guild.GetLogText("left", usr.Username, beforeVch.Name);
                    }
                    var toDelete = await logChannel.SendMessageAsync(str, true).ConfigureAwait(false);
                    toDelete.DeleteAfter(5);
                }
                catch
                {
                    // ignored
                }
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
                    var mutes = "";
                    var mutedLocalized = logChannel.Guild.GetLogText("muted_sn");
                    switch (muteType)
                    {
                        case MuteCommands.MuteType.Voice:
                            mutes = "🔇 " + logChannel.Guild.GetLogText("xmuted_voice", mutedLocalized);
                            break;
                        case MuteCommands.MuteType.Chat:
                            mutes = "🔇 " + logChannel.Guild.GetLogText("xmuted_text", mutedLocalized);
                            break;
                        case MuteCommands.MuteType.All:
                            mutes = "🔇 " + logChannel.Guild.GetLogText("xmuted_text_and_voice", mutedLocalized);
                            break;
                    }

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName(mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText(currentTime))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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

                    var mutes = "";
                    var unmutedLocalized = logChannel.Guild.GetLogText("unmuted_sn");
                    switch (muteType)
                    {
                        case MuteCommands.MuteType.Voice:
                            mutes = "🔊 " + logChannel.Guild.GetLogText("xmuted_voice", unmutedLocalized);
                            break;
                        case MuteCommands.MuteType.Chat:
                            mutes = "🔊 " + logChannel.Guild.GetLogText("xmuted_text", unmutedLocalized);
                            break;
                        case MuteCommands.MuteType.All:
                            mutes = "🔊 " + logChannel.Guild.GetLogText("xmuted_text_and_voice", unmutedLocalized);
                            break;
                    }

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName(mutes))
                            .WithTitle($"{usr.Username}#{usr.Discriminator} | {usr.Id}")
                            .WithFooter(fb => fb.WithText($"{currentTime}"))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                    switch (action)
                    {
                        case PunishmentAction.Mute:
                            punishment = "🔇 " + logChannel.Guild.GetLogText("muted_pl").ToUpperInvariant();
                            break;
                        case PunishmentAction.Kick:
                            punishment = "☣ " + logChannel.Guild.GetLogText("soft_banned_pl").ToUpperInvariant();
                            break;
                        case PunishmentAction.Ban:
                            punishment = "⛔️ " + logChannel.Guild.GetLogText("banned_pl").ToUpperInvariant();
                            break;
                    }

                    var embed = new EmbedBuilder().WithAuthor(eab => eab.WithName($"🛡 Anti-{protection}"))
                            .WithTitle(logChannel.Guild.GetLogText("users") + " " + punishment)
                            .WithDescription(string.Join("\n", users.Select(u => u.ToString())))
                            .WithFooter(fb => fb.WithText($"{currentTime}"))
                            .WithOkColor();

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                        embed.WithAuthor(eab => eab.WithName("👥 " + logChannel.Guild.GetLogText("nick_change")))

                            .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("old_nick")).WithValue($"{before.Nickname}#{before.Discriminator}"))
                            .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("new_nick")).WithValue($"{after.Nickname}#{after.Discriminator}"));
                    }
                    else if (!before.RoleIds.SequenceEqual(after.RoleIds))
                    {
                        if (before.RoleIds.Count < after.RoleIds.Count)
                        {
                            var diffRoles = after.RoleIds.Where(r => !before.RoleIds.Contains(r)).Select(r => before.Guild.GetRole(r).Name);
                            embed.WithAuthor(eab => eab.WithName("⚔ " + logChannel.Guild.GetLogText("user_role_add")))
                                .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                        }
                        else if (before.RoleIds.Count > after.RoleIds.Count)
                        {
                            var diffRoles = before.RoleIds.Where(r => !after.RoleIds.Contains(r)).Select(r => before.Guild.GetRole(r).Name);
                            embed.WithAuthor(eab => eab.WithName("⚔ " + logChannel.Guild.GetLogText("user_role_rem")))
                                .WithDescription(string.Join(", ", diffRoles).SanitizeMentions());
                        }
                    }
                    else
                        return;
                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                        embed.WithTitle("ℹ️ " + logChannel.Guild.GetLogText("ch_name_change"))
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("ch_old_name")).WithValue(before.Name));
                    }
                    else if (beforeTextChannel?.Topic != afterTextChannel?.Topic)
                    {
                        embed.WithTitle("ℹ️ " + logChannel.Guild.GetLogText("ch_topic_change"))
                            .WithDescription($"{after} | {after.Id}")
                            .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("old_topic")).WithValue(beforeTextChannel?.Topic ?? "-"))
                            .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("new_topic")).WithValue(afterTextChannel?.Topic ?? "-"));
                    }
                    else
                        return;

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                    string title;
                    if (ch is IVoiceChannel)
                    {
                        title = logChannel.Guild.GetLogText("voice_chan_destroyed");
                    }
                    else
                        title = logChannel.Guild.GetLogText("text_chan_destroyed");
                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + title)
                        .WithDescription($"{ch.Name} | {ch.Id}")
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                    string title;
                    if (ch is IVoiceChannel)
                    {
                        title = logChannel.Guild.GetLogText("voice_chan_created");
                    }
                    else
                        title = logChannel.Guild.GetLogText("text_chan_created");
                    await logChannel.EmbedAsync(new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("🆕 " + title)
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
                        str = "🎙" + Format.Code(prettyCurrentTime) + logChannel.Guild.GetLogText("user_vmoved",
                                "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(beforeVch?.Name ?? ""), Format.Bold(afterVch?.Name ?? ""));
                    }
                    else if (beforeVch == null)
                    {
                        str = "🎙" + Format.Code(prettyCurrentTime) + logChannel.Guild.GetLogText("user_vjoined",
                                "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(afterVch.Name ?? ""));
                    }
                    else if (afterVch == null)
                    {
                        str = "🎙" + Format.Code(prettyCurrentTime) + logChannel.Guild.GetLogText("user_vleft",
                                "👤" + Format.Code(prettyCurrentTime), "👤" + Format.Bold(usr.Username + "#" + usr.Discriminator),
                                Format.Bold(beforeVch.Name ?? ""));
                    }
                    if (str != null)
                        presenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch
                {
                    // ignored
                }
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
                        str = "🎭" + Format.Code(prettyCurrentTime) +
                              logChannel.Guild.GetLogText("user_status_change",
                                    "👤" + Format.Bold(usr.Username),
                                    Format.Bold(after.Status.ToString()));

                    //if (before.Game?.Name != after.Game?.Name)
                    //{
                    //    if (str != "")
                    //        str += "\n";
                    //    str += $"👾`{prettyCurrentTime}`👤__**{usr.Username}**__ is now playing **{after.Game?.Name}**.";
                    //}

                    presenceUpdates.AddOrUpdate(logChannel, new List<string>() { str }, (id, list) => { list.Add(str); return list; });
                }
                catch
                {
                    // ignored
                }
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
                        .WithTitle("❌ " + logChannel.Guild.GetLogText("user_left"))
                        .WithThumbnailUrl(usr.AvatarUrl)
                        .WithDescription(usr.ToString())
                        .AddField(efb => efb.WithName("Id").WithValue(usr.Id.ToString()))
                        .WithFooter(efb => efb.WithText(currentTime))).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                        .WithTitle("✅ " + logChannel.Guild.GetLogText("user_joined"))
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
                        .WithTitle("♻️ " + logChannel.Guild.GetLogText("user_unbanned"))
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
                        .WithTitle("🚫 " + logChannel.Guild.GetLogText("user_banned"))
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
                        .WithTitle("🗑 " + logChannel.Guild.GetLogText("msg_del", ((ITextChannel)msg.Channel).Name))
                        .WithDescription(msg.Author.ToString())
                        .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("content")).WithValue(string.IsNullOrWhiteSpace(msg.Content) ? "-" : msg.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(msg.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(currentTime));
                    if (msg.Attachments.Any())
                        embed.AddField(efb => efb.WithName(logChannel.Guild.GetLogText("attachments")).WithValue(string.Join(", ", msg.Attachments.Select(a => a.Url))).WithIsInline(false));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    // ignored
                }
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
                        .WithTitle("📝 " + logChannel.Guild.GetLogText("msg_update", ((ITextChannel)after.Channel).Name))
                        .WithDescription(after.Author.ToString())
                        .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("old_msg")).WithValue(string.IsNullOrWhiteSpace(before.Content) ? "-" : before.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName(logChannel.Guild.GetLogText("new_msg")).WithValue(string.IsNullOrWhiteSpace(after.Content) ? "-" : after.Resolve(userHandling: TagHandling.FullName)).WithIsInline(false))
                        .AddField(efb => efb.WithName("Id").WithValue(after.Id.ToString()).WithIsInline(false))
                        .WithFooter(efb => efb.WithText(currentTime));

                    await logChannel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
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
                await Context.Channel.SendConfirmAsync(GetText("log_events") + "\n" +
                                                       string.Join(", ", Enum.GetNames(typeof(LogType)).Cast<string>()))
                    .ConfigureAwait(false);
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
                    await ReplyConfirmLocalized("log", Format.Bold(type.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_stop", Format.Bold(type.ToString())).ConfigureAwait(false);
            }
        }
    }

    public static class GuildExtensions
    {
        public static string GetLogText(this IGuild guild, string key, params object[] replacements)
            => NadekoTopLevelModule.GetTextStatic(key,
                NadekoBot.Localization.GetCultureInfo(guild),
                typeof(Administration).Name.ToLowerInvariant(),
                replacements);
    }
}