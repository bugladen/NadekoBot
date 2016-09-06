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

//            // start the userpresence queue


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

//        private async void UsrUpdtd(object sender, UserUpdatedEventArgs e)
//        {
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