using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.Permissions;
using NadekoBot.Commands;
using System;
using System.Linq;
using ChPermOverride = Discord.ChannelPermissionOverrides;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class VoicePlusTextCommand : DiscordCommand
    {

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
                                $" so I cannot run voice+text on **{e.Server.Name}** server.");
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
                                                   sendMessages: PermValue.Deny));
                    }
                    var afterVch = e.After.VoiceChannel;
                    if (afterVch != null)
                    {
                        var textChannel = e.Server.FindChannels(
                                                    GetChannelName(afterVch.Name),
                                                    ChannelType.Text)
                                                    .FirstOrDefault();
                        if (textChannel == null)
                        {
                            textChannel = (await e.Server.CreateChannel(GetChannelName(afterVch.Name), ChannelType.Text));
                            await textChannel.AddPermissionsRule(e.Server.EveryoneRole,
                                new ChPermOverride(readMessages: PermValue.Deny,
                                                   sendMessages: PermValue.Deny));
                        }
                        await textChannel.AddPermissionsRule(e.After,
                            new ChPermOverride(readMessages: PermValue.Allow,
                                               sendMessages: PermValue.Allow));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };
        }

        private string GetChannelName(string voiceName) =>
            voiceName.Replace(" ", "-").Trim() + "-voice";

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "v+t")
                .Alias(Module.Prefix + "voice+text")
                .Description("Creates a text channel for each voice channel only users in that voice channel can see." +
                             "If you are server owner, keep in mind you will see them all the time regardless.")
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
                                    await textChannel.Delete();
                                }
                                catch
                                {
                                    await
                                        e.Channel.SendMessage(
                                            ":anger: Error: Most likely i don't have permissions to do this.");
                                    return;
                                }
                            }
                            await e.Channel.SendMessage("Successfuly removed voice + text feature.");
                            return;
                        }
                        config.VoicePlusTextEnabled = true;
                        await e.Channel.SendMessage("Successfuly enabled voice + text feature. " +
                                                    "**Make sure the bot has manage roles and manage channels permissions**");

                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage(ex.ToString());
                    }
                });
        }
    }
}
