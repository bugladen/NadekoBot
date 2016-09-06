//using Discord;
//using Discord.Commands;
//using NadekoBot.Classes;
//using NadekoBot.Extensions;
//using NadekoBot.Modules.Permissions.Classes;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

////todo DB
////todo Add flags for every event
//namespace NadekoBot.Modules.Administration
//{
//    public class LogCommand : DiscordCommand
//    {
//        private string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";

//        private ConcurrentBag<KeyValuePair<Channel, string>> voicePresenceUpdates = new ConcurrentBag<KeyValuePair<Channel, string>>();

//        public LogCommand(DiscordModule module) : base(module)
//        {
//            NadekoBot.Client.MessageReceived += MsgRecivd;
//            NadekoBot.Client.MessageDeleted += MsgDltd;
//            NadekoBot.Client.MessageUpdated += MsgUpdtd;
//            NadekoBot.Client.UserUpdated += UsrUpdtd;
//            NadekoBot.Client.UserBanned += UsrBanned;
//            NadekoBot.Client.UserLeft += UsrLeft;
//            NadekoBot.Client.UserJoined += UsrJoined;
//            NadekoBot.Client.UserUnbanned += UsrUnbanned;
//            NadekoBot.Client.ChannelCreated += ChannelCreated;
//            NadekoBot.Client.ChannelDestroyed += ChannelDestroyed;
//            NadekoBot.Client.ChannelUpdated += ChannelUpdated;


//            NadekoBot.Client.MessageReceived += async (s, e) =>
//            {
//                if (e.Channel.IsPrivate || umsg.Author.Id == NadekoBot.Client.CurrentUser.Id)
//                    return;
//                if (!SpecificConfigurations.Default.Of(e.Server.Id).SendPrivateMessageOnMention) return;
//                try
//                {
//                    var usr = e.Message.MentionedUsers.FirstOrDefault(u => u != umsg.Author);
//                    if (usr?.Status != UserStatus.Offline)
//                        return;
//                    await channel.SendMessageAsync($"User `{usr.Name}` is offline. PM sent.").ConfigureAwait(false);
//                    await usr.SendMessageAsync(
//                        $"User `{umsg.Author.Username}` mentioned you on " +
//                        $"`{e.Server.Name}` server while you were offline.\n" +
//                        $"`Message:` {e.Message.Text}").ConfigureAwait(false);
//                }
//                catch { }
//            };

//            // start the userpresence queue

//            NadekoBot.OnReady += () => Task.Run(async () =>
//             {
//                 while (true)
//                 {
//                     var toSend = new Dictionary<Channel, string>();
//                     //take everything from the queue and merge the messages which are going to the same channel
//                     KeyValuePair<Channel, string> item;
//                     while (voicePresenceUpdates.TryTake(out item))
//                     {
//                         if (toSend.ContainsKey(item.Key))
//                         {
//                             toSend[item.Key] = toSend[item.Key] + Environment.NewLine + item.Value;
//                         }
//                         else
//                         {
//                             toSend.Add(item.Key, item.Value);
//                         }
//                     }
//                     //send merged messages to each channel
//                     foreach (var k in toSend)
//                     {
//                         try { await k.Key.SendMessageAsync(Environment.NewLine + k.Value).ConfigureAwait(false); } catch { }
//                     }

//                     await Task.Delay(5000);
//                 }
//             });
//        }

//        private async void ChannelUpdated(object sender, ChannelUpdatedEventArgs e)
//        {
//            try
//            {
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || config.LogserverIgnoreChannels.Contains(e.After.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                if (e.Before.Name != e.After.Name)
//                    await ch.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Name Changed** `#{e.Before.Name}` (*{e.After.Id}*)
//        `New:` {e.After.Name}").ConfigureAwait(false);
//                else if (e.Before.Topic != e.After.Topic)
//                    await ch.SendMessageAsync($@"`{prettyCurrentTime}` **Channel Topic Changed** `#{e.After.Name}` (*{e.After.Id}*)
//        `Old:` {e.Before.Topic}
//        `New:` {e.After.Topic}").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void ChannelDestroyed(object sender, ChannelEventArgs e)
//        {
//            try
//            {
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || config.LogserverIgnoreChannels.Contains(e.Channel.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"❗`{prettyCurrentTime}`❗`Channel Deleted:` #{e.Channel.Name} (*{e.Channel.Id}*)").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void ChannelCreated(object sender, ChannelEventArgs e)
//        {
//            try
//            {
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || config.LogserverIgnoreChannels.Contains(e.Channel.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"`{prettyCurrentTime}`🆕`Channel Created:` #{e.Channel.Mention} (*{e.Channel.Id}*)").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void UsrUnbanned(object sender, UserEventArgs e)
//        {
//            try
//            {
//                var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                if (chId == null)
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"`{prettyCurrentTime}`♻`User was unbanned:` **{umsg.Author.Username}** ({umsg.Author.Id})").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void UsrJoined(object sender, UserEventArgs e)
//        {
//            try
//            {
//                var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                if (chId == null)
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"`{prettyCurrentTime}`✅`User joined:` **{umsg.Author.Username}** ({umsg.Author.Id})").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void UsrLeft(object sender, UserEventArgs e)
//        {
//            try
//            {
//                var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                if (chId == null)
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"`{prettyCurrentTime}`❗`User left:` **{umsg.Author.Username}** ({umsg.Author.Id})").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void UsrBanned(object sender, UserEventArgs e)
//        {
//            try
//            {
//                var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                if (chId == null)
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync($"❗`{prettyCurrentTime}`❌`User banned:` **{umsg.Author.Username}** ({umsg.Author.Id})").ConfigureAwait(false);
//            }
//            catch { }
//        }

