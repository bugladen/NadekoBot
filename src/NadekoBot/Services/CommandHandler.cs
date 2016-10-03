using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NLog;
using System.Diagnostics;
using Discord.Commands;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NadekoBot.Modules.Permissions;
using Microsoft.Data.Sqlite;
using Discord.Net;
using NadekoBot.Extensions;

namespace NadekoBot.Services
{
    public class CommandHandler
    {
        private ShardedDiscordClient  _client;
        private CommandService _commandService;
        private Logger _log;

        public event EventHandler<CommandExecutedEventArgs> CommandExecuted = delegate { };

        public CommandHandler(ShardedDiscordClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _log = LogManager.GetCurrentClassLogger();

            _client.MessageReceived += MessageReceivedHandler;
        }

        private async Task MessageReceivedHandler(IMessage msg)
        {
            var usrMsg = msg as IUserMessage;
            if (usrMsg == null)
                return;

            if (usrMsg.Author.IsBot) //no bots
                return;

            var guild = (msg.Channel as ITextChannel)?.Guild;

            BlacklistItem blacklistedItem;
            if ((blacklistedItem = Permissions.BlacklistCommands.BlacklistedItems.FirstOrDefault(bi =>
                 (bi.Type == BlacklistItem.BlacklistType.Server && bi.ItemId == guild?.Id) ||
                 (bi.Type == BlacklistItem.BlacklistType.Channel && bi.ItemId == msg.Channel.Id) ||
                 (bi.Type == BlacklistItem.BlacklistType.User && bi.ItemId == usrMsg.Author.Id))) != null)
            {
                _log.Warn("Attempt was made to run a command by a blacklisted {0}, id: {1}", blacklistedItem.Type, blacklistedItem.ItemId);
                return;
            }

            if (guild != null)
            {
                if (Permissions.FilterCommands.InviteFilteringChannels.Contains(usrMsg.Channel.Id) ||
                    Permissions.FilterCommands.InviteFilteringServers.Contains(guild.Id))
                {
                    if (usrMsg.Content.IsDiscordInvite())
                    {
                        try
                        {
                            await usrMsg.DeleteAsync().ConfigureAwait(false);
                            return;
                        }
                        catch (HttpException ex)
                        {
                            _log.Warn("I do not have permission to filter invites in channel with id " + usrMsg.Channel.Id, ex);
                        }
                    }
                }
                var filteredWords = Permissions.FilterCommands.FilteredWordsForChannel(usrMsg.Channel.Id, guild.Id).Concat(Permissions.FilterCommands.FilteredWordsForServer(guild.Id));
                var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
                if (filteredWords.Any())
                {
                    try
                    {
                        await usrMsg.DeleteAsync().ConfigureAwait(false);
                        return;
                    }
                    catch (HttpException ex)
                    {
                        _log.Warn("I do not have permission to filter words in channel with id " + usrMsg.Channel.Id, ex);
                    }
                }
            }

            try
            {
                bool verbose = false;
                Permission rootPerm = null;
                string permRole = "";
                if (guild != null)
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.GuildConfigs.PermissionsFor(guild.Id);
                        verbose = config.VerbosePermissions;
                        rootPerm = config.RootPermission;
                        permRole = config.PermissionRole.Trim().ToLowerInvariant();
                    }

                    
                }

