using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;
using NadekoBot.Attributes;
using System.Text.RegularExpressions;

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

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Restart(IMessage imsg)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    await imsg.Channel.SendMessageAsync("`Restarting in 2 seconds...`");
        //    await Task.Delay(2000);
        //    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
        //    Environment.Exit(0);
        //}

        ////todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //[RequirePermission(GuildPermission.ManageGuild)]
        //public async Task Delmsgoncmd(IMessage imsg)
        //{
        //    var channel = imsg.Channel as ITextChannel;

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
        public async Task Setrole(IMessage imsg, IGuildUser usr, [Remainder] IRole role)
        {
            var channel = imsg.Channel as ITextChannel;
            try
            {
                await usr.AddRolesAsync(role).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully added role **{role.Name}** to user **{usr.Username}**").ConfigureAwait(false);
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
        public async Task Removerole(IMessage imsg, IGuildUser usr, [Remainder] IRole role)
        {
            try
            {
                await usr.RemoveRolesAsync(role).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully removed role **{role.Name}** from user **{usr.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RenameRole(IMessage imsg, IRole roleToEdit, string newname)
        {
            var channel = imsg.Channel as ITextChannel;
            try
            {
                if (roleToEdit.Position > (await channel.Guild.GetCurrentUserAsync().ConfigureAwait(false)).Roles.Max(r => r.Position))
                {
                    await imsg.Channel.SendMessageAsync("You can't edit roles higher than your highest role.").ConfigureAwait(false);
                    return;
                }
                await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
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
        public async Task RemoveAllRoles(IMessage imsg, [Remainder] IGuildUser user)
        {
            var channel = imsg.Channel as ITextChannel;

            try
            {
                await user.RemoveRolesAsync(user.Roles).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync($"Successfully removed **all** roles from user **{user.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task CreateRole(IMessage imsg, [Remainder] string roleName = null)
        {
            var channel = imsg.Channel as ITextChannel;


            if (string.IsNullOrWhiteSpace(roleName))
                return;
            try
            {
                var r = await channel.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
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
        public async Task RoleColor(IMessage imsg, params string[] args)
        {
            var channel = imsg.Channel as ITextChannel;

            if (args.Count() != 2 && args.Count() != 4)
            {
                await imsg.Channel.SendMessageAsync("The parameters are invalid.").ConfigureAwait(false);
                return;
            }
            var roleName = args[0].ToUpperInvariant();
            var role = channel.Guild.Roles.Where(r=>r.Name.ToUpperInvariant() == roleName).FirstOrDefault();

            if (role == null)
            {
                await imsg.Channel.SendMessageAsync("That role does not exist.").ConfigureAwait(false);
                return;
            }
            try
            {
                var rgb = args.Count() == 4;
                var arg1 = args[1].Replace("#", "");

                var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                var green = Convert.ToByte(rgb ? int.Parse(args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                var blue = Convert.ToByte(rgb ? int.Parse(args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));
                
                await role.ModifyAsync(r => r.Color = new Color(red, green, blue).RawValue).ConfigureAwait(false);
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
        public async Task Ban(IMessage imsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = imsg.Channel as ITextChannel;

            if (!string.IsNullOrWhiteSpace(msg))
            {
                await (await user.CreateDMChannelAsync()).SendMessageAsync($"**You have been BANNED from `{channel.Guild.Name}` server.**\n" +
                                        $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await channel.Guild.AddBanAsync(user, 7).ConfigureAwait(false);

                await imsg.Channel.SendMessageAsync("Banned user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.BanMembers)]
        public async Task Softban(IMessage imsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = imsg.Channel as ITextChannel;

            if (user == null)
            {
                await imsg.Channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await user.SendMessageAsync($"**You have been SOFT-BANNED from `{channel.Guild.Name}` server.**\n" +
                                        $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await channel.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
                await channel.Guild.RemoveBanAsync(user).ConfigureAwait(false);

                await imsg.Channel.SendMessageAsync("Soft-Banned user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Kick(IMessage imsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = imsg.Channel as ITextChannel;

            if (user == null)
            {
                await imsg.Channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await user.SendMessageAsync($"**You have been KICKED from `{channel.Guild.Name}` server.**\n" +
                                      $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false); // temp solution; give time for a message to be send, fu volt
            }
            try
            {
                await user.KickAsync().ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync("Kicked user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await imsg.Channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task Mute(IMessage imsg, params IGuildUser[] users)
        {
            var channel = imsg.Channel as ITextChannel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr => usr.Mute = true).ConfigureAwait(false);
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
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IMessage imsg, params IGuildUser[] users)
        {
            var channel = imsg.Channel as ITextChannel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr => usr.Mute = false).ConfigureAwait(false);
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
        public async Task Deafen(IMessage imsg, params IGuildUser[] users)
        {
            var channel = imsg.Channel as ITextChannel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr=>usr.Deaf = true).ConfigureAwait(false);
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
        public async Task UnDeafen(IMessage imsg, params IGuildUser[] users)
        {
            var channel = imsg.Channel as ITextChannel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr=> usr.Deaf = false).ConfigureAwait(false);
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
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task DelVoiChanl(IMessage imsg, [Remainder] IVoiceChannel channel)
        {
            await channel.DeleteAsync().ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Removed channel **{channel.Name}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task CreatVoiChanl(IMessage imsg, [Remainder] string channelName)
        {
            var channel = imsg.Channel as ITextChannel;
            //todo actually print info about created channel
            await channel.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Created voice channel **{channelName}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task DelTxtChanl(IMessage imsg, [Remainder] ITextChannel channel)
        {
            await channel.DeleteAsync().ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Removed text channel **{channel.Name}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task CreaTxtChanl(IMessage imsg, [Remainder] string channelName)
        {
            var channel = imsg.Channel as ITextChannel;
            //todo actually print info about created channel
            var txtCh = await channel.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync($"Added text channel **{channelName}**.").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetTopic(IMessage imsg, [Remainder] string topic = null)
        {
            var channel = imsg.Channel as ITextChannel;
            topic = topic ?? "";
            await (channel as ITextChannel).ModifyAsync(c => c.Topic = topic);
            //await (channel).ModifyAsync(c => c).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync(":ok: **New channel topic set.**").ConfigureAwait(false);

        }
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetChanlName(IMessage imsg, [Remainder] string name)
        {
            var channel = imsg.Channel as ITextChannel;

            await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
            await imsg.Channel.SendMessageAsync(":ok: **New channel name set.**").ConfigureAwait(false);
        }


        //todo maybe split into 3/4 different commands with the same name
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Prune(IMessage msg, [Remainder] string target = null)
        {
            var channel = msg.Channel as ITextChannel;

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
        //    var channel = imsg.Channel as ITextChannel;

        //    await imsg.Channel.SendMessageAsync("`Shutting down.`").ConfigureAwait(false);
        //    await Task.Delay(2000).ConfigureAwait(false);
        //    Environment.Exit(0);
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Setname(IMessage imsg, [Remainder] string newName = null)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task NewAvatar(IMessage imsg, [Remainder] string img = null)
        //{
        //    var channel = imsg.Channel as ITextChannel;

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
        //public async Task SetGame(IMessage imsg, [Remainder] string game = null)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    game = game ?? "";

        //    client.SetGame(e.GetArg("set_game"));
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Send(IMessage imsg, string where, [Remainder] string msg = null)
        //{
        //    var channel = imsg.Channel as ITextChannel;

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
        //    var channel = imsg.Channel as ITextChannel;
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

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Announce(IMessage imsg, [Remainder] string message)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    foreach (var ch in (await _client.GetGuildsAsync().ConfigureAwait(false)).Select(async g => await g.GetDefaultChannelAsync().ConfigureAwait(false)))
        //    {
        //        await imsg.Channel.SendMessageAsync(message).ConfigureAwait(false);
        //    }

        //    await imsg.Channel.SendMessageAsync(":ok:").ConfigureAwait(false);
        //}

        ////todo owner only
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task SaveChat(IMessage imsg, int cnt)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    ulong? lastmsgId = null;
        //    var sb = new StringBuilder();
        //    var msgs = new List<IMessage>(cnt);
        //    while (cnt > 0)
        //    {
        //        var dlcnt = cnt < 100 ? cnt : 100;
        //        IReadOnlyCollection<IMessage> dledMsgs;
        //        if (lastmsgId == null)
        //            dledMsgs = await imsg.Channel.GetMessagesAsync(cnt).ConfigureAwait(false);
        //        else
        //            dledMsgs = await imsg.Channel.GetMessagesAsync(lastmsgId.Value, Direction.Before, dlcnt);

        //        if (!dledMsgs.Any())
        //            break;

        //        msgs.AddRange(dledMsgs);
        //        lastmsgId = msgs[msgs.Count - 1].Id;
        //        cnt -= 100;
        //    }
        //    var title = $"Chatlog-{channel.Guild.Name}/#{channel.Name}-{DateTime.Now}.txt";
        //    await (imsg.Author as IGuildUser).SendFileAsync(
        //        await JsonConvert.SerializeObject(new { Messages = msgs.Select(s => s.ToString()) }, Formatting.Indented).ToStream().ConfigureAwait(false),
        //        title, title).ConfigureAwait(false);
        //}


        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MentionEveryone)]
        public async Task MentionRole(IMessage imsg, params IRole[] roles)
        {
            var channel = imsg.Channel as ITextChannel;

            string send = $"--{imsg.Author.Mention} has invoked a mention on the following roles--";
            foreach (var role in roles)
            { 
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", (await channel.Guild.GetUsersAsync()).Where(u => u.Roles.Contains(role)).Distinct());
            }

            while (send.Length > 2000)
            {
                var curstr = send.Substring(0, 2000);
                await channel.SendMessageAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await channel.SendMessageAsync(send).ConfigureAwait(false);
        }

        //todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Donators(IMessage imsg)
        //{
        //    var channel = imsg.Channel as ITextChannel;

        //    var rows = DbHandler.Instance.GetAllRows<Donator>();
        //    var donatorsOrdered = rows.OrderByDescending(d => d.Amount);
        //    string str = $"**Thanks to the people listed below for making this project happen!**\n";

        //    await imsg.Channel.SendMessageAsync(str + string.Join("⭐", donatorsOrdered.Select(d => d.UserName))).ConfigureAwait(false);
        //}
    }
}
