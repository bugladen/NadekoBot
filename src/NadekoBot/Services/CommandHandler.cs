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

namespace NadekoBot.Services
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private Logger _log;

        public event EventHandler<CommandExecutedEventArgs> CommandExecuted = delegate { };

        public CommandHandler(DiscordSocketClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _log = LogManager.GetCurrentClassLogger();

            _client.MessageReceived += MessageReceivedHandler;
        }

        private Task MessageReceivedHandler(IMessage msg)
        {
            var usrMsg = msg as IUserMessage;
            if (usrMsg == null)
                return Task.CompletedTask;

            var guild = (msg.Channel as ITextChannel)?.Guild;

            var throwaway = Task.Run(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    bool verbose;
                    Permission rootPerm;
                    string permRole;
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var config = uow.GuildConfigs.PermissionsFor(guild.Id);
                        verbose = config.VerbosePermissions;
                        rootPerm = config.RootPermission;
                        permRole = config.PermissionRole.Trim().ToLowerInvariant();
                    }

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
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine(ex.InnerException);
                }
                catch (HttpException ex)
                {
                    Console.WriteLine(ex);
                }
            });

            return Task.CompletedTask;
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
                    if (!rootPerm.AsEnumerable().CheckPermissions(message, cmd, out index))
                    {
                        var returnMsg = $"Permission number #{index} **{rootPerm.GetAt(index).GetCommand()}** is preventing this action.";
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
