using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.Permissions;
using NadekoBot.Modules;
using ServerPermissions = NadekoBot.Classes.Permissions.ServerPermissions;

namespace NadekoBot.Commands {
    internal class FilterInvitesCommand : DiscordCommand {
        private readonly Regex filterRegex = new Regex(@"(?:discord(?:\.gg|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))");


        public FilterInvitesCommand(DiscordModule module) : base(module) {
            NadekoBot.Client.MessageReceived += async (sender, args) => {
                if (args.Channel.IsPrivate || args.User.Id == NadekoBot.Client.CurrentUser.Id) return;
                try {
                    ServerPermissions serverPerms;
                    if (!IsChannelOrServerFiltering(args.Channel, out serverPerms)) return;

                    if (filterRegex.IsMatch(args.Message.RawText)) {
                        await args.Message.Delete();
                        IncidentsHandler.Add(args.Server.Id, $"User [{args.User.Name}/{args.User.Id}] posted " +
                                                             $"INVITE LINK in [{args.Channel.Name}/{args.Channel.Id}] channel. " +
                                                             $"Full message: [[{args.Message.Text}]]");
                        if (serverPerms.Verbose)
                            await args.Channel.SendMessage($"{args.User.Mention} Invite links are not " +
                                                           $"allowed on this channel.");
                    }
                } catch { }
            };
        }

        private static bool IsChannelOrServerFiltering(Channel channel, out ServerPermissions serverPerms) {
            if (!PermissionsHandler.PermissionsDict.TryGetValue(channel.Server.Id, out serverPerms)) return false;

            if (serverPerms.Permissions.FilterInvites)
                return true;

            Permissions perms;
            return serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perms) && perms.FilterInvites;
        }

        internal override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(Module.Prefix + "cfi")
                .Alias(Module.Prefix + "channelfilterinvites")
                .Description("Enables or disables automatic deleting of invites on the channel." +
                             "If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once." +
                             "\n**Usage**: ;cfi enable #general-chat")
                .Parameter("bool")
                .Parameter("channel", ParameterType.Optional)
                .Do(async e => {
                    try {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        var chanStr = e.GetArg("channel");

                        if (chanStr?.ToLowerInvariant().Trim() != "all") {

                            var chan = string.IsNullOrWhiteSpace(chanStr)
                                ? e.Channel
                                : PermissionHelper.ValidateChannel(e.Server, chanStr);
                            PermissionsHandler.SetChannelFilterInvitesPermission(chan, state);
                            await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for **{chan.Name}** channel.");
                            return;
                        }
                        //all channels

                        foreach (var curChannel in e.Server.TextChannels) {
                            PermissionsHandler.SetChannelFilterInvitesPermission(curChannel, state);
                        }
                        await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for **ALL** channels.");

                    } catch (Exception ex) {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                    }
                });

            cgb.CreateCommand(Module.Prefix + "sfi")
                .Alias(Module.Prefix + "serverfilterinvites")
                .Description("Enables or disables automatic deleting of invites on the server.\n**Usage**: ;sfi disable")
                .Parameter("bool")
                .Do(async e => {
                    try {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        PermissionsHandler.SetServerFilterInvitesPermission(e.Server, state);
                        await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for this server.");

                    } catch (Exception ex) {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                    }
                });
        }
    }
}
