using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Linq;

namespace NadekoBot.Modules.Permissions.Commands
{
    internal class FilterWords : DiscordCommand
    {
        public FilterWords(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += async (sender, args) =>
            {
                if (args.Channel.IsPrivate || args.User.Id == NadekoBot.Client.CurrentUser.Id) return;
                try
                {
                    Classes.ServerPermissions serverPerms;
                    if (!IsChannelOrServerFiltering(args.Channel, out serverPerms)) return;

                    var wordsInMessage = args.Message.RawText.ToLowerInvariant().Split(' ');
                    if (serverPerms.Words.Any(w => wordsInMessage.Contains(w)))
                    {
                        await args.Message.Delete().ConfigureAwait(false);
                        IncidentsHandler.Add(args.Server.Id, args.Channel.Id, $"User [{args.User.Name}/{args.User.Id}] posted " +
                                                             $"BANNED WORD in [{args.Channel.Name}/{args.Channel.Id}] channel.\n" +
                                                             $"`Full message:` {args.Message.Text}");
                        if (serverPerms.Verbose)
                            await args.Channel.SendMessage($"{args.User.Mention} One or more of the words you used " +
                                                           $"in that sentence are not allowed here.")
                                                           .ConfigureAwait(false);
                    }
                }
                catch { }
            };
        }

        private static bool IsChannelOrServerFiltering(Channel channel, out Classes.ServerPermissions serverPerms)
        {
            if (!PermissionsHandler.PermissionsDict.TryGetValue(channel.Server.Id, out serverPerms)) return false;

            if (serverPerms.Permissions.FilterWords)
                return true;

            Classes.Permissions perms;
            return serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perms) && perms.FilterWords;
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "chnlfilterwords")
                .Alias(Module.Prefix + "cfw")
                .Description("Enables or disables automatic deleting of messages containing banned words on the channel." +
                             "If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once." +
                             $" | {Prefix}cfw enable #general-chat")
                .Parameter("bool")
                .Parameter("channel", ParameterType.Optional)
                .Do(async e =>
                {
                    try
                    {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        var chanStr = e.GetArg("channel")?.ToLowerInvariant().Trim();

                        if (chanStr != "all")
                        {
                            var chan = string.IsNullOrWhiteSpace(chanStr)
                                ? e.Channel
                                : PermissionHelper.ValidateChannel(e.Server, chanStr);
                            await PermissionsHandler.SetChannelWordPermission(chan, state).ConfigureAwait(false);
                            await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** for **{chan.Name}** channel.").ConfigureAwait(false);
                            return;
                        }
                        //all channels

                        foreach (var curChannel in e.Server.TextChannels)
                        {
                            await PermissionsHandler.SetChannelWordPermission(curChannel, state).ConfigureAwait(false);
                        }
                        await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** for **ALL** channels.").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}").ConfigureAwait(false);
                    }
                });

            cgb.CreateCommand(Module.Prefix + "addfilterword")
               .Alias(Module.Prefix + "afw")
               .Description("Adds a new word to the list of filtered words" +
                            $" | {Prefix}afw poop")
               .Parameter("word", ParameterType.Unparsed)
               .Do(async e =>
               {
                   try
                   {
                       var word = e.GetArg("word");
                       if (string.IsNullOrWhiteSpace(word))
                           return;
                       await PermissionsHandler.AddFilteredWord(e.Server, word.ToLowerInvariant().Trim()).ConfigureAwait(false);
                       await e.Channel.SendMessage($"Successfully added new filtered word.").ConfigureAwait(false);

                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}").ConfigureAwait(false);
                   }
               });

            cgb.CreateCommand(Module.Prefix + "rmvfilterword")
               .Alias(Module.Prefix + "rfw")
               .Description("Removes the word from the list of filtered words" +
                            $" | {Prefix}rw poop")
               .Parameter("word", ParameterType.Unparsed)
               .Do(async e =>
               {
                   try
                   {
                       var word = e.GetArg("word");
                       if (string.IsNullOrWhiteSpace(word))
                           return;
                       await PermissionsHandler.RemoveFilteredWord(e.Server, word.ToLowerInvariant().Trim()).ConfigureAwait(false);
                       await e.Channel.SendMessage($"Successfully removed filtered word.").ConfigureAwait(false);

                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}").ConfigureAwait(false);
                   }
               });

            cgb.CreateCommand(Module.Prefix + "lstfilterwords")
               .Alias(Module.Prefix + "lfw")
               .Description("Shows a list of filtered words" +
                            $" | {Prefix}lfw")
               .Do(async e =>
               {
                   try
                   {
                       Classes.ServerPermissions serverPerms;
                       if (!PermissionsHandler.PermissionsDict.TryGetValue(e.Server.Id, out serverPerms))
                           return;
                       await e.Channel.SendMessage($"There are `{serverPerms.Words.Count}` filtered words.\n" +
                           string.Join("\n", serverPerms.Words)).ConfigureAwait(false);
                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}").ConfigureAwait(false);
                   }
               });

            cgb.CreateCommand(Module.Prefix + "srvrfilterwords")
                .Alias(Module.Prefix + "sfw")
                .Description($"Enables or disables automatic deleting of messages containing forbidden words on the server. | {Prefix}sfw disable")
                .Parameter("bool")
                .Do(async e =>
                {
                    try
                    {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        await PermissionsHandler.SetServerWordPermission(e.Server, state).ConfigureAwait(false);
                        await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** on this server.")
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
