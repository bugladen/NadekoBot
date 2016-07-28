using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Permissions.Commands
{
    internal class FilterInvitesCommand : DiscordCommand
    {
        private readonly Regex filterRegex = new Regex(@"(?:discord(?:\.gg|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))");


        public FilterInvitesCommand(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += async (sender, args) =>
            {
                if (args.Channel.IsPrivate || args.User.Id == NadekoBot.Client.CurrentUser.Id) return;
                try
                {
                    Classes.ServerPermissions serverPerms;
                    if (!IsChannelOrServerFiltering(args.Channel, out serverPerms)) return;

                    if (filterRegex.IsMatch(args.Message.RawText))
                    {
                        await args.Message.Delete().ConfigureAwait(false);
                        IncidentsHandler.Add(args.Server.Id, args.Channel.Id, $"User [{args.User.Name}/{args.User.Id}] posted " +
                                                             $"INVITE LINK in [{args.Channel.Name}/{args.Channel.Id}] channel.\n" +
                                                             $"`Full message:` {args.Message.Text}");
                        if (serverPerms.Verbose)
                            await args.Channel.SendMessage($"{args.User.Mention} Invite links are not " +
                                                           $"allowed on this channel.")
                                                           .ConfigureAwait(false);
                    }
                }
                catch { }
            };
        }

        private static bool IsChannelOrServerFiltering(Channel channel, out Classes.ServerPermissions serverPerms)
        {
            if (!PermissionsHandler.PermissionsDict.TryGetValue(channel.Server.Id, out serverPerms)) return false;

            if (serverPerms.Permissions.FilterInvites)
                return true;

            Classes.Permissions perms;
            return serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perms) && perms.FilterInvites;
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "chnlfilterinv")
                .Alias(Module.Prefix + "cfi")
                .Description("Enables or disables automatic deleting of invites on the channel." +
                             "If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once." +
                             $" | {Prefix}cfi enable #general-chat")
                .Parameter("bool")
                .Parameter("channel", ParameterType.Optional)
                .Do(async e =>
                {
                    try
                    {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        var chanStr = e.GetArg("channel");

                        if (chanStr?.ToLowerInvariant().Trim() != "all")
                        {

                            var chan = string.IsNullOrWhiteSpace(chanStr)
                                ? e.Channel
                                : PermissionHelper.ValidateChannel(e.Server, chanStr);
                            await PermissionsHandler.SetChannelFilterInvitesPermission(chan, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for **{chan.Name}** channel.")
                                            .ConfigureAwait(false);
                            return;
                        }
                        //all channels

                        foreach (var curChannel in e.Server.TextChannels)
                        {
                            await PermissionsHandler.SetChannelFilterInvitesPermission(curChannel, state).ConfigureAwait(false);
                        }
                        await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for **ALL** channels.")
                                       .ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}")
                                       .ConfigureAwait(false);
                    }
                });

            cgb.CreateCommand(Module.Prefix + "srvrfilterinv")
                .Alias(Module.Prefix + "sfi")
                .Description($"Enables or disables automatic deleting of invites on the server. | {Prefix}sfi disable")
                .Parameter("bool")
                .Do(async e =>
                {
                    try
                    {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        await PermissionsHandler.SetServerFilterInvitesPermission(e.Server, state).ConfigureAwait(false);
                        await e.Channel.SendMessage($"Invite Filter has been **{(state ? "enabled" : "disabled")}** for this server.")
                                       .ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}").ConfigureAwait(false);
                    }
                });
        }
    }
}
