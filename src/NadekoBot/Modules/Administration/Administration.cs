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

namespace NadekoBot.Modules.Administration
{
    [NadekoModule("Administration", ".")]
    public partial class Administration : DiscordModule
    {

        private static ConcurrentDictionary<ulong, string> GuildMuteRoles { get; } = new ConcurrentDictionary<ulong, string>();

        public Administration(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {
            NadekoBot.CommandHandler.CommandExecuted += DelMsgOnCmd_Handler;
        }

        static Administration()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var configs = NadekoBot.AllGuildConfigs;
                GuildMuteRoles = new ConcurrentDictionary<ulong, string>(configs
                        .Where(c=>!string.IsNullOrWhiteSpace(c.MuteRoleName))
                        .ToDictionary(c => c.GuildId, c => c.MuteRoleName));
            }
        }

        private async void DelMsgOnCmd_Handler(object sender, CommandExecutedEventArgs e)
        {
            try
            {
                var channel = e.Message.Channel as ITextChannel;
                if (channel == null)
                    return;

                //todo cache this
                bool shouldDelete;
                using (var uow = DbHandler.UnitOfWork())
                {
                    shouldDelete = uow.GuildConfigs.For(channel.Guild.Id).DeleteMessageOnCommand;
                }

                if (shouldDelete)
                    await e.Message.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Delmsgoncmd errored...");
            }
        }

        private static async Task<IRole> GetMuteRole(IGuild guild)
        {
            const string defaultMuteRoleName = "nadeko-mute";

            var muteRoleName = GuildMuteRoles.GetOrAdd(guild.Id, defaultMuteRoleName);

            var muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName);
            if (muteRole == null)
            {

                //if it doesn't exist, create it 
                try { muteRole = await guild.CreateRoleAsync(muteRoleName, GuildPermissions.None).ConfigureAwait(false); }
                catch
                {
                    //if creations fails,  maybe the name is not correct, find default one, if doesn't work, create default one
                    muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName) ??
                        await guild.CreateRoleAsync(defaultMuteRoleName, GuildPermissions.None).ConfigureAwait(false);
                }

