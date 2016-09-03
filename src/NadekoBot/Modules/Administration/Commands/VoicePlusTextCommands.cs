using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class VoicePlusTextCommands
        {
            Regex channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);
            //guildid/voiceplustextenabled
            private ConcurrentDictionary<ulong, bool> voicePlusTextCache;
            public VoicePlusTextCommands()
            {
                NadekoBot.Client.UserUpdated += UserUpdatedEventHandler;
                voicePlusTextCache = new ConcurrentDictionary<ulong, bool>();
            }

            private Task UserUpdatedEventHandler(IGuildUser before, IGuildUser after)
            {
                Task.Run(async () =>
                {
                    var guild = before.Guild ?? after.Guild;
                    var botUserPerms = guild.GetCurrentUser().GuildPermissions;
                    try
                    {
                        if (before.VoiceChannel == after.VoiceChannel) return;

                        bool isEnabled;
                        voicePlusTextCache.TryGetValue(guild.Id, out isEnabled);
                        if (!isEnabled)
                            return;

                        if (!botUserPerms.ManageChannels || !botUserPerms.ManageRoles)
                        {
                            try
                            {
                                await (await guild.GetOwnerAsync()).SendMessageAsync(
                                    "I don't have manage server and/or Manage Channels permission," +
                                    $" so I cannot run voice+text on **{guild.Name}** server.").ConfigureAwait(false);
                            }
                            catch { }
                            using (var uow = DbHandler.UnitOfWork())
                            {
                                uow.GuildConfigs.For(before.Guild.Id).VoicePlusTextEnabled = false;
                                voicePlusTextCache.TryUpdate(guild.Id, false, true);
                            }
                            return;
                        }


                        var beforeVch = before.VoiceChannel;
                        if (beforeVch != null)
                        {
                            var textChannel = guild.GetTextChannels().Where(t => t.Name == GetChannelName(beforeVch.Name)).FirstOrDefault();
                            if (textChannel != null)
                                await textChannel.AddPermissionOverwriteAsync(before,
                                    new OverwritePermissions(readMessages: PermValue.Deny,
                                                       sendMessages: PermValue.Deny)).ConfigureAwait(false);
                        }
                        var afterVch = after.VoiceChannel;
                        if (afterVch != null && guild.AFKChannelId != afterVch.Id)
                        {
                            var textChannel = guild.GetTextChannels()
                                                        .Where(t => t.Name ==  GetChannelName(afterVch.Name))
                                                        .FirstOrDefault();
                            if (textChannel == null)
                            {
                                textChannel = (await guild.CreateTextChannelAsync(GetChannelName(afterVch.Name)).ConfigureAwait(false));
                                await textChannel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                                    new OverwritePermissions(readMessages: PermValue.Deny,
                                                       sendMessages: PermValue.Deny)).ConfigureAwait(false);
                            }
                            await textChannel.AddPermissionOverwriteAsync(after,
                                new OverwritePermissions(readMessages: PermValue.Allow,
                                                        sendMessages: PermValue.Allow)).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
                return Task.CompletedTask;
            }

            private string GetChannelName(string voiceName) =>
                channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            [RequirePermission(GuildPermission.ManageChannels)]
            public async Task VoicePlusText(IUserMessage msg, [Remainder] string arg)
            {
                var channel = (ITextChannel)msg.Channel;
                var guild = channel.Guild;

                var botUser = guild.GetCurrentUser();
                if (!botUser.GuildPermissions.ManageRoles || !botUser.GuildPermissions.ManageChannels)
                {
                    await channel.SendMessageAsync(":anger: `I require manage roles and manage channels permissions to enable this feature.`");
                    return;
                }
                try
                {
                    bool isEnabled;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var conf = uow.GuildConfigs.For(guild.Id);
                        isEnabled = conf.VoicePlusTextEnabled = !conf.VoicePlusTextEnabled;
                    }
                    voicePlusTextCache.AddOrUpdate(guild.Id, isEnabled, (id, val) => isEnabled);
                    if (isEnabled)
                    {
                        foreach (var textChannel in guild.GetTextChannels().Where(c => c.Name.EndsWith("-voice")))
                        {
                            try { await textChannel.DeleteAsync().ConfigureAwait(false); } catch { }
                        }
                        await channel.SendMessageAsync("Successfuly removed voice + text feature.").ConfigureAwait(false);
                        return;
                    }
                    await channel.SendMessageAsync("Successfuly enabled voice + text feature.").ConfigureAwait(false);

                }
                catch (Exception ex)
                {
                    await channel.SendMessageAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageChannels)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task CleanVPlusT(IUserMessage msg, [Remainder] string arg)
            {
                var channel = (ITextChannel)msg.Channel;
                var guild = channel.Guild;
                if (!guild.GetCurrentUser().GuildPermissions.ManageChannels)
                {
                    await channel.SendMessageAsync("`I have insufficient permission to do that.`");
                    return;
                }

                var allTxtChannels = guild.GetTextChannels().Where(c => c.Name.EndsWith("-voice"));
                var validTxtChannelNames = guild.GetVoiceChannels().Select(c => GetChannelName(c.Name));

                var invalidTxtChannels = allTxtChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                foreach (var c in invalidTxtChannels)
                {
                    try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
                    await Task.Delay(500);
                }

                await channel.SendMessageAsync("`Done.`");
            }
        }
    }
}