//        private async void MsgRecivd(object sender, MessageEventArgs e)
//        {
//            try
//            {
//                if (e.Server == null || e.Channel.IsPrivate || umsg.Author.Id == NadekoBot.Client.CurrentUser.Id)
//                    return;
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || e.Channel.Id == chId || config.LogserverIgnoreChannels.Contains(e.Channel.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                if (!string.IsNullOrWhiteSpace(e.Message.Text))
//                {
//                    await ch.SendMessageAsync(
//        $@"🕔`{prettyCurrentTime}` **New Message** `#{e.Channel.Name}`
//👤`{umsg.Author?.ToString() ?? ("NULL")}` {e.Message.Text.Unmention()}").ConfigureAwait(false);
//                }
//                else
//                {
//                    await ch.SendMessageAsync(
//        $@"🕔`{prettyCurrentTime}` **File Uploaded** `#{e.Channel.Name}`
//👤`{umsg.Author?.ToString() ?? ("NULL")}` {e.Message.Attachments.FirstOrDefault()?.ProxyUrl}").ConfigureAwait(false);
//                }

//            }
//            catch { }
//        }
//        private async void MsgDltd(object sender, MessageEventArgs e)
//        {
//            try
//            {
//                if (e.Server == null || e.Channel.IsPrivate || umsg.Author?.Id == NadekoBot.Client.CurrentUser.Id)
//                    return;
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || e.Channel.Id == chId || config.LogserverIgnoreChannels.Contains(e.Channel.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                if (!string.IsNullOrWhiteSpace(e.Message.Text))
//                {
//                    await ch.SendMessageAsync(
//        $@"🕔`{prettyCurrentTime}` **Message** 🚮 `#{e.Channel.Name}`
//👤`{umsg.Author?.ToString() ?? ("NULL")}` {e.Message.Text.Unmention()}").ConfigureAwait(false);
//                }
//                else
//                {
//                    await ch.SendMessageAsync(
//        $@"🕔`{prettyCurrentTime}` **File Deleted** `#{e.Channel.Name}`
//👤`{umsg.Author?.ToString() ?? ("NULL")}` {e.Message.Attachments.FirstOrDefault()?.ProxyUrl}").ConfigureAwait(false);
//                }
//            }
//            catch { }
//        }
//        private async void MsgUpdtd(object sender, MessageUpdatedEventArgs e)
//        {
//            try
//            {
//                if (e.Server == null || e.Channel.IsPrivate || umsg.Author?.Id == NadekoBot.Client.CurrentUser.Id)
//                    return;
//                var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                var chId = config.LogServerChannel;
//                if (chId == null || e.Channel.Id == chId || config.LogserverIgnoreChannels.Contains(e.Channel.Id))
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                await ch.SendMessageAsync(
//        $@"🕔`{prettyCurrentTime}` **Message** 📝 `#{e.Channel.Name}`
//👤`{umsg.Author?.ToString() ?? ("NULL")}`
//        `Old:` {e.Before.Text.Unmention()}
//        `New:` {e.After.Text.Unmention()}").ConfigureAwait(false);
//            }
//            catch { }
//        }
//        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e)
//        {
//            var config = SpecificConfigurations.Default.Of(e.Server.Id);
//            try
//            {
//                var chId = config.LogPresenceChannel;
//                if (chId != null)
//                {
//                    Channel ch;
//                    if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) != null)
//                    {
//                        if (e.Before.Status != e.After.Status)
//                        {
//                            voicePresenceUpdates.Add(new KeyValuePair<Channel, string>(ch, $"`{prettyCurrentTime}`**{e.Before.Name}** is now **{e.After.Status}**."));
//                        }
//                    }
//                }
//            }
//            catch { }

//            try
//            {
//                ulong notifyChBeforeId;
//                ulong notifyChAfterId;
//                Channel notifyChBefore = null;
//                Channel notifyChAfter = null;
//                var beforeVch = e.Before.VoiceChannel;
//                var afterVch = e.After.VoiceChannel;
//                var notifyLeave = false;
//                var notifyJoin = false;
//                if ((beforeVch != null || afterVch != null) && (beforeVch != afterVch)) // this means we need to notify for sure.
//                {
//                    if (beforeVch != null && config.VoiceChannelLog.TryGetValue(beforeVch.Id, out notifyChBeforeId) && (notifyChBefore = e.Before.Server.TextChannels.FirstOrDefault(tc => tc.Id == notifyChBeforeId)) != null)
//                    {
//                        notifyLeave = true;
//                    }
//                    if (afterVch != null && config.VoiceChannelLog.TryGetValue(afterVch.Id, out notifyChAfterId) && (notifyChAfter = e.After.Server.TextChannels.FirstOrDefault(tc => tc.Id == notifyChAfterId)) != null)
//                    {
//                        notifyJoin = true;
//                    }
//                    if ((notifyLeave && notifyJoin) && (notifyChAfter == notifyChBefore))
//                    {
//                        await notifyChAfter.SendMessageAsync($"🎼`{prettyCurrentTime}` {e.Before.Name} moved from **{beforeVch.Mention}** to **{afterVch.Mention}** voice channel.").ConfigureAwait(false);
//                    }
//                    else if (notifyJoin)
//                    {
//                        await notifyChAfter.SendMessageAsync($"🎼`{prettyCurrentTime}` {e.Before.Name} has joined **{afterVch.Mention}** voice channel.").ConfigureAwait(false);
//                    }
//                    else if (notifyLeave)
//                    {
//                        await notifyChBefore.SendMessageAsync($"🎼`{prettyCurrentTime}` {e.Before.Name} has left **{beforeVch.Mention}** voice channel.").ConfigureAwait(false);
//                    }
//                }
//            }
//            catch { }

//            try
//            {
//                var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                if (chId == null)
//                    return;
//                Channel ch;
//                if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                    return;
//                string str = $"🕔`{prettyCurrentTime}`";
//                if (e.Before.Name != e.After.Name)
//                    str += $"**Name Changed**👤`{e.Before?.ToString()}`\n\t\t`New:`{e.After.ToString()}`";
//                else if (e.Before.Nickname != e.After.Nickname)
//                    str += $"**Nickname Changed**👤`{e.Before?.ToString()}`\n\t\t`Old:` {e.Before.Nickname}#{e.Before.Discriminator}\n\t\t`New:` {e.After.Nickname}#{e.After.Discriminator}";
//                else if (e.Before.AvatarUrl != e.After.AvatarUrl)
//                    str += $"**Avatar Changed**👤`{e.Before?.ToString()}`\n\t {await e.Before.AvatarUrl.ShortenUrl()} `=>` {await e.After.AvatarUrl.ShortenUrl()}";
//                else if (!e.Before.Roles.SequenceEqual(e.After.Roles))
//                {
//                    if (e.Before.Roles.Count() < e.After.Roles.Count())
//                    {
//                        var diffRoles = e.After.Roles.Where(r => !e.Before.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
//                        str += $"**User's Roles changed ⚔➕**👤`{e.Before?.ToString()}`\n\tNow has {string.Join(", ", diffRoles)} role.";
//                    }
//                    else if (e.Before.Roles.Count() > e.After.Roles.Count())
//                    {
//                        var diffRoles = e.Before.Roles.Where(r => !e.After.Roles.Contains(r)).Select(r => "`" + r.Name + "`");
//                        str += $"**User's Roles changed ⚔➖**👤`{e.Before?.ToString()}`\n\tNo longer has {string.Join(", ", diffRoles)} role.";
//                    }
//                    else
//                    {
//                        Console.WriteLine("SEQUENCE NOT EQUAL BUT NO DIFF ROLES - REPORT TO KWOTH on #NADEKOLOG server");
//                        return;
//                    }

//                }
//                else
//                    return;
//                await ch.SendMessageAsync(str).ConfigureAwait(false);
//            }
//            catch { }
//        }

//        public override void Init(CommandGroupBuilder cgb)
//        {

//            cgb.CreateCommand(Module.Prefix + "spmom")
//                .Description($"Toggles whether mentions of other offline users on your server will send a pm to them. **Needs Manage Server Permissions.**| `{Prefix}spmom`")
//                .AddCheck(SimpleCheckers.ManageServer())
//                .Do(async e =>
//                {
//                    var specificConfig = SpecificConfigurations.Default.Of(e.Server.Id);
//                    specificConfig.SendPrivateMessageOnMention =
//                        !specificConfig.SendPrivateMessageOnMention;
//                    if (specificConfig.SendPrivateMessageOnMention)
//                        await channel.SendMessageAsync(":ok: I will send private messages " +
//                                                    "to mentioned offline users.").ConfigureAwait(false);
//                    else
//                        await channel.SendMessageAsync(":ok: I won't send private messages " +
//                                                    "to mentioned offline users anymore.").ConfigureAwait(false);
//                });

//            cgb.CreateCommand(Module.Prefix + "logserver")
//                  .Description($"Toggles logging in this channel. Logs every message sent/deleted/edited on the server. **Bot Owner Only!** | `{Prefix}logserver`")
//                  .AddCheck(SimpleCheckers.OwnerOnly())
//                  .AddCheck(SimpleCheckers.ManageServer())
//                  .Do(async e =>
//                  {
//                      var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel;
//                      if (chId == null)
//                      {
//                          SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel = e.Channel.Id;
//                          await channel.SendMessageAsync($"❗**I WILL BEGIN LOGGING SERVER ACTIVITY IN THIS CHANNEL**❗").ConfigureAwait(false);
//                          return;
//                      }
//                      Channel ch;
//                      if ((ch = e.Server.TextChannels.Where(tc => tc.Id == chId).FirstOrDefault()) == null)
//                          return;

//                      SpecificConfigurations.Default.Of(e.Server.Id).LogServerChannel = null;
//                      await channel.SendMessageAsync($"❗**NO LONGER LOGGING IN {ch.Mention} CHANNEL**❗").ConfigureAwait(false);
//                  });


//            cgb.CreateCommand(Prefix + "logignore")
//                .Description($"Toggles whether the {Prefix}logserver command ignores this channel. Useful if you have hidden admin channel and public log channel. **Bot Owner Only!**| `{Prefix}logignore`")
//                .AddCheck(SimpleCheckers.OwnerOnly())
//                .AddCheck(SimpleCheckers.ManageServer())
//                .Do(async e =>
//                {
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    if (config.LogserverIgnoreChannels.Remove(e.Channel.Id))
//                    {
//                        await channel.SendMessageAsync($"`{Prefix}logserver will stop ignoring this channel.`");
//                    }
//                    else
//                    {
//                        config.LogserverIgnoreChannels.Add(e.Channel.Id);
//                        await channel.SendMessageAsync($"`{Prefix}logserver will ignore this channel.`");
//                    }
//                });

//            cgb.CreateCommand(Module.Prefix + "userpresence")
//                  .Description($"Starts logging to this channel when someone from the server goes online/offline/idle. **Needs Manage Server Permissions.**| `{Prefix}userpresence`")
//                  .AddCheck(SimpleCheckers.ManageServer())
//                  .Do(async e =>
//                  {
//                      var chId = SpecificConfigurations.Default.Of(e.Server.Id).LogPresenceChannel;
//                      if (chId == null)
//                      {
//                          SpecificConfigurations.Default.Of(e.Server.Id).LogPresenceChannel = e.Channel.Id;
//                          await channel.SendMessageAsync($"**User presence notifications enabled.**").ConfigureAwait(false);
//                          return;
//                      }
//                      SpecificConfigurations.Default.Of(e.Server.Id).LogPresenceChannel = null;
//                      await channel.SendMessageAsync($"**User presence notifications disabled.**").ConfigureAwait(false);
//                  });

//            cgb.CreateCommand(Module.Prefix + "voicepresence")
//                  .Description($"Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now. **Needs Manage Server Permissions.**| `{Prefix}voicerpresence`")
//                  .Parameter("all", ParameterType.Optional)
//                  .AddCheck(SimpleCheckers.ManageServer())
//                  .Do(async e =>
//                  {

//                      var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                      if (all?.ToLower() == "all")
//                      {
//                          foreach (var voiceChannel in e.Server.VoiceChannels)
//                          {
//                              config.VoiceChannelLog.TryAdd(voiceChannel.Id, e.Channel.Id);
//                          }
//                          await channel.SendMessageAsync("Started logging user presence for **ALL** voice channels!").ConfigureAwait(false);
//                          return;
//                      }

//                      if (umsg.Author.VoiceChannel == null)
//                      {
//                          await channel.SendMessageAsync("💢 You are not in a voice channel right now. If you are, please rejoin it.").ConfigureAwait(false);
//                          return;
//                      }
//                      ulong throwaway;
//                      if (!config.VoiceChannelLog.TryRemove(umsg.Author.VoiceChannel.Id, out throwaway))
//                      {
//                          config.VoiceChannelLog.TryAdd(umsg.Author.VoiceChannel.Id, e.Channel.Id);
//                          await channel.SendMessageAsync($"`Logging user updates for` {umsg.Author.VoiceChannel.Mention} `voice channel.`").ConfigureAwait(false);
//                      }
//                      else
//                          await channel.SendMessageAsync($"`Stopped logging user updates for` {umsg.Author.VoiceChannel.Mention} `voice channel.`").ConfigureAwait(false);
//                  });
//        }
//    }
//}