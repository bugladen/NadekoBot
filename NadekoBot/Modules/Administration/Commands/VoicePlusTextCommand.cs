using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChPermOverride = Discord.ChannelPermissionOverrides;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class VoicePlusTextCommand : DiscordCommand
    {
        Regex channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);
        public VoicePlusTextCommand(DiscordModule module) : base(module)
        {
            // changing servers may cause bugs
            NadekoBot.Client.UserUpdated += async (sender, e) =>
            {
                try
                {
                    if (e.Server == null)
                        return;
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    if (e.Before.VoiceChannel == e.After.VoiceChannel) return;
                    if (!config.VoicePlusTextEnabled)
                        return;
                    var serverPerms = e.Server.GetUser(NadekoBot.Client.CurrentUser.Id)?.ServerPermissions;
                    if (serverPerms == null)
                        return;
                    if (!serverPerms.Value.ManageChannels || !serverPerms.Value.ManageRoles)
                    {

                        try
                        {
                            await e.Server.Owner.SendMessage(
                                "I don't have manage server and/or Manage Channels permission," +
                                $" so I cannot run voice+text on **{e.Server.Name}** server.").ConfigureAwait(false);
                        }
                        catch { } // meh
                        config.VoicePlusTextEnabled = false;
                        return;
                    }


                    var beforeVch = e.Before.VoiceChannel;
                    if (beforeVch != null)
                    {
                        var textChannel =
                            e.Server.FindChannels(GetChannelName(beforeVch.Name), ChannelType.Text).FirstOrDefault();
                        if (textChannel != null)
                            await textChannel.AddPermissionsRule(e.Before,
                                new ChPermOverride(readMessages: PermValue.Deny,
                                                   sendMessages: PermValue.Deny)).ConfigureAwait(false);
                    }
                    var afterVch = e.After.VoiceChannel;
                    if (afterVch != null && e.Server.AFKChannel != afterVch)
                    {
                        var textChannel = e.Server.FindChannels(
                                                    GetChannelName(afterVch.Name),
                                                    ChannelType.Text)
                                                    .FirstOrDefault();
                        if (textChannel == null)
                        {
                            textChannel = (await e.Server.CreateChannel(GetChannelName(afterVch.Name), ChannelType.Text).ConfigureAwait(false));
                            await textChannel.AddPermissionsRule(e.Server.EveryoneRole,
                                new ChPermOverride(readMessages: PermValue.Deny,
                                                   sendMessages: PermValue.Deny)).ConfigureAwait(false);
                        }
                        await textChannel.AddPermissionsRule(e.After,
                            new ChPermOverride(readMessages: PermValue.Allow,
                                               sendMessages: PermValue.Allow)).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };
        }

        private string GetChannelName(string voiceName) =>
            channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "cleanv+t")
                .Alias(Module.Prefix + "cv+t")
                .Description($"Deletes all text channels ending in `-voice` for which voicechannels are not found. **Use at your own risk.** | `{Prefix}cleanv+t`")
                .AddCheck(SimpleCheckers.CanManageRoles)
                .AddCheck(SimpleCheckers.ManageChannels())
                .Do(async e =>
                {
                    if (!e.Server.CurrentUser.ServerPermissions.ManageChannels)
                    {
                        await e.Channel.SendMessage("`I have insufficient permission to do that.`");
                        return;
                    }

                    var allTxtChannels = e.Server.TextChannels.Where(c => c.Name.EndsWith("-voice"));
                    var validTxtChannelNames = e.Server.VoiceChannels.Select(c => GetChannelName(c.Name));

                    var invalidTxtChannels = allTxtChannels.Where(c => !validTxtChannelNames.Contains(c.Name));

                    foreach (var c in invalidTxtChannels)
                    {
                        try
                        {
                            await c.Delete();
                        }
                        catch { }
                        await Task.Delay(500);
                    }

                    await e.Channel.SendMessage("`Done.`");
                });

            cgb.CreateCommand(Module.Prefix + "voice+text")
                .Alias(Module.Prefix + "v+t")
                .Description("Creates a text channel for each voice channel only users in that voice channel can see." +
                             $"If you are server owner, keep in mind you will see them all the time regardless. | `{Prefix}voice+text`")
                .AddCheck(SimpleCheckers.ManageChannels())
                .AddCheck(SimpleCheckers.CanManageRoles)
                .Do(async e =>
                {
                    try
                    {
                        var config = SpecificConfigurations.Default.Of(e.Server.Id);
                        if (config.VoicePlusTextEnabled == true)
                        {
                            config.VoicePlusTextEnabled = false;
                            foreach (var textChannel in e.Server.TextChannels.Where(c => c.Name.EndsWith("-voice")))
                            {
                                try
                                {
                                    await textChannel.Delete().ConfigureAwait(false);
                                }
                                catch
                                {
                                    await e.Channel.SendMessage(
                                            ":anger: Error: Most likely i don't have permissions to do this.")
                                                .ConfigureAwait(false);
                                    return;
                                }
                            }
                            await e.Channel.SendMessage("Successfuly removed voice + text feature.").ConfigureAwait(false);
                            return;
                        }
                        config.VoicePlusTextEnabled = true;
                        await e.Channel.SendMessage("Successfuly enabled voice + text feature. " +
                                                    "**Make sure the bot has manage roles and manage channels permissions**")
                                                    .ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage(ex.ToString()).ConfigureAwait(false);
                    }
                });
        }
    }
}
