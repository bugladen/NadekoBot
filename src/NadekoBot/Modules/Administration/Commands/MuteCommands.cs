using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands : ModuleBase
        {
            private static ConcurrentDictionary<ulong, string> GuildMuteRoles { get; } = new ConcurrentDictionary<ulong, string>();

            private static ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> MutedUsers { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();

            public static event Action<IGuildUser, MuteType> UserMuted = delegate { };
            public static event Action<IGuildUser, MuteType> UserUnmuted = delegate { };


            public enum MuteType {
                Voice,
                Chat,
                All
            }

            static MuteCommands()
            {
                var _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
                
                var configs = NadekoBot.AllGuildConfigs;
                GuildMuteRoles = new ConcurrentDictionary<ulong, string>(configs
                        .Where(c => !string.IsNullOrWhiteSpace(c.MuteRoleName))
                        .ToDictionary(c => c.GuildId, c => c.MuteRoleName));

                MutedUsers = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>(configs.ToDictionary(
                    k => k.GuildId,
                    v => new ConcurrentHashSet<ulong>(v.MutedUsers.Select(m => m.UserId))
                ));

                NadekoBot.Client.UserJoined += Client_UserJoined;

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            private static async void Client_UserJoined(IGuildUser usr)
            {
                try
                {
                    ConcurrentHashSet<ulong> muted;
                    MutedUsers.TryGetValue(usr.Guild.Id, out muted);

                    if (muted == null || !muted.Contains(usr.Id))
                        return;
                    else
                        await MuteUser(usr).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
                    
            }

            public static async Task MuteUser(IGuildUser usr)
            {
                await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                await usr.AddRolesAsync(await GetMuteRole(usr.Guild)).ConfigureAwait(false);
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id, set => set.Include(gc => gc.MutedUsers));
                    config.MutedUsers.Add(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    ConcurrentHashSet<ulong> muted;
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out muted))
                        muted.Add(usr.Id);
                    
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                UserMuted(usr, MuteType.All);
            }

            public static async Task UnmuteUser(IGuildUser usr)
            {
                await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                await usr.RemoveRolesAsync(await GetMuteRole(usr.Guild)).ConfigureAwait(false);
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id, set => set.Include(gc => gc.MutedUsers));
                    config.MutedUsers.Remove(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    ConcurrentHashSet<ulong> muted;
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out muted))
                        muted.TryRemove(usr.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                UserUnmuted(usr, MuteType.All);
            }

            public static async Task<IRole> GetMuteRole(IGuild guild)
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

                    foreach (var toOverwrite in (await guild.GetTextChannelsAsync()))
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
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public async Task SetMuteRole([Remainder] string name)
            {
                //var channel = (ITextChannel)Context.Channel;
                name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    config.MuteRoleName = name;
                    GuildMuteRoles.AddOrUpdate(Context.Guild.Id, name, (id, old) => name);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await Context.Channel.SendConfirmAsync("☑️ **New mute role set.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public Task SetMuteRole([Remainder] IRole role)
                => SetMuteRole(role.Name);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Mute(IGuildUser user)
            {
                try
                {
                    await MuteUser(user).ConfigureAwait(false);                    
                    await Context.Channel.SendConfirmAsync($"🔇 **{user}** has been **muted** from text and voice chat.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Unmute(IGuildUser user)
            {
                try
                {
                    await UnmuteUser(user).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync($"🔉 **{user}** has been **unmuted** from text and voice chat.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ChatMute(IGuildUser user)
            {
                try
                {
                    await user.AddRolesAsync(await GetMuteRole(Context.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                    UserMuted(user, MuteType.Chat);
                    await Context.Channel.SendConfirmAsync($"✏️🚫 **{user}** has been **muted** from chatting.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task ChatUnmute(IGuildUser user)
            {
                try
                {
                    await user.RemoveRolesAsync(await GetMuteRole(Context.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                    UserUnmuted(user, MuteType.Chat);
                    await Context.Channel.SendConfirmAsync($"✏️✅ **{user}** has been **unmuted** from chatting.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceMute(IGuildUser user)
            {
                try
                {
                    await user.ModifyAsync(usr => usr.Mute = true).ConfigureAwait(false);
                    UserMuted(user, MuteType.Voice);
                    await Context.Channel.SendConfirmAsync($"🎙🚫 **{user}** has been **voice muted**.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceUnmute(IGuildUser user)
            {
                try
                {
                    await user.ModifyAsync(usr => usr.Mute = false).ConfigureAwait(false);
                    UserUnmuted(user, MuteType.Voice);
                    await Context.Channel.SendConfirmAsync($"🎙✅ **{user}** has been **voice unmuted**.").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("⚠️ I most likely don't have the permission necessary for that.").ConfigureAwait(false);
                }
            }
        }
    }
}
