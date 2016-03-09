using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Classes.Permissions;
using ChPermOverride = Discord.ChannelPermissionOverrides;

namespace NadekoBot.Commands {
    /// <summary>
    /// This is an ingenious idea by @Googie2149 a few months back.
    /// He never got around to implementing it, so i grew impatient
    /// and did it myself. Googie is cool guy and a creator of RoboNitori
    /// You can check out his server here: https://discord.gg/0ZgChoTkuxAzARfF
    /// sowwy googie ;(
    /// </summary>
    internal class VoicePlusTextCommand : IDiscordCommand {
        public static readonly HashSet<ulong> Subscribers = new HashSet<ulong>();

        public VoicePlusTextCommand() {
            NadekoBot.Client.UserUpdated += async (sender, e) => {
                try {
                    if (e.Before.VoiceChannel == e.After.VoiceChannel) return;

                    var beforeVch = e.Before.VoiceChannel;
                    if (beforeVch != null) {
                        var textChannel =
                            e.Server.FindChannels(beforeVch.Name + "-voice", ChannelType.Text).FirstOrDefault();
                        if (textChannel == null)
                            return;
                        await textChannel.AddPermissionsRule(e.Before,
                            new ChPermOverride(readMessages: PermValue.Deny,
                                               sendMessages: PermValue.Deny));
                    }
                    var afterVch = e.After.VoiceChannel;
                    if (afterVch != null) {
                        var textChannel =
                            e.Server.FindChannels(afterVch.Name + "-voice", ChannelType.Text).FirstOrDefault() ??
                                     (await e.Server.CreateChannel(afterVch.Name + "-voice", ChannelType.Text));
                        if (textChannel == null)
                            return;
                        await textChannel.AddPermissionsRule(e.After,
                            new ChPermOverride(readMessages: PermValue.Allow,
                                               sendMessages: PermValue.Allow));
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            };
        }

        public void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(".v+t")
                .Alias(".voice+text")
                .Description("Creates a text channel for each voice channel only users in that voice channel can see.")
                .AddCheck(SimpleCheckers.ManageChannels())
                .AddCheck(SimpleCheckers.CanManageRoles)
                .Do(async e => {
                    if (Subscribers.Contains(e.Server.Id)) {
                        Subscribers.Remove(e.Server.Id);
                        foreach (var textChannel in e.Server.TextChannels.Where(c => c.Name.EndsWith("-voice"))) {
                            var deleteTask = textChannel?.Delete();
                            try {
                                if (deleteTask != null)
                                    await deleteTask;
                            } catch {
                                await e.Channel.SendMessage(":anger: Error: Most likely i don't have permissions to do this.");
                                return;
                            }
                        }
                        await e.Channel.SendMessage("Successfuly removed voice + text feature.");
                        return;
                    }
                    Subscribers.Add(e.Server.Id);
                    await e.Channel.SendMessage("Successfuly enabled voice + text feature. " +
                                                "**Make sure the bot has manage roles and manage channels permissions**");
                });
        }
    }
}
