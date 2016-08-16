using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Commands;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;
using NadekoBot.Attributes;

//todo fix delmsgoncmd
//todo DB
namespace NadekoBot.Modules.Administration
{
    [Module(".", AppendSpace = false)]
    public partial class AdministrationModule : DiscordModule
    {
        public AdministrationModule(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        //todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Restart(IMessage imsg)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    await imsg.Channel.SendMessageAsync("`Restarting in 2 seconds...`");
        //    await Task.Delay(2000);
        //    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
        //    Environment.Exit(0);
        //}

        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //[RequirePermission(GuildPermission.ManageGuild)]
        //public async Task Delmsgoncmd(IMessage imsg)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    var conf = SpecificConfigurations.Default.Of(channel.Guild.Id);
        //    conf.AutoDeleteMessagesOnCommand = !conf.AutoDeleteMessagesOnCommand;
        //    await Classes.JSONModels.ConfigHandler.SaveConfig().ConfigureAwait(false);
        //    if (conf.AutoDeleteMessagesOnCommand)
        //        await imsg.Channel.SendMessageAsync("❗`Now automatically deleting successfull command invokations.`");
        //    else
        //        await imsg.Channel.SendMessageAsync("❗`Stopped automatic deletion of successfull command invokations.`");
        //}

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task Setrole(IMessage imsg, IUser userName, [Remainder] string roleName)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (string.IsNullOrWhiteSpace(roleName)) return;

            var usr = channel.Guild.FindUsers(userName).FirstOrDefault();
            if (usr == null)
            {
                await imsg.Channel.SendMessageAsync("You failed to supply a valid username").ConfigureAwait(false);
                return;
            }

            var role = channel.Guild.FindRoles(roleName).FirstOrDefault();
            if (role == null)
            {
                await imsg.Channel.SendMessageAsync("You failed to supply a valid role").ConfigureAwait(false);
                return;
            }

            try
            {
                await usr.AddRoles(role).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully added role **{role.Name}** to user **{usr.Name}**").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await imsg.Channel.SendMessageAsync("Failed to add roles. Bot has insufficient permissions.\n").ConfigureAwait(false);
                Console.WriteLine(ex.ToString());
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task Removerole(IMessage imsg, IUser userName, [Remainder] string roleName)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (string.IsNullOrWhiteSpace(roleName)) return;

            var usr = channel.Guild.FindUsers(userName).FirstOrDefault();
            if (usr == null)
            {
                await imsg.Channel.SendMessageAsync("You failed to supply a valid username").ConfigureAwait(false);
                return;
            }

            var role = channel.Guild.FindRoles(roleName).FirstOrDefault();
            if (role == null)
            {
                await imsg.Channel.SendMessageAsync("You failed to supply a valid role").ConfigureAwait(false);
                return;
            }

            try
            {
                await usr.RemoveRoles(role).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully removed role **{role.Name}** from user **{usr.Name}**").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RenameRole(IMessage imsg, string r1, string r2)
        {
            var channel = imsg.Channel as IGuildChannel;

            var roleToEdit = channel.Guild.FindRoles(r1).FirstOrDefault();
            if (roleToEdit == null)
            {
                await imsg.Channel.SendMessageAsync("Can't find that role.").ConfigureAwait(false);
                return;
            }

            try
            {
                if (roleToEdit.Position > channel.Guild.CurrentUser.Roles.Max(r => r.Position))
                {
                    await imsg.Channel.SendMessageAsync("I can't edit roles higher than my highest role.").ConfigureAwait(false);
                    return;
                }
                await roleToEdit.Edit(r2);
                await imsg.Channel.SendMessageAsync("Role renamed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await imsg.Channel.SendMessageAsync("Failed to rename role. Probably insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RemoveAllRoles(IMessage imsg, [Remainder] string userName)
        {
            var channel = imsg.Channel as IGuildChannel;

            var usr = channel.Guild.FindUsers(userName).FirstOrDefault();
            if (usr == null)
            {
                await imsg.Channel.SendMessageAsync("You failed to supply a valid username").ConfigureAwait(false);
                return;
            }

            try
            {
                await usr.RemoveRoles(usr.Roles.ToArray()).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully removed **all** roles from user **{usr.Name}**").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task CreateRole(IMessage imsg, [Remainder] string roleName)
        {
            var channel = imsg.Channel as IGuildChannel;


            if (string.IsNullOrWhiteSpace(e.GetArg("role_name")))
                return;
            try
            {
                var r = await channel.Guild.CreateRole(e.GetArg("role_name")).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully created role **{r.Name}**.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await imsg.Channel.SendMessageAsync(":warning: Unspecified error.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RoleColor(IMessage imsg, string roleName, string r="", string g="", string b="")
        {
            var channel = imsg.Channel as IGuildChannel;

            var args = e.Args.Where(s => s != string.Empty);

            if (args.Count() != 2 && args.Count() != 4)
            {
                await imsg.Channel.SendMessageAsync("The parameters are invalid.").ConfigureAwait(false);
                return;
            }

            var role = channel.Guild.FindRoles(e.Args[0]).FirstOrDefault();

            if (role == null)
            {
                await imsg.Channel.SendMessageAsync("That role does not exist.").ConfigureAwait(false);
                return;
            }
            try
            {
                var rgb = args.Count() == 4;
                var arg1 = e.Args[1].Replace("#", "");

                var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                var green = Convert.ToByte(rgb ? int.Parse(e.Args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                var blue = Convert.ToByte(rgb ? int.Parse(e.Args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));

                await role.Edit(color: new Color(red, green, blue)).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Role {role.Name}'s color has been changed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await imsg.Channel.SendMessageAsync("Error occured, most likely invalid parameters or insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.BanMembers)]
        public async Task Ban(IMessage imsg, IUser user, [Remainder] string msg)
        {
            var channel = imsg.Channel as IGuildChannel;
            if (user == null)
            {
                await imsg.Channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await user.SendMessage($"**You have been BANNED from `{channel.Guild.Name}` server.**\n" +
                                        $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await channel.Guild.Ban(user, 7).ConfigureAwait(false);

                await imsg.Channel.SendMessageAsync("Banned user " + user.Name + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.BanMembers)]
        public async Task Softban(IMessage imsg, IUser user, [Remainder] string msg)
        {
            var channel = imsg.Channel as IGuildChannel;
            
            if (user == null)
            {
                await imsg.Channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await user.SendMessage($"**You have been SOFT-BANNED from `{channel.Guild.Name}` server.**\n" +
                                        $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await channel.Guild.Ban(user, 7).ConfigureAwait(false);
                await channel.Guild.Unban(user).ConfigureAwait(false);

                await imsg.Channel.SendMessageAsync("Soft-Banned user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Kick(IMessage imsg, IUser user, [Remainder] string msg)
        {
            var channel = imsg.Channel as IGuildChannel;

            var usr = channel.Guild.FindUsers(user).FirstOrDefault();
            if (usr == null)
            {
                await imsg.Channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await usr.SendMessage($"**You have been KICKED from `{channel.Guild.Name}` server.**\n" +
                                      $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await usr.Kick().ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync("Kicked user " + usr.Name + " Id: " + usr.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task Mute(IMessage imsg, [Remainder] string throwaway)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (!e.Message.MentionedUsers.Any())
                return;
            try
            {
                foreach (var u in e.Message.MentionedUsers)
                {
                    await u.Edit(isMuted: true).ConfigureAwait(false);
                }
                await imsg.Channel.SendMessageAsync("Mute successful").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Unmute(IMessage imsg, [Remainder] string throwaway)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (!e.Message.MentionedUsers.Any())
                return;
            try
            {
                foreach (var u in e.Message.MentionedUsers)
                {
                    await u.Edit(isMuted: false).ConfigureAwait(false);
                }
                await imsg.Channel.SendMessageAsync("Unmute successful").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.DeafenMembers)]
        public async Task Deafen(IMessage imsg, [Remainder] string throwaway)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (!e.Message.MentionedUsers.Any())
                return;
            try
            {
                foreach (var u in e.Message.MentionedUsers)
                {
                    await u.Edit(isDeafened: true).ConfigureAwait(false);
                }
                await imsg.Channel.SendMessageAsync("Deafen successful").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }

        }
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.DeafenMembers)]
        public async Task UnDeafen(IMessage imsg, [Remainder] string throwaway)
        {
            var channel = imsg.Channel as IGuildChannel;

            if (!e.Message.MentionedUsers.Any())
                return;
            try
            {
                foreach (var u in e.Message.MentionedUsers)
                {
                    await u.Edit(isDeafened: false).ConfigureAwait(false);
                }
                await imsg.Channel.SendMessageAsync("Undeafen successful").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task DelVoiChanl(IMessage imsg, [Remainder] channelName)
        {
            var channel = imsg.Channel as IGuildChannel;
            var ch = channel.Guild.FindChannels(channelName, ChannelType.Voice).FirstOrDefault();
            if (ch == null)
                return;
            await ch.Delete().ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Removed channel **{channelName}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task CreatVoiChanl(IMessage imsg, [Remainder] string channelName)
        {
            var channel = imsg.Channel as IGuildChannel;

            await channel.Guild.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Created voice channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task DelTxtChanl(IMessage imsg, [Remainder] string channelName)
        {
            var channel = imsg.Channel as IGuildChannel;

            var channel = channel.Guild.FindChannels(e.GetArg("channel_name"), ChannelType.Text).FirstOrDefault();
            if (channel == null) return;
            await channel.Delete().ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Removed text channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task CreaTxtChanl(IMessage imsg, [Remainder] string arg)
        {
            var channel = imsg.Channel as IGuildChannel;
            await channel.Guild.CreateChannel(e.GetArg("channel_name"), ChannelType.Text).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Added text channel **{e.GetArg("channel_name")}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetTopic(IMessage imsg, [Remainder] string arg)
        {
            var topic = e.GetArg("topic")?.Trim() ?? "";
            await e.Channel.Edit(topic: topic).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync(":ok: **New channel topic set.**").ConfigureAwait(false);

        }
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetChanlName(IMessage imsg, [Remainder] string arg)
        {
            var channel = imsg.Channel as IGuildChannel;

            var name = e.GetArg("name");
            if (string.IsNullOrWhiteSpace(name))
                return;
            await e.Channel.Edit(name: name).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync(":ok: **New channel name set.**").ConfigureAwait(false);
        }


        //todo maybe split into 3/4 different commands with the same name
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Prune(IMessage msg, [Remainder] string target = null)
        {
            var channel = msg.Channel as IGuildChannel;

            var user = await channel.Guild.GetCurrentUserAsync();
            if (string.IsNullOrWhiteSpace(target))
            {

                var enumerable = (await msg.Channel.GetMessagesAsync()).Where(x => x.Author.Id == user.Id);
                await msg.Channel.DeleteMessagesAsync(enumerable);
                return;
            }
            target = target.Trim();
            if (!user.GetPermissions(channel).ManageMessages)
            {
                await msg.Reply("Don't have permissions to manage messages in channel");
                return;
            }
            int count;
            if (int.TryParse(target, out count))
            {
                while (count > 0)
                {
                    int limit = (count < 100) ? count : 100;
                    var enumerable = (await msg.Channel.GetMessagesAsync(limit: limit));
                    await msg.Channel.DeleteMessagesAsync(enumerable);
                    await Task.Delay(1000); // there is a 1 per second per guild ratelimit for deletemessages
                    if (enumerable.Count < limit) break;
                    count -= limit;
                }
            }
            else if (msg.MentionedUsers.Count > 0)
            {
                var toDel = new List<IMessage>();

                var match = Regex.Match(target, @"\s(\d+)\s");
                if (match.Success)
                {
                    int.TryParse(match.Groups[1].Value, out count);
                    var messages = new List<IMessage>(count);

                    while (count > 0)
                    {
                        var toAdd = await msg.Channel.GetMessagesAsync(limit: count < 100 ? count : 100);
                        messages.AddRange(toAdd);
                        count -= toAdd.Count;
                    }

                    foreach (var mention in msg.MentionedUsers)
                    {
                        toDel.AddRange(messages.Where(m => m.Author.Id == mention.Id));
                    }

                    var messagesEnum = messages.AsEnumerable();
                    while (messagesEnum.Count() > 0)
                    {
                        await msg.Channel.DeleteMessagesAsync(messagesEnum.Take(100));
                        await Task.Delay(1000); // 1 second ratelimit
                        messagesEnum = messagesEnum.Skip(100);
                    }
                }
            }
        }

        //todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Die(IMessage imsg)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    await imsg.Channel.SendMessageAsync("`Shutting down.`").ConfigureAwait(false);
        //    await Task.Delay(2000).ConfigureAwait(false);
        //    Environment.Exit(0);
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Setname(IMessage imsg, [Remainder] string newName)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task NewAvatar(IMessage imsg, [Remainder] string img)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    if (string.IsNullOrWhiteSpace(e.GetArg("img")))
        //        return;
        //    // Gather user provided URL.
        //    var avatarAddress = e.GetArg("img");
        //    var imageStream = await SearchHelper.GetResponseStreamAsync(avatarAddress).ConfigureAwait(false);
        //    var image = System.Drawing.Image.FromStream(imageStream);
        //    await client.CurrentUser.Edit("", avatar: image.ToStream()).ConfigureAwait(false);

        //    // Send confirm.
        //    await imsg.Channel.SendMessageAsync("New avatar set.").ConfigureAwait(false);
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task SetGame(IMessage imsg, [Remainder] string game)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    game = game ?? "";

        //    client.SetGame(e.GetArg("set_game"));
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Send(IMessage imsg, string where, [Remainder] string msg)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    if (string.IsNullOrWhiteSpace(msg))
        //        return;

        //    var ids = where.Split('|');
        //    if (ids.Length != 2)
        //        return;
        //    var sid = ulong.Parse(ids[0]);
        //    var server = NadekoBot.Client.Servers.Where(s => s.Id == sid).FirstOrDefault();

        //    if (server == null)
        //        return;

        //    if (ids[1].ToUpperInvariant().StartsWith("C:"))
        //    {
        //        var cid = ulong.Parse(ids[1].Substring(2));
        //        var channel = server.TextChannels.Where(c => c.Id == cid).FirstOrDefault();
        //        if (channel == null)
        //        {
        //            return;
        //        }
        //        await channel.SendMessage(msg);
        //    }
        //    else if (ids[1].ToUpperInvariant().StartsWith("U:"))
        //    {
        //        var uid = ulong.Parse(ids[1].Substring(2));
        //        var user = server.Users.Where(u => u.Id == uid).FirstOrDefault();
        //        if (user == null)
        //        {
        //            return;
        //        }
        //        await user.SendMessage(msg);
        //    }
        //    else
        //    {
        //        await imsg.Channel.SendMessageAsync("`Invalid format.`");
        //    }
        //}

        ////todo owner only
        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Donadd(IMessage imsg, IUser donator, int amount)
        //{
        //    var channel = imsg.Channel as IGuildChannel;
        //    var donator = channel.Guild.FindUsers(e.GetArg("donator")).FirstOrDefault();
        //    var amount = int.Parse(e.GetArg("amount"));
        //    if (donator == null) return;
        //    try
        //    {
        //        DbHandler.Instance.Connection.Insert(new Donator
        //        {
        //            Amount = amount,
        //            UserName = donator.Name,
        //            UserId = (long)donator.Id
        //        });
        //        imsg.Channel.SendMessageAsync("Successfuly added a new donator. 👑").ConfigureAwait(false);
        //    }
        //    catch { }

        //}

        //todo owner only
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Announce(IMessage imsg, [Remainder] string message)
        {
            var channel = imsg.Channel as IGuildChannel;


            foreach (var ch in NadekoBot.Client.Servers.Select(s => s.DefaultChannel))
            {
                await ch.SendMessage(e.GetArg("msg")).ConfigureAwait(false);
            }

            await imsg.Channel.SendMessageAsync(":ok:").ConfigureAwait(false);
        }

        //todo owner only
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task SaveChat(IMessage imsg, int cnt)
        {
            var channel = imsg.Channel as IGuildChannel;

            ulong? lastmsgId = null;
            var sb = new StringBuilder();
            var msgs = new List<IMessage>(cnt);
            while (cnt > 0)
            {
                var dlcnt = cnt < 100 ? cnt : 100;

                var dledMsgs = await e.Channel.DownloadMessages(dlcnt, lastmsgId);
                if (!dledMsgs.Any())
                    break;
                msgs.AddRange(dledMsgs);
                lastmsgId = msgs[msgs.Count - 1].Id;
                cnt -= 100;
            }
            await e.User.SendFile($"Chatlog-{channel.Guild.Name}/#{e.Channel.Name}-{DateTime.Now}.txt", 
                JsonConvert.SerializeObject(new { Messages = msgs.Select(s => s.ToString()) }, Formatting.Indented).ToStream()).ConfigureAwait(false);
        }


        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MentionEveryone)]
        public async Task MentionRole(IMessage imsg, [Remainder] string roles)
        {
            var channel = imsg.Channel as IGuildChannel;
            
            var arg = e.GetArg("roles").Split(',').Select(r => r.Trim());
            string send = $"--{e.User.Mention} has invoked a mention on the following roles--";
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str)))
            {
                var role = channel.Guild.FindRoles(roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", role.Members.Select(r => r.Mention));
            }

            while (send.Length > 2000)
            {
                var curstr = send.Substring(0, 2000);
                await
                    e.Channel.Send(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await e.Channel.Send(send).ConfigureAwait(false);
        }

        //todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Donators(IMessage imsg)
        //{
        //    var channel = imsg.Channel as IGuildChannel;

        //    var rows = DbHandler.Instance.GetAllRows<Donator>();
        //    var donatorsOrdered = rows.OrderByDescending(d => d.Amount);
        //    string str = $"**Thanks to the people listed below for making this project happen!**\n";

        //    await imsg.Channel.SendMessageAsync(str + string.Join("⭐", donatorsOrdered.Select(d => d.UserName))).ConfigureAwait(false);
        //}
    }
}
