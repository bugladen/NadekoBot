using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VoicePlusTextCommands : ModuleBase
        {
            private static Regex channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);

            private static ConcurrentHashSet<ulong> voicePlusTextCache { get; }

            private static ConcurrentDictionary<ulong, SemaphoreSlim> guildLockObjects = new ConcurrentDictionary<ulong, SemaphoreSlim>();
            static VoicePlusTextCommands()
            {
                var _log = LogManager.GetCurrentClassLogger();
                var sw = Stopwatch.StartNew();
                using (var uow = DbHandler.UnitOfWork())
                {
                    voicePlusTextCache = new ConcurrentHashSet<ulong>(NadekoBot.AllGuildConfigs.Where(g => g.VoicePlusTextEnabled).Select(g => g.GuildId));
                }
                NadekoBot.Client.UserVoiceStateUpdated += UserUpdatedEventHandler;

                sw.Stop();
                _log.Debug($"Loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }

            private static Task UserUpdatedEventHandler(SocketUser iuser, SocketVoiceState before, SocketVoiceState after)
            {
                var user = (iuser as SocketGuildUser);
                var guild = user?.Guild;

                if (guild == null)
                    return Task.CompletedTask;

                var botUserPerms = guild.CurrentUser.GuildPermissions;

                if (before.VoiceChannel == after.VoiceChannel)
                    return Task.CompletedTask;

                if (!voicePlusTextCache.Contains(guild.Id))
                    return Task.CompletedTask;

                var _ = Task.Run(async () =>
                {
                    try
                    {

                        if (!botUserPerms.ManageChannels || !botUserPerms.ManageRoles)
                        {
                            try
                            {
                                await guild.Owner.SendErrorAsync(
                                    "⚠️ I don't have **manage server** and/or **manage channels** permission," +
                                    $" so I cannot run `voice+text` on **{guild.Name}** server.").ConfigureAwait(false);
                            }
                            catch { }
                            using (var uow = DbHandler.UnitOfWork())
                            {
                                uow.GuildConfigs.For(guild.Id, set => set).VoicePlusTextEnabled = false;
                                voicePlusTextCache.TryRemove(guild.Id);
                                await uow.CompleteAsync().ConfigureAwait(false);
                            }
                            return;
                        }

                        var semaphore = guildLockObjects.GetOrAdd(guild.Id, (key) => new SemaphoreSlim(1, 1));

                        try
                        {
                            await semaphore.WaitAsync().ConfigureAwait(false);

                            var beforeVch = before.VoiceChannel;
                            if (beforeVch != null)
                            {
                                var beforeRoleName = GetRoleName(beforeVch);
                                var beforeRole = guild.Roles.FirstOrDefault(x => x.Name == beforeRoleName);
                                if (beforeRole != null)
                                    try
                                    {
                                        _log.Info("Removing role " + beforeRoleName + " from user " + user.Username);
                                        await user.RemoveRolesAsync(beforeRole).ConfigureAwait(false);
                                        await Task.Delay(200).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Warn(ex);
                                    }
                            }
                            var afterVch = after.VoiceChannel;
                            if (afterVch != null && guild.AFKChannel?.Id != afterVch.Id)
                            {
                                var roleName = GetRoleName(afterVch);
                                IRole roleToAdd = guild.Roles.FirstOrDefault(x => x.Name == roleName);
                                if (roleToAdd == null)
                                    roleToAdd = await guild.CreateRoleAsync(roleName).ConfigureAwait(false);

                                ITextChannel textChannel = guild.TextChannels
                                                            .Where(t => t.Name == GetChannelName(afterVch.Name).ToLowerInvariant())
                                                            .FirstOrDefault();
                                if (textChannel == null)
                                {
                                    var created = (await guild.CreateTextChannelAsync(GetChannelName(afterVch.Name).ToLowerInvariant()).ConfigureAwait(false));

                                    try { await guild.CurrentUser.AddRolesAsync(roleToAdd).ConfigureAwait(false); } catch { }
                                    await Task.Delay(50).ConfigureAwait(false);
                                    await created.AddPermissionOverwriteAsync(roleToAdd, new OverwritePermissions(
                                        readMessages: PermValue.Allow,
                                        sendMessages: PermValue.Allow))
                                            .ConfigureAwait(false);
                                    await Task.Delay(50).ConfigureAwait(false);
                                    await created.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(
                                        readMessages: PermValue.Deny,
                                        sendMessages: PermValue.Deny))
                                            .ConfigureAwait(false);
                                    await Task.Delay(50).ConfigureAwait(false);
                                }
                                _log.Warn("Adding role " + roleToAdd.Name + " to user " + user.Username);
                                await user.AddRolesAsync(roleToAdd).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                });
                return Task.CompletedTask;
            }

            private static string GetChannelName(string voiceName) =>
                channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

            private static string GetRoleName(IVoiceChannel ch) =>
                "nvoice-" + ch.Id;

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task VoicePlusText()
            {
                var guild = Context.Guild;

                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.ManageRoles || !botUser.GuildPermissions.ManageChannels)
                {
                    await Context.Channel.SendErrorAsync("I require atleast **manage roles** and **manage channels permissions** to enable this feature. `(preffered Administration permission)`");
                    return;
                }

                if (!botUser.GuildPermissions.Administrator)
                {
                    try
                    {
                        await Context.Channel.SendErrorAsync("⚠️ You are enabling/disabling this feature and **I do not have ADMINISTRATOR permissions**. " +
                      "`This may cause some issues, and you will have to clean up text channels yourself afterwards.`");
                    }
                    catch { }
                }
                try
                {
                    bool isEnabled;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var conf = uow.GuildConfigs.For(guild.Id, set => set);
                        isEnabled = conf.VoicePlusTextEnabled = !conf.VoicePlusTextEnabled;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    if (!isEnabled)
                    {
                        voicePlusTextCache.TryRemove(guild.Id);
                        foreach (var textChannel in (await guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(c => c.Name.EndsWith("-voice")))
                        {
                            try { await textChannel.DeleteAsync().ConfigureAwait(false); } catch { }
                            await Task.Delay(500).ConfigureAwait(false);
                        }

                        foreach (var role in guild.Roles.Where(c => c.Name.StartsWith("nvoice-")))
                        {
                            try { await role.DeleteAsync().ConfigureAwait(false); } catch { }
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                        await Context.Channel.SendConfirmAsync("ℹ️ Successfuly **removed** voice + text feature.").ConfigureAwait(false);
                        return;
                    }
                    voicePlusTextCache.Add(guild.Id);
                    await Context.Channel.SendConfirmAsync("🆗 Successfuly **enabled** voice + text feature.").ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            //[RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task CleanVPlusT()
            {
                var guild = Context.Guild;
                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.Administrator)
                {
                    await Context.Channel.SendErrorAsync("I need **Administrator permission** to do that.").ConfigureAwait(false);
                    return;
                }

                var textChannels = await guild.GetTextChannelsAsync().ConfigureAwait(false);
                var voiceChannels = await guild.GetVoiceChannelsAsync().ConfigureAwait(false);

                var boundTextChannels = textChannels.Where(c => c.Name.EndsWith("-voice"));
                var validTxtChannelNames = new HashSet<string>(voiceChannels.Select(c => GetChannelName(c.Name).ToLowerInvariant()));
                var invalidTxtChannels = boundTextChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                foreach (var c in invalidTxtChannels)
                {
                    try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500).ConfigureAwait(false);
                }
                
                var boundRoles = guild.Roles.Where(r => r.Name.StartsWith("nvoice-"));
                var validRoleNames = new HashSet<string>(voiceChannels.Select(c => GetRoleName(c).ToLowerInvariant()));
                var invalidRoles = boundRoles.Where(r => !validRoleNames.Contains(r.Name));

                foreach (var r in invalidRoles)
                {
                    try { await r.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500).ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync("Cleaned v+t.").ConfigureAwait(false);
            }
        }
    }
}