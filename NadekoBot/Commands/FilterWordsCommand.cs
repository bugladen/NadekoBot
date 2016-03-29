using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Classes.Permissions;
using NadekoBot.Modules;
using System;
using System.Linq;
using ServerPermissions = NadekoBot.Classes.Permissions.ServerPermissions;

namespace NadekoBot.Commands
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
                    ServerPermissions serverPerms;
                    if (!IsChannelOrServerFiltering(args.Channel, out serverPerms)) return;

                    var wordsInMessage = args.Message.RawText.ToLowerInvariant().Split(' ');
                    if (serverPerms.Words.Any(w => wordsInMessage.Contains(w)))
                    {
                        await args.Message.Delete();
                        IncidentsHandler.Add(args.Server.Id, $"User [{args.User.Name}/{args.User.Id}] posted " +
                                                             $"BANNED WORD in [{args.Channel.Name}/{args.Channel.Id}] channel. " +
                                                             $"Full message: [[{args.Message.Text}]]");
                        if (serverPerms.Verbose)
                            await args.Channel.SendMessage($"{args.User.Mention} One or more of the words you used " +
                                                           $"in that sentence are not allowed here.");
                    }
                }
                catch { }
            };
        }

        private static bool IsChannelOrServerFiltering(Channel channel, out ServerPermissions serverPerms)
        {
            if (!PermissionsHandler.PermissionsDict.TryGetValue(channel.Server.Id, out serverPerms)) return false;

            if (serverPerms.Permissions.FilterWords)
                return true;

            Permissions perms;
            return serverPerms.ChannelPermissions.TryGetValue(channel.Id, out perms) && perms.FilterWords;
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "cfw")
                .Alias(Module.Prefix + "channelfilterwords")
                .Description("Enables or disables automatic deleting of messages containing banned words on the channel." +
                             "If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once." +
                             "\n**Usage**: ;cfi enable #general-chat")
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
                            PermissionsHandler.SetChannelWordPermission(chan, state);
                            await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** for **{chan.Name}** channel.");
                            return;
                        }
                        //all channels

                        foreach (var curChannel in e.Server.TextChannels)
                        {
                            PermissionsHandler.SetChannelWordPermission(curChannel, state);
                        }
                        await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** for **ALL** channels.");
                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                    }
                });

            cgb.CreateCommand(Module.Prefix + "afw")
               .Alias(Module.Prefix + "addfilteredword")
               .Description("Adds a new word to the list of filtered words" +
                            "\n**Usage**: ;aw poop")
               .Parameter("word", ParameterType.Unparsed)
               .Do(async e =>
               {
                   try
                   {
                       var word = e.GetArg("word");
                       if (string.IsNullOrWhiteSpace(word))
                           return;
                       PermissionsHandler.AddFilteredWord(e.Server, word.ToLowerInvariant().Trim());
                       await e.Channel.SendMessage($"Successfully added new filtered word.");

                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                   }
               });

            cgb.CreateCommand(Module.Prefix + "rfw")
               .Alias(Module.Prefix + "removefilteredword")
               .Description("Removes the word from the list of filtered words" +
                            "\n**Usage**: ;rw poop")
               .Parameter("word", ParameterType.Unparsed)
               .Do(async e =>
               {
                   try
                   {
                       var word = e.GetArg("word");
                       if (string.IsNullOrWhiteSpace(word))
                           return;
                       PermissionsHandler.RemoveFilteredWord(e.Server, word.ToLowerInvariant().Trim());
                       await e.Channel.SendMessage($"Successfully removed filtered word.");

                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                   }
               });

            cgb.CreateCommand(Module.Prefix + "lfw")
               .Alias(Module.Prefix + "listfilteredwords")
               .Description("Shows a list of filtered words" +
                            "\n**Usage**: ;lfw")
               .Do(async e =>
               {
                   try
                   {
                       ServerPermissions serverPerms;
                       if (!PermissionsHandler.PermissionsDict.TryGetValue(e.Server.Id, out serverPerms))
                           return;
                       await e.Channel.SendMessage($"There are `{serverPerms.Words.Count}` filtered words.\n" +
                           string.Join("\n", serverPerms.Words));
                   }
                   catch (Exception ex)
                   {
                       await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                   }
               });

            cgb.CreateCommand(Module.Prefix + "sfw")
                .Alias(Module.Prefix + "serverfilterwords")
                .Description("Enables or disables automatic deleting of messages containing forbidden words on the server.\n**Usage**: ;sfi disable")
                .Parameter("bool")
                .Do(async e =>
                {
                    try
                    {
                        var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                        PermissionsHandler.SetServerWordPermission(e.Server, state);
                        await e.Channel.SendMessage($"Word filtering has been **{(state ? "enabled" : "disabled")}** on this server.");

                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage($"💢 Error: {ex.Message}");
                    }
                });
        }
    }
}