                var throwaway = Task.Run(async () =>
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    try
                    {
                        var t = await ExecuteCommand(usrMsg, usrMsg.Content, guild, usrMsg.Author, rootPerm, permRole, MultiMatchHandling.Best);
                        var command = t.Item1;
                        var result = t.Item2;
                        sw.Stop();
                        var channel = (usrMsg.Channel as ITextChannel);
                        if (result.IsSuccess)
                        {
                            CommandExecuted(this, new CommandExecutedEventArgs(usrMsg, command));
                            _log.Info("Command Executed after {4}s\n\t" +
                                      "User: {0}\n\t" +
                                      "Server: {1}\n\t" +
                                      "Channel: {2}\n\t" +
                                      "Message: {3}",
                                      usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                                      (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                                      (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                                      usrMsg.Content, // {3}
                                      sw.Elapsed.TotalSeconds // {4}
                                      );
                        }
                        else if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        {
                            _log.Warn("Command Errored after {5}s\n\t" +
                                      "User: {0}\n\t" +
                                      "Server: {1}\n\t" +
                                      "Channel: {2}\n\t" +
                                      "Message: {3}\n\t" +
                                      "Error: {4}",
                                      usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                                      (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                                      (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                                      usrMsg.Content,// {3}
                                      result.ErrorReason, // {4}
                                      sw.Elapsed.TotalSeconds // {5}
                                      );
                            if (guild != null && command != null && result.Error == CommandError.Exception)
                            {
                                if (verbose)
                                    await msg.Channel.SendMessageAsync(":warning: " + result.ErrorReason).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, "Error in CommandHandler");
                        if (ex.InnerException != null)
                            _log.Warn(ex.InnerException, "Inner Exception of the error in CommandHandler");
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in the outter scope of the commandhandler.");
                if (ex.InnerException != null)
                    _log.Error(ex.InnerException, "Inner exception: ");
            }
        }

        public async Task<Tuple<Command,IResult>> ExecuteCommand(IUserMessage message, string input, IGuild guild, IUser user, Permission rootPerm, string permRole, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Best) {
            var searchResult = _commandService.Search(message, input);
            if (!searchResult.IsSuccess)
                return new Tuple<Command, IResult>(null, searchResult);

            var commands = searchResult.Commands;
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                var preconditionResult = await commands[i].CheckPreconditions(message);
                if (!preconditionResult.IsSuccess)
                {
                    if (commands.Count == 1)
                        return new Tuple<Command, IResult>(null, searchResult);
                    else
                        continue;
                }

                var parseResult = await commands[i].Parse(message, searchResult, preconditionResult);
                if (!parseResult.IsSuccess)
                {
                    if (parseResult.Error == CommandError.MultipleMatches)
                    {
                        TypeReaderValue[] argList, paramList;
                        switch (multiMatchHandling)
                        {
                            case MultiMatchHandling.Best:
                                argList = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToArray();
                                paramList = parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToArray();
                                parseResult = ParseResult.FromSuccess(argList, paramList);
                                break;
                        }
                    }

                    if (!parseResult.IsSuccess)
                    {
                        if (commands.Count == 1)
                            return new Tuple<Command, IResult>(null, parseResult);
                        else
                            continue;
                    }
                }
                var cmd = commands[i];
                //check permissions
                if (guild != null)
                {
                    int index;
                    if (!rootPerm.AsEnumerable().CheckPermissions(message, cmd.Name, cmd.Module.Name, out index))
                    {
                        var returnMsg = $"Permission number #{index + 1} **{rootPerm.GetAt(index).GetCommand()}** is preventing this action.";
                        return new Tuple<Command, IResult>(cmd, SearchResult.FromError(CommandError.Exception, returnMsg));
                    }


                    if (cmd.Module.Source.Name == typeof(Permissions).Name) //permissions, you must have special role
                    {
                        if (!((IGuildUser)user).Roles.Any(r => r.Name.Trim().ToLowerInvariant() == permRole))
                        {
                            return new Tuple<Command, IResult>(cmd, SearchResult.FromError(CommandError.Exception, $"You need a **{permRole}** role in order to use permission commands."));
                        }
                    }
                }

                return new Tuple<Command, IResult>(commands[i], await commands[i].Execute(message, parseResult));
            }

            return new Tuple<Command, IResult>(null, SearchResult.FromError(CommandError.UnknownCommand, "This input does not match any overload."));
        }
    }

    public class CommandExecutedEventArgs
    {
        public Command Command { get; }
        public IUserMessage Message { get; }

        public CommandExecutedEventArgs(IUserMessage msg, Command cmd)
        {
            Message = msg;
            Command = cmd;
        }
    }
}