                foreach (var toOverwrite in guild.GetTextChannels())
                {
                    try
                    {
                        await toOverwrite.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(sendMessages: PermValue.Deny, attachFiles: PermValue.Deny))
                                .ConfigureAwait(false);
                    }
                    catch { }
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            return muteRole;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.Administrator)]
        public async Task ResetPermissions(IUserMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;

            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.PermissionsFor(channel.Guild.Id);
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

            await channel.SendMessageAsync($"{imsg.Author.Mention} :ok: `Permissions for this server are reset`");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.Administrator)]
        public async Task Delmsgoncmd(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            GuildConfig conf;
            using (var uow = DbHandler.UnitOfWork())
            {
                conf = uow.GuildConfigs.For(channel.Guild.Id);
                conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;
                uow.GuildConfigs.Update(conf);
                await uow.CompleteAsync();
            }
            if (conf.DeleteMessageOnCommand)
                await channel.SendMessageAsync("❗`Now automatically deleting successfull command invokations.`").ConfigureAwait(false);
            else
                await channel.SendMessageAsync("❗`Stopped automatic deletion of successfull command invokations.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task Setrole(IUserMessage umsg, IGuildUser usr, [Remainder] IRole role)
        {
            var channel = (ITextChannel)umsg.Channel;
            try
            {
                await usr.AddRolesAsync(role).ConfigureAwait(false);
                await channel.SendMessageAsync($"Successfully added role **{role.Name}** to user **{usr.Username}**").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync("Failed to add roles. Bot has insufficient permissions.\n").ConfigureAwait(false);
                Console.WriteLine(ex.ToString());
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task Removerole(IUserMessage umsg, IGuildUser usr, [Remainder] IRole role)
        {
            var channel = (ITextChannel)umsg.Channel;
            try
            {
                await usr.RemoveRolesAsync(role).ConfigureAwait(false);
                await channel.SendMessageAsync($"Successfully removed role **{role.Name}** from user **{usr.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RenameRole(IUserMessage umsg, IRole roleToEdit, string newname)
        {
            var channel = (ITextChannel)umsg.Channel;
            try
            {
                if (roleToEdit.Position > (await channel.Guild.GetCurrentUserAsync().ConfigureAwait(false)).Roles.Max(r => r.Position))
                {
                    await channel.SendMessageAsync("You can't edit roles higher than your highest role.").ConfigureAwait(false);
                    return;
                }
                await roleToEdit.ModifyAsync(g => g.Name = newname).ConfigureAwait(false);
                await channel.SendMessageAsync("Role renamed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await channel.SendMessageAsync("Failed to rename role. Probably insufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RemoveAllRoles(IUserMessage umsg, [Remainder] IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.RemoveRolesAsync(user.Roles).ConfigureAwait(false);
                await channel.SendMessageAsync($"Successfully removed **all** roles from user **{user.Username}**").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("Failed to remove roles. Most likely reason: Insufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task CreateRole(IUserMessage umsg, [Remainder] string roleName = null)
        {
            var channel = (ITextChannel)umsg.Channel;


            if (string.IsNullOrWhiteSpace(roleName))
                return;
            try
            {
                var r = await channel.Guild.CreateRoleAsync(roleName).ConfigureAwait(false);
                await channel.SendMessageAsync($"Successfully created role **{r.Name}**.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await channel.SendMessageAsync(":warning: Unspecified error.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task RoleColor(IUserMessage umsg, params string[] args)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (args.Count() != 2 && args.Count() != 4)
            {
                await channel.SendMessageAsync("The parameters are invalid.").ConfigureAwait(false);
                return;
            }
            var roleName = args[0].ToUpperInvariant();
            var role = channel.Guild.Roles.Where(r=>r.Name.ToUpperInvariant() == roleName).FirstOrDefault();

            if (role == null)
            {
                await channel.SendMessageAsync("That role does not exist.").ConfigureAwait(false);
                return;
            }
            try
            {
                var rgb = args.Count() == 4;
                var arg1 = args[1].Replace("#", "");

                var red = Convert.ToByte(rgb ? int.Parse(arg1) : Convert.ToInt32(arg1.Substring(0, 2), 16));
                var green = Convert.ToByte(rgb ? int.Parse(args[2]) : Convert.ToInt32(arg1.Substring(2, 2), 16));
                var blue = Convert.ToByte(rgb ? int.Parse(args[3]) : Convert.ToInt32(arg1.Substring(4, 2), 16));
                
                await role.ModifyAsync(r => r.Color = new Discord.Color(red, green, blue).RawValue).ConfigureAwait(false);
                await channel.SendMessageAsync($"Role {role.Name}'s color has been changed.").ConfigureAwait(false);
            }
            catch (Exception)
            {
                await channel.SendMessageAsync("Error occured, most likely invalid parameters or insufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.BanMembers)]
        public async Task Ban(IUserMessage umsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "No reason provided.";
            }
            if (umsg.Author.Id != user.Guild.OwnerId && user.Roles.Select(r=>r.Position).Max() >= ((IGuildUser)umsg.Author).Roles.Select(r => r.Position).Max())
            {
                await channel.SendMessageAsync("You can't use this command on users with a role higher or equal to yours in the role hierarchy.");
                return;
            }
            try
            {
                await (await user.CreateDMChannelAsync()).SendMessageAsync($"**You have been BANNED from `{channel.Guild.Name}` server.**\n" +
                                        $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);
            }
            catch { }
            try
            {
                await channel.Guild.AddBanAsync(user, 7).ConfigureAwait(false);

                await channel.SendMessageAsync("Banned user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.KickMembers)]
        [RequirePermission(GuildPermission.ManageMessages)]
        public async Task Softban(IUserMessage umsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (string.IsNullOrWhiteSpace(msg))
            {
                msg = "No reason provided.";
            }
            if (umsg.Author.Id != user.Guild.OwnerId && user.Roles.Select(r => r.Position).Max() >= ((IGuildUser)umsg.Author).Roles.Select(r => r.Position).Max())
            {
                await channel.SendMessageAsync("You can't use this command on users with a role higher or equal to yours in the role hierarchy.");
                return;
            }
            try
            {
                await user.SendMessageAsync($"**You have been SOFT-BANNED from `{channel.Guild.Name}` server.**\n" +
              $"Reason: {msg}").ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);
            }
            catch { }
            try
            {
                await channel.Guild.AddBanAsync(user, 7).ConfigureAwait(false);
                try { await channel.Guild.RemoveBanAsync(user).ConfigureAwait(false); }
                catch { await channel.Guild.RemoveBanAsync(user).ConfigureAwait(false); }

                await channel.SendMessageAsync("Soft-Banned user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.KickMembers)]
        public async Task Kick(IUserMessage umsg, IGuildUser user, [Remainder] string msg = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (user == null)
            {
                await channel.SendMessageAsync("User not found.").ConfigureAwait(false);
                return;
            }

            if (umsg.Author.Id != user.Guild.OwnerId && user.Roles.Select(r => r.Position).Max() >= ((IGuildUser)umsg.Author).Roles.Select(r => r.Position).Max())
            {
                await channel.SendMessageAsync("You can't use this command on users with a role higher or equal to yours in the role hierarchy.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(msg))
            {
                try
                {
                    await user.SendMessageAsync($"**You have been KICKED from `{channel.Guild.Name}` server.**\n" +
                                    $"Reason: {msg}").ConfigureAwait(false);
                    await Task.Delay(2000).ConfigureAwait(false);
                }
                catch { }
            }
            try
            {
                await user.KickAsync().ConfigureAwait(false);
                await channel.SendMessageAsync("Kicked user " + user.Username + " Id: " + user.Id).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("Error. Most likely I don't have sufficient permissions.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        [Priority(1)]
        public async Task SetMuteRole(IUserMessage imsg, [Remainder] string name)
        {
            var channel = (ITextChannel)imsg.Channel;
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;
            
            using (var uow = DbHandler.UnitOfWork())
            {
                var config = uow.GuildConfigs.For(channel.Guild.Id);
                config.MuteRoleName = name;
                GuildMuteRoles.AddOrUpdate(channel.Guild.Id, name, (id, old) => name);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await channel.SendMessageAsync("`New mute role set.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        [Priority(0)]
        public Task SetMuteRole(IUserMessage imsg, [Remainder] IRole role)
            => SetMuteRole(imsg, role.Name);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task Mute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.ModifyAsync(usr => usr.Mute = true).ConfigureAwait(false);
                await user.AddRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was muted from text and voice chat successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.ModifyAsync(usr => usr.Mute = false).ConfigureAwait(false);
                await user.RemoveRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was unmuted from text and voice chat successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task ChatMute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.AddRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was muted from chatting successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageRoles)]
        public async Task ChatUnmute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.RemoveRolesAsync(await GetMuteRole(channel.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was unmuted from chatting successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task VoiceMute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                await user.ModifyAsync(usr => usr.Mute = true).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was voice muted successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MuteMembers)]
        public async Task VoiceUnmute(IUserMessage umsg, IGuildUser user)
        {
            var channel = (ITextChannel)umsg.Channel;
            try
            {
                await user.ModifyAsync(usr => usr.Mute = false).ConfigureAwait(false);
                await channel.SendMessageAsync($"**{user}** was voice unmuted successfully.").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.DeafenMembers)]
        public async Task Deafen(IUserMessage umsg, params IGuildUser[] users)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr=>usr.Deaf = true).ConfigureAwait(false);
                }
                await channel.SendMessageAsync("Deafen successful").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.DeafenMembers)]
        public async Task UnDeafen(IUserMessage umsg, params IGuildUser[] users)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!users.Any())
                return;
            try
            {
                foreach (var u in users)
                {
                    await u.ModifyAsync(usr=> usr.Deaf = false).ConfigureAwait(false);
                }
                await channel.SendMessageAsync("Undeafen successful").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync("I most likely don't have the permission necessary for that.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task DelVoiChanl(IUserMessage umsg, [Remainder] IVoiceChannel voiceChannel)
        {
            await voiceChannel.DeleteAsync().ConfigureAwait(false);
            await umsg.Channel.SendMessageAsync($"Removed channel **{voiceChannel.Name}**.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task CreatVoiChanl(IUserMessage umsg, [Remainder] string channelName)
        {
            var channel = (ITextChannel)umsg.Channel;
            var ch = await channel.Guild.CreateVoiceChannelAsync(channelName).ConfigureAwait(false);
            await channel.SendMessageAsync($"Created voice channel **{ch.Name}**, id `{ch.Id}`.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task DelTxtChanl(IUserMessage umsg, [Remainder] ITextChannel channel)
        {
            await channel.DeleteAsync().ConfigureAwait(false);
            await channel.SendMessageAsync($"Removed text channel **{channel.Name}**, id `{channel.Id}`.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task CreaTxtChanl(IUserMessage umsg, [Remainder] string channelName)
        {
            var channel = (ITextChannel)umsg.Channel;
            var txtCh = await channel.Guild.CreateTextChannelAsync(channelName).ConfigureAwait(false);
            await channel.SendMessageAsync($"Added text channel **{txtCh.Name}**, id `{txtCh.Id}`.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetTopic(IUserMessage umsg, [Remainder] string topic = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            topic = topic ?? "";
            await channel.ModifyAsync(c => c.Topic = topic);
            await channel.SendMessageAsync(":ok: **New channel topic set.**").ConfigureAwait(false);

        }
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.ManageChannels)]
        public async Task SetChanlName(IUserMessage umsg, [Remainder] string name)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.ModifyAsync(c => c.Name = name).ConfigureAwait(false);
            await channel.SendMessageAsync(":ok: **New channel name set.**").ConfigureAwait(false);
        }


        //delets her own messages, no perm required
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Prune(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            var user = channel.Guild.GetCurrentUser();
            
            var enumerable = (await umsg.Channel.GetMessagesAsync()).Where(x => x.Author.Id == user.Id);
            await umsg.Channel.DeleteMessagesAsync(enumerable);
        }

        // prune x
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(ChannelPermission.ManageMessages)]
        public async Task Prune(IUserMessage msg, int count)
        {
            var channel = (ITextChannel)msg.Channel;
            await (msg as IUserMessage).DeleteAsync();
            int limit = (count < 100) ? count : 100;
            var enumerable = (await msg.Channel.GetMessagesAsync(limit: limit));
            await msg.Channel.DeleteMessagesAsync(enumerable);
        }

        //prune @user [x]
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(ChannelPermission.ManageMessages)]
        public async Task Prune(IUserMessage msg, IGuildUser user, int count = 100)
        {
            var channel = (ITextChannel)msg.Channel;
            int limit = (count < 100) ? count : 100;
            var enumerable = (await msg.Channel.GetMessagesAsync(limit: limit)).Where(m => m.Author == user);
            await msg.Channel.DeleteMessagesAsync(enumerable);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Die(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            try { await channel.SendMessageAsync("`Shutting down.`").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
            await Task.Delay(2000).ConfigureAwait(false);
            Environment.Exit(0);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Setname(IUserMessage umsg, [Remainder] string newName)
        {
            var channel = (ITextChannel)umsg.Channel;
            if (string.IsNullOrWhiteSpace(newName))
                return;

            await (await NadekoBot.Client.GetCurrentUserAsync()).ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

            await channel.SendMessageAsync($"Successfully changed name to {newName}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetAvatar(IUserMessage umsg, [Remainder] string img = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (string.IsNullOrWhiteSpace(img))
                return;

            using (var http = new HttpClient())
            {
                using (var sr = await http.GetStreamAsync(img))
                {
                    var imgStream = new MemoryStream();
                    await sr.CopyToAsync(imgStream);
                    imgStream.Position = 0;

                    await (await NadekoBot.Client.GetCurrentUserAsync().ConfigureAwait(false)).ModifyAsync(u => u.Avatar = imgStream).ConfigureAwait(false);
                }
            }

            await channel.SendMessageAsync("New avatar set.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetGame(IUserMessage umsg, [Remainder] string game = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            game = game ?? "";

            await NadekoBot.Client.SetGame(game).ConfigureAwait(false);

            await channel.SendMessageAsync("`New game set.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetStream(IUserMessage umsg, string url, [Remainder] string name = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            name = name ?? "";

            await NadekoBot.Client.SetStream(name, url).ConfigureAwait(false);

            await channel.SendMessageAsync("`New stream set.`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Send(IUserMessage umsg, string where, [Remainder] string msg = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (string.IsNullOrWhiteSpace(msg))
                return;

            var ids = where.Split('|');
            if (ids.Length != 2)
                return;
            var sid = ulong.Parse(ids[0]);
            var server = NadekoBot.Client.GetGuilds().Where(s => s.Id == sid).FirstOrDefault();

            if (server == null)
                return;

            if (ids[1].ToUpperInvariant().StartsWith("C:"))
            {
                var cid = ulong.Parse(ids[1].Substring(2));
                var ch = server.GetTextChannels().Where(c => c.Id == cid).FirstOrDefault();
                if (ch == null)
                {
                    return;
                }
                await ch.SendMessageAsync(msg).ConfigureAwait(false);
            }
            else if (ids[1].ToUpperInvariant().StartsWith("U:"))
            {
                var uid = ulong.Parse(ids[1].Substring(2));
                var user = server.GetUsers().Where(u => u.Id == uid).FirstOrDefault();
                if (user == null)
                {
                    return;
                }
                await user.SendMessageAsync(msg).ConfigureAwait(false);
            }
            else
            {
                await channel.SendMessageAsync("`Invalid format.`").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Announce(IUserMessage umsg, [Remainder] string message)
        {
            var channel = (ITextChannel)umsg.Channel;

            var channels = await Task.WhenAll(_client.GetGuilds().Select(g =>
                g.GetDefaultChannelAsync()
            )).ConfigureAwait(false);

            await Task.WhenAll(channels.Select(c => c.SendMessageAsync($"`Message from {umsg.Author} (Bot Owner):` " + message)))
                    .ConfigureAwait(false);

            await channel.SendMessageAsync(":ok:").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveChat(IUserMessage umsg, int cnt)
        {
            var channel = (ITextChannel)umsg.Channel;

            ulong? lastmsgId = null;
            var sb = new StringBuilder();
            var msgs = new List<IMessage>(cnt);
            while (cnt > 0)
            {
                var dlcnt = cnt < 100 ? cnt : 100;
                IReadOnlyCollection<IMessage> dledMsgs;
                if (lastmsgId == null)
                    dledMsgs = await umsg.Channel.GetMessagesAsync(cnt).ConfigureAwait(false);
                else
                    dledMsgs = await umsg.Channel.GetMessagesAsync(lastmsgId.Value, Direction.Before, dlcnt);

                if (!dledMsgs.Any())
                    break;

                msgs.AddRange(dledMsgs);
                lastmsgId = msgs[msgs.Count - 1].Id;
                cnt -= 100;
            }
            var title = $"Chatlog-{channel.Guild.Name}/#{channel.Name}-{DateTime.Now}.txt";
            await (umsg.Author as IGuildUser).SendFileAsync(
                await JsonConvert.SerializeObject(new { Messages = msgs.Select(s => $"【{s.Timestamp:HH:mm:ss}】{s.Author}:" + s.ToString()) }, Formatting.Indented).ToStream().ConfigureAwait(false),
                title, title).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(GuildPermission.MentionEveryone)]
        public async Task MentionRole(IUserMessage umsg, params IRole[] roles)
        {
            var channel = (ITextChannel)umsg.Channel;

            string send = $"--{umsg.Author.Mention} has invoked a mention on the following roles--";
            foreach (var role in roles)
            { 
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", (await channel.Guild.GetUsersAsync()).Where(u => u.Roles.Contains(role)).Distinct().Select(u=>u.Mention));
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

        IGuild nadekoSupportServer;
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Donators(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            IEnumerable<Donator> donatorsOrdered;

            using (var uow = DbHandler.UnitOfWork())
            {
                donatorsOrdered = uow.Donators.GetDonatorsOrdered();
            }

            string str = $"**Thanks to the people listed below for making this project happen!**\n";
            await channel.SendMessageAsync(str + string.Join("⭐", donatorsOrdered.Select(d => d.Name))).ConfigureAwait(false);
            
            nadekoSupportServer = nadekoSupportServer ?? NadekoBot.Client.GetGuild(117523346618318850);

            if (nadekoSupportServer == null)
                return;

            var patreonRole = nadekoSupportServer.GetRole(236667642088259585);
            if (patreonRole == null)
                return;

            var usrs = nadekoSupportServer.GetUsers().Where(u => u.Roles.Contains(patreonRole));
            await channel.SendMessageAsync("\n`Patreon supporters:`\n" + string.Join("⭐", usrs.Select(d => d.Username))).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Donadd(IUserMessage umsg, IUser donator, int amount)
        {
            var channel = (ITextChannel)umsg.Channel;

            Donator don;
            using (var uow = DbHandler.UnitOfWork())
            {
                don = uow.Donators.AddOrUpdateDonator(donator.Id, donator.Username, amount);
                await uow.CompleteAsync();
            }

            await channel.SendMessageAsync($"Successfuly added a new donator. Total donated amount from this user: {don.Amount} 👑").ConfigureAwait(false);
        }
    }
}
