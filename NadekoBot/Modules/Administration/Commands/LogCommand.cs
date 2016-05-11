using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class LogCommand : DiscordCommand
    {

        private readonly ConcurrentDictionary<Server, Channel> logs = new ConcurrentDictionary<Server, Channel>();
        private readonly ConcurrentDictionary<Server, Channel> loggingPresences = new ConcurrentDictionary<Server, Channel>();
        private readonly ConcurrentDictionary<Channel, Channel> voiceChannelLog = new ConcurrentDictionary<Channel, Channel>();

        private string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";

        public LogCommand(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += MsgRecivd;
            NadekoBot.Client.MessageDeleted += MsgDltd;
            NadekoBot.Client.MessageUpdated += MsgUpdtd;
            NadekoBot.Client.UserUpdated += UsrUpdtd;
            NadekoBot.Client.UserBanned += UsrBanned;
            NadekoBot.Client.UserLeft += UsrLeft;
            NadekoBot.Client.UserJoined += UsrJoined;
            NadekoBot.Client.UserUnbanned += UsrUnbanned;
            NadekoBot.Client.ChannelCreated += ChannelCreated;
            NadekoBot.Client.ChannelDestroyed += ChannelDestroyed;
            NadekoBot.Client.ChannelUpdated += ChannelUpdated;


            NadekoBot.Client.MessageReceived += async (s, e) =>
            {
                if (e.Channel.IsPrivate || e.User.Id == NadekoBot.Client.CurrentUser.Id)
                    return;
                if (!SpecificConfigurations.Default.Of(e.Server.Id).SendPrivateMessageOnMention) return;
                try
                {
                    var usr = e.Message.MentionedUsers.FirstOrDefault(u => u != e.User);
                    if (usr?.Status != UserStatus.Offline)
                        return;
                    await e.Channel.SendMessage($"User `{usr.Name}` is offline. PM sent.").ConfigureAwait(false);
                    await usr.SendMessage(
                        $"User `{e.User.Name}` mentioned you on " +
                        $"`{e.Server.Name}` server while you were offline.\n" +
                        $"`Message:` {e.Message.Text}").ConfigureAwait(false);
                }
                catch { }
            };
        }

        private async void ChannelUpdated(object sender, ChannelUpdatedEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                if (e.Before.Name != e.After.Name)
                    await ch.SendMessage($@"`{prettyCurrentTime}` **Channel Name Changed** `#{e.Before.Name}` (*{e.After.Id}*)
        `New:` {e.After.Name}").ConfigureAwait(false);
                else if (e.Before.Topic != e.After.Topic)
                    await ch.SendMessage($@"`{prettyCurrentTime}` **Channel Topic Changed** `#{e.After.Name}` (*{e.After.Id}*)
        `Old:` {e.Before.Topic}
        `New:` {e.After.Topic}").ConfigureAwait(false);
            }
            catch { }
        }

        private async void ChannelDestroyed(object sender, ChannelEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"❗`{prettyCurrentTime}`❗`Channel Deleted:` #{e.Channel.Name} (*{e.Channel.Id}*)").ConfigureAwait(false);
            }
            catch { }
        }

        private async void ChannelCreated(object sender, ChannelEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"`{prettyCurrentTime}`🆕`Channel Created:` #{e.Channel.Mention} (*{e.Channel.Id}*)").ConfigureAwait(false);
            }
            catch { }
        }

        private async void UsrUnbanned(object sender, UserEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"`{prettyCurrentTime}`♻`User was unbanned:` **{e.User.Name}** ({e.User.Id})").ConfigureAwait(false);
            }
            catch { }
        }

        private async void UsrJoined(object sender, UserEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"`{prettyCurrentTime}`✅`User joined:` **{e.User.Name}** ({e.User.Id})").ConfigureAwait(false);
            }
            catch { }
        }

        private async void UsrLeft(object sender, UserEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"`{prettyCurrentTime}`❗`User left:` **{e.User.Name}** ({e.User.Id})").ConfigureAwait(false);
            }
            catch { }
        }

        private async void UsrBanned(object sender, UserEventArgs e)
        {
            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                await ch.SendMessage($"❗`{prettyCurrentTime}`❌`User banned:` **{e.User.Name}** ({e.User.Id})").ConfigureAwait(false);
            }
            catch { }
        }

        public Func<CommandEventArgs, Task> DoFunc() => async e =>
        {
            Channel ch;
            if (!logs.TryRemove(e.Server, out ch))
            {
                logs.TryAdd(e.Server, e.Channel);
                await e.Channel.SendMessage($"❗**I WILL BEGIN LOGGING SERVER ACTIVITY IN THIS CHANNEL**❗").ConfigureAwait(false);
                return;
            }

            await e.Channel.SendMessage($"❗**NO LONGER LOGGING IN {ch.Mention} CHANNEL**❗").ConfigureAwait(false);
        };

        private async void MsgRecivd(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Server == null || e.Channel.IsPrivate || e.User.Id == NadekoBot.Client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage(
$@"🕔`{prettyCurrentTime}` **New Message** `#{e.Channel.Name}`
👤`{e.User?.ToString() ?? ("NULL")}` {e.Message.Text}").ConfigureAwait(false);
            }
            catch { }
        }
        private async void MsgDltd(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Server == null || e.Channel.IsPrivate || e.User?.Id == NadekoBot.Client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage(
$@"🕔`{prettyCurrentTime}` **Message** 🚮 `#{e.Channel.Name}`
👤`{e.User?.ToString() ?? ("NULL")}` {e.Message.Text}").ConfigureAwait(false);
            }
            catch { }
        }
        private async void MsgUpdtd(object sender, MessageUpdatedEventArgs e)
        {
            try
            {
                if (e.Server == null || e.Channel.IsPrivate || e.User?.Id == NadekoBot.Client.CurrentUser.Id)
                    return;
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch) || e.Channel == ch)
                    return;
                await ch.SendMessage(
$@"🕔`{prettyCurrentTime}` **Message** 📝 `#{e.Channel.Name}`
👤`{e.User?.ToString() ?? ("NULL")}`
        `Old:` {e.Before.Text}
        `New:` {e.After.Text}").ConfigureAwait(false);
            }
            catch { }
        }
        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e)
        {
            try
            {
                Channel ch;
                if (loggingPresences.TryGetValue(e.Server, out ch))
                    if (e.Before.Status != e.After.Status)
                    {
                        await ch.SendMessage($"`{prettyCurrentTime}`**{e.Before.Name}** is now **{e.After.Status}**.").ConfigureAwait(false);
                    }
            }
            catch { }

            try
            {
                Channel notifyChBefore = null;
                Channel notifyChAfter = null;
                var beforeVch = e.Before.VoiceChannel;
                var afterVch = e.After.VoiceChannel;
                var notifyLeave = false;
                var notifyJoin = false;
                if ((beforeVch != null || afterVch != null) && (beforeVch != afterVch)) // this means we need to notify for sure.
                {
                    if (beforeVch != null && voiceChannelLog.TryGetValue(beforeVch, out notifyChBefore))
                    {
                        notifyLeave = true;
                    }
                    if (afterVch != null && voiceChannelLog.TryGetValue(afterVch, out notifyChAfter))
                    {
                        notifyJoin = true;
                    }
                    if ((notifyLeave && notifyJoin) && (notifyChAfter == notifyChBefore))
                    {
                        await notifyChAfter.SendMessage($"🎼`{prettyCurrentTime}` {e.Before.Name} moved from **{beforeVch.Mention}** to **{afterVch.Mention}** voice channel.").ConfigureAwait(false);
                    }
                    else if (notifyJoin)
                    {
                        await notifyChAfter.SendMessage($"🎼`{prettyCurrentTime}` {e.Before.Name} has joined **{afterVch.Mention}** voice channel.").ConfigureAwait(false);
                    }
                    else if (notifyLeave)
                    {
                        await notifyChBefore.SendMessage($"🎼`{prettyCurrentTime}` {e.Before.Name} has left **{beforeVch.Mention}** voice channel.").ConfigureAwait(false);
                    }
                }
            }
            catch { }

            try
            {
                Channel ch;
                if (!logs.TryGetValue(e.Server, out ch))
                    return;
                string str = $"🕔`{prettyCurrentTime}`";
                if (e.Before.Name != e.After.Name)
                    str += $"**Name Changed**👤`{e.Before?.ToString()}`\n\t\t`New:`{e.After.ToString()}`";
                else if (e.Before.Nickname != e.After.Nickname)
                    str += $"**Nickname Changed**👤`{e.Before?.ToString()}`\n\t\t`Old:` {e.Before.Nickname}#{e.Before.Discriminator}\n\t\t`New:` {e.After.Nickname}#{e.After.Discriminator}";
                else if (e.Before.AvatarUrl != e.After.AvatarUrl)
                    str += $"**Avatar Changed**👤`{e.Before?.ToString()}`\n\t {await e.Before.AvatarUrl.ShortenUrl()} `=>` {await e.After.AvatarUrl.ShortenUrl()}";
                else if (!e.Before.Roles.SequenceEqual(e.After.Roles))
                {
                    if (e.Before.Roles.Count() < e.After.Roles.Count())
                    {
                        var diffRoles = e.After.Roles.Where(r => !e.Before.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
                        str += $"**User's Roles changed ⚔➕**👤`{e.Before?.ToString()}`\n\tNow has {string.Join(", ", diffRoles)} role.";
                    }
                    else if (e.Before.Roles.Count() > e.After.Roles.Count())
                    {
                        var diffRoles = e.Before.Roles.Where(r => !e.After.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
                        str += $"**User's Roles changed ⚔➖**👤`{e.Before?.ToString()}`\n\tNo longer has {string.Join(", ", diffRoles)} role.";
                    }
                    else
                    {
                        Console.WriteLine("SEQUENCE NOT EQUAL BUT NO DIFF ROLES - REPORT TO KWOTH on #NADEKOLOG server");
                        return;
                    }

                }
                else
                    return;
                await ch.SendMessage(str).ConfigureAwait(false);
            }
            catch { }
        }

        internal override void Init(CommandGroupBuilder cgb)
        {

            cgb.CreateCommand(Module.Prefix + "spmom")
                .Description("Toggles whether mentions of other offline users on your server will send a pm to them.")
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(async e =>
                {
                    var specificConfig = SpecificConfigurations.Default.Of(e.Server.Id);
                    specificConfig.SendPrivateMessageOnMention =
                        !specificConfig.SendPrivateMessageOnMention;
                    if (specificConfig.SendPrivateMessageOnMention)
                        await e.Channel.SendMessage(":ok: I will send private messages " +
                                                    "to mentioned offline users.").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage(":ok: I won't send private messages " +
                                                    "to mentioned offline users anymore.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "logserver")
                  .Description("Toggles logging in this channel. Logs every message sent/deleted/edited on the server. **Owner Only!**")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .AddCheck(SimpleCheckers.ManageServer())
                  .Do(DoFunc());

            cgb.CreateCommand(Module.Prefix + "userpresence")
                  .Description("Starts logging to this channel when someone from the server goes online/offline/idle. **Owner Only!**")
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .AddCheck(SimpleCheckers.ManageServer())
                  .Do(async e =>
                  {
                      Channel ch;
                      if (!loggingPresences.TryRemove(e.Server, out ch))
                      {
                          loggingPresences.TryAdd(e.Server, e.Channel);
                          await e.Channel.SendMessage($"**User presence notifications enabled.**").ConfigureAwait(false);
                          return;
                      }

                      await e.Channel.SendMessage($"**User presence notifications disabled.**").ConfigureAwait(false);
                  });

            cgb.CreateCommand(Module.Prefix + "voicepresence")
                  .Description("Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now. **Owner Only!**")
                  .Parameter("all", ParameterType.Optional)
                  .AddCheck(SimpleCheckers.OwnerOnly())
                  .AddCheck(SimpleCheckers.ManageServer())
                  .Do(async e =>
                  {

                      if (e.GetArg("all")?.ToLower() == "all")
                      {
                          foreach (var voiceChannel in e.Server.VoiceChannels)
                          {
                              voiceChannelLog.TryAdd(voiceChannel, e.Channel);
                          }
                          await e.Channel.SendMessage("Started logging user presence for **ALL** voice channels!").ConfigureAwait(false);
                          return;
                      }

                      if (e.User.VoiceChannel == null)
                      {
                          await e.Channel.SendMessage("💢 You are not in a voice channel right now. If you are, please rejoin it.").ConfigureAwait(false);
                          return;
                      }
                      Channel throwaway;
                      if (!voiceChannelLog.TryRemove(e.User.VoiceChannel, out throwaway))
                      {
                          voiceChannelLog.TryAdd(e.User.VoiceChannel, e.Channel);
                          await e.Channel.SendMessage($"`Logging user updates for` {e.User.VoiceChannel.Mention} `voice channel.`").ConfigureAwait(false);
                      }
                      else
                          await e.Channel.SendMessage($"`Stopped logging user updates for` {e.User.VoiceChannel.Mention} `voice channel.`").ConfigureAwait(false);
                  });
        }
    }
}
