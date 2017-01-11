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
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System.Net.Http;
using System.IO;
using static NadekoBot.Modules.Permissions.Permissions;
using System.Collections.Concurrent;
using NLog;

namespace NadekoBot.Modules.Administration
{
    [NadekoModule("Administration", ".")]
    public partial class Administration : DiscordModule
    {

        private static ConcurrentDictionary<ulong, string> GuildMuteRoles { get; } = new ConcurrentDictionary<ulong, string>();

        private static ConcurrentHashSet<ulong> DeleteMessagesOnCommand { get; } = new ConcurrentHashSet<ulong>();

        private new static Logger _log { get; }

        static Administration()
        {
            _log = LogManager.GetCurrentClassLogger();
            NadekoBot.CommandHandler.CommandExecuted += DelMsgOnCmd_Handler;

            DeleteMessagesOnCommand = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs.Where(g => g.DeleteMessageOnCommand).Select(g => g.GuildId));

        }

        private static async Task DelMsgOnCmd_Handler(SocketUserMessage msg, CommandInfo cmd)
        {
            try
            {
                var channel = msg.Channel as SocketTextChannel;
                if (channel == null)
                    return;
                if (DeleteMessagesOnCommand.Contains(channel.Guild.Id))
                    await msg.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Delmsgoncmd errored...");
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetPermissions()
        {
            var channel = (ITextChannel)Context.Channel;
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.PermissionsFor(Context.Guild.Id);
                config.RootPermission = Permission.GetDefaultRoot();
                var toAdd = new PermissionCache()
                {
                    RootPermission = config.RootPermission,
                    PermRole = config.PermissionRole,
                    Verbose = config.VerbosePermissions,
                };
                Permissions.Permissions.Cache.AddOrUpdate(channel.Guild.Id,
                    toAdd, (id, old) => toAdd);
                await uow.CompleteAsync();
            }

            await channel.SendConfirmAsync($"{Context.Message.Author.Mention} 🆗 **Permissions for this server are reset.**");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Delmsgoncmd()
        {
            bool enabled;
            using (var uow = DbHandler.UnitOfWork())
            {
                var conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                await uow.CompleteAsync();
            }
            if (enabled)
            {
                DeleteMessagesOnCommand.Add(Context.Guild.Id);
                await Context.Channel.SendConfirmAsync("✅ **Now automatically deleting successful command invokations.**").ConfigureAwait(false);
            }
            else
            {
                DeleteMessagesOnCommand.TryRemove(Context.Guild.Id);
                await Context.Channel.SendConfirmAsync("❗**Stopped automatic deletion of successful command invokations.**").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Setrole(IGuildUser usr, [Remainder] IRole role)
        {
            try
            {
                await usr.AddRolesAsync(role).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"ℹ️ Successfully added role **{role.Name}** to user **{usr.Username}**").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync("⚠️ Failed to add role. **Bot has insufficient permissions.**\n").ConfigureAwait(false);
                Console.WriteLine(ex.ToString());
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Removerole(IGuildUser usr, [Remainder] IRole role)
        {
            try
            {
                await usr.RemoveRolesAsync(role).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"ℹ️ Successfully removed role **{role.Name}** from user **{usr.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ Failed to remove role. Most likely reason: **Insufficient permissions.**").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RenameRole(IRole roleToEdit, string newname)
        {
            try
            {
                if (roleToEdit.Position > (await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false)).GetRoles().Max(r => r.Position))
                {
                    await Context.Channel.SendErrorAsync("🚫 You can't edit roles higher than your highest role.").ConfigureAwait(false);
                    return;
                }
                await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync("✅ Role renamed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("⚠️ Failed to rename role. Probably **insufficient permissions.**").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveAllRoles([Remainder] IGuildUser user)
        {
            try
            {
                await user.RemoveRolesAsync(user.GetRoles()).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"🗑 Successfully removed **all** roles from user **{user.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ Failed to remove roles. Most likely reason: **Insufficient permissions.**").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task CreateRole([Remainder] string roleName = null)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return;
            try
            {
                var r = await Context.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"✅ Successfully created role **{r.Name}**.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("⚠️ Unspecified error.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleColor(params string[] args)
        {
            if (args.Count() != 2 && args.Count() != 4)
            {
                await Context.Channel.SendErrorAsync("❌ The parameters specified are **invalid.**").ConfigureAwait(false);
                return;
            }
            var roleName = args[0].ToUpperInvariant();
            var role = Context.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleName).FirstOrDefault();

            if (role == null)
            {
                await Context.Channel.SendErrorAsync("🚫 That role **does not exist.**").ConfigureAwait(false);
                return;
            }
            try
            {
                var rgb = args.Count() == 4;
                var arg1 = args[1].Replace("#", "");

                var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                var green = Convert.ToByte(rgb ? int.Parse(args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                var blue = Convert.ToByte(rgb ? int.Parse(args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));

                await role.ModifyAsync(r => r.Color = new Color(red, green, blue)).ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync($"☑️ Role **{role.Name}'s** color has been changed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await Context.Channel.SendErrorAsync("⚠️ Error occured, most likely **invalid parameters** or **insufficient permissions.**").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, [Remainder] string msg = null)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "❗️No reason provided.";
            }
            if (Context.User.Id != user.Guild.OwnerId && (user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max()))
            {
                await Context.Channel.SendErrorAsync("⚠️ You can't use this command on users with a role higher or equal to yours in the role hierarchy.").ConfigureAwait(false);
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await (await user.CreateDMChannelAsync()).SendErrorAsync($"⛔️ **You have been BANNED from `{Context.Guild.Name}` server.**\n" +
                                            $"⚖ *Reason:* {msg}").ConfigureAwait(false);
                    await Task.Delay(2000).ConfigureAwait(false);

                }
                catch { }
            }
            try
            {
                await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);

                await Context.Channel.SendConfirmAsync("⛔️ **Banned** user **" + user.Username + "** ID: `" + user.Id + "`").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ **Error.** Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Softban(IGuildUser user, [Remainder] string msg = null)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "❗️No reason provided.";
            }
            if (Context.User.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
            {
                await Context.Channel.SendErrorAsync("⚠️ You can't use this command on users with a role higher or equal to yours in the role hierarchy.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendErrorAsync($"☣ **You have been SOFT-BANNED from `{Context.Guild.Name}` server.**\n" +
                  $"⚖ *Reason:* {msg}").ConfigureAwait(false);
                    await Task.Delay(2000).ConfigureAwait(false);
                }
                catch { }
            }

            try
            {
                await Context.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
                try { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
                catch { await Context.Guild.RemoveBanAsync(user).ConfigureAwait(false); }

                await Context.Channel.SendConfirmAsync("☣ **Soft-Banned** user **" + user.Username + "** ID: `" + user.Id + "`").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, [Remainder] string msg = null)
        {
            if (user == null)
            {
                await Context.Channel.SendErrorAsync("❗️User not found.").ConfigureAwait(false);
                return;
            }

            if (Context.Message.Author.Id != user.Guild.OwnerId && user.GetRoles().Select(r => r.Position).Max() >= ((IGuildUser)Context.User).GetRoles().Select(r => r.Position).Max())
            {
                await Context.Channel.SendErrorAsync("⚠️ You can't use this command on users with a role higher or equal to yours in the role hierarchy.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendErrorAsync($"‼️**You have been KICKED from `{Context.Guild.Name}` server.**\n" +
                                    $"⚖ *Reason:* {msg}").ConfigureAwait(false);
                    await Task.Delay(2000).ConfigureAwait(false);
                }
                catch { }
            }
            try
            {
                await user.KickAsync().ConfigureAwait(false);
                await Context.Channel.SendConfirmAsync("‼️**Kicked** user **" + user.Username + "** ID: `" + user.Id + "`").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.DeafenMembers)]
        public async Task Deafen(params IGuildUser[] users)
        {
            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr => usr.Deaf = true).ConfigureAwait(false);
                }
                await Context.Channel.SendConfirmAsync("🔇 **Deafen** successful.").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.DeafenMembers)]
        public async Task UnDeafen(params IGuildUser[] users)
        {
            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr => usr.Deaf = false).ConfigureAwait(false);
                }
                await Context.Channel.SendConfirmAsync("🔊 **Undeafen** successful.").ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task DelVoiChanl([Remainder] IVoiceChannel voiceChannel)
        {
            await voiceChannel.DeleteAsync().ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync($"🗑 Removed voice channel **{voiceChannel.Name}** successfully.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreatVoiChanl([Remainder] string channelName)
        {
            var ch = await Context.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync($"✅ Created voice channel **{ch.Name}**. ID: `{ch.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task DelTxtChanl([Remainder] ITextChannel toDelete)
        {
            await toDelete.DeleteAsync().ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync($"🗑 Removed text channel **{toDelete.Name}**. ID: `{toDelete.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreaTxtChanl([Remainder] string channelName)
        {
            var txtCh = await Context.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
            await Context.Channel.SendConfirmAsync($"✅ Added text channel **{txtCh.Name}**. ID: `{txtCh.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetTopic([Remainder] string topic = null)
        {
            var channel = (ITextChannel)Context.Channel;
            topic = topic ?? "";
            await channel.ModifyAsync(c => c.Topic = topic);
            await channel.SendConfirmAsync("🆗 **New channel topic set.**").ConfigureAwait(false);

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetChanlName([Remainder] string name)
        {
            var channel = (ITextChannel)Context.Channel;
            await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
            await channel.SendConfirmAsync("🆗 **New channel name set.**").ConfigureAwait(false);
        }


        //delets her own messages, no perm required
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Prune()
        {
            var user = await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

            var enumerable = (await Context.Channel.GetMessagesAsync().Flatten()).AsEnumerable();
            enumerable = enumerable.Where(x => x.Author.Id == user.Id);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
        }

        // prune x
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Prune(int count)
        {
            if (count < 1)
                return;
            count += 1;
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            int limit = (count < 100) ? count : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten().ConfigureAwait(false));
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
        }

        //prune @user [x]
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Prune(IGuildUser user, int count = 100)
        {
            if (count < 1)
                return;

            if (user.Id == Context.User.Id)
                count += 1;

            int limit = (count < 100) ? count : 100;
            var enumerable = (await Context.Channel.GetMessagesAsync(limit: limit).Flatten()).Where(m => m.Author == user);
            await Context.Channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.MentionEveryone)]
        public async Task MentionRole(params IRole[] roles)
        {
            string send = $"❕{Context.User.Mention} has invoked a mention on the following roles ❕";
            foreach (var role in roles)
            {
                send += $"\n**{role.Name}**\n";
                send += string.Join(", ", (await Context.Guild.GetUsersAsync()).Where(u => u.GetRoles().Contains(role)).Distinct().Select(u => u.Mention));
            }

            while (send.Length > 2000)
            {
                var curstr = send.Substring(0, 2000);
                await Context.Channel.SendMessageAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await Context.Channel.SendMessageAsync(send).ConfigureAwait(false);
        }

        IGuild nadekoSupportServer;
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Donators()
        {
            IEnumerable<Donator> donatorsOrdered;

            using (var uow = DbHandler.UnitOfWork())
            {
                donatorsOrdered = uow.Donators.GetDonatorsOrdered();
            }
            await Context.Channel.SendConfirmAsync("Thanks to the people listed below for making this project happen!", string.Join("⭐", donatorsOrdered.Select(d => d.Name))).ConfigureAwait(false);

            nadekoSupportServer = nadekoSupportServer ?? NadekoBot.Client.GetGuild(117523346618318850);

            if (nadekoSupportServer == null)
                return;

            var patreonRole = nadekoSupportServer.GetRole(236667642088259585);
            if (patreonRole == null)
                return;

            var usrs = (await nadekoSupportServer.GetUsersAsync()).Where(u => u.RoleIds.Contains(236667642088259585u));
            await Context.Channel.SendConfirmAsync("Patreon supporters", string.Join("⭐", usrs.Select(d => d.Username))).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task Donadd(IUser donator, int amount)
        {
            Donator don;
            using (var uow = DbHandler.UnitOfWork())
            {
                don = uow.Donators.AddOrUpdateDonator(donator.Id, donator.Username, amount);
                await uow.CompleteAsync();
            }

            await Context.Channel.SendConfirmAsync($"Successfuly added a new donator. Total donated amount from this user: {don.Amount} 👑").ConfigureAwait(false);
        }
    }
}