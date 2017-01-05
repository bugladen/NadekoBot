using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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

            private static async void UserUpdatedEventHandler(SocketUser iuser, SocketVoiceState before, SocketVoiceState after)
            {
                var user = (iuser as SocketGuildUser);
                var guild = user?.Guild;

                if (guild == null)
                    return;

                try
                {
                    var botUserPerms = guild.CurrentUser.GuildPermissions;

                    if (before.VoiceChannel == after.VoiceChannel) return;

                    if (!voicePlusTextCache.Contains(guild.Id))
                        return;

                    if (!botUserPerms.ManageChannels || !botUserPerms.ManageRoles)
                    {
                        try
                        {
                            await (await guild.GetOwnerAsync()).SendErrorAsync(
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


                    var beforeVch = before.VoiceChannel;
                    if (beforeVch != null)
                    {
                        var textChannel = (await guild.GetTextChannelsAsync()).Where(t => t.Name == GetChannelName(beforeVch.Name).ToLowerInvariant()).FirstOrDefault();
                        if (textChannel != null)
                            await textChannel.AddPermissionOverwriteAsync(user,
                                new OverwritePermissions(readMessages: PermValue.Deny,
                                                   sendMessages: PermValue.Deny)).ConfigureAwait(false);
                    }
                    var afterVch = after.VoiceChannel;
                    if (afterVch != null && guild.AFKChannelId != afterVch.Id)
                    {
                        var textChannel = (await guild.GetTextChannelsAsync())
                                                    .Where(t => t.Name == GetChannelName(afterVch.Name).ToLowerInvariant())
                                                    .FirstOrDefault();
                        if (textChannel == null)
                        {
                            textChannel = (await guild.CreateTextChannelAsync(GetChannelName(afterVch.Name).ToLowerInvariant()).ConfigureAwait(false));
                            await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                                new OverwritePermissions(readMessages: PermValue.Deny,
                                                   sendMessages: PermValue.Deny)).ConfigureAwait(false);
                        }
                        await textChannel.AddPermissionOverwriteAsync(user,
                            new OverwritePermissions(readMessages: PermValue.Allow,
                                                    sendMessages: PermValue.Allow)).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            private static string GetChannelName(string voiceName) =>
                channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

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
                        await Context.Channel.SendErrorAsync("⚠️ You are enabling this feature and **I do not have ADMINISTRATOR permissions**. " +
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
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task CleanVPlusT()
            {
                var guild = Context.Guild;
                var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                if (!botUser.GuildPermissions.Administrator)
                {
                    await Context.Channel.SendErrorAsync("I need **Administrator permission** to do that.").ConfigureAwait(false);
                    return;
                }

                var allTxtChannels = (await guild.GetTextChannelsAsync()).Where(c => c.Name.EndsWith("-voice"));
                var validTxtChannelNames = (await guild.GetVoiceChannelsAsync()).Select(c => GetChannelName(c.Name).ToLowerInvariant());

                var invalidTxtChannels = allTxtChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                foreach (var c in invalidTxtChannels)
                {
                    try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500);
                }

                await Context.Channel.SendConfirmAsync("Cleaned v+t.").ConfigureAwait(false);
            }
        }
    }
}