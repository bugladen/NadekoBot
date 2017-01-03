using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NLog;
using System.Diagnostics;
using Discord.Commands;
using NadekoBot.Services.Database.Models;
using NadekoBot.Modules.Permissions;
using Discord.Net;
using NadekoBot.Extensions;
using static NadekoBot.Modules.Permissions.Permissions;
using NadekoBot.Modules.Help;
using static NadekoBot.Modules.Administration.Administration;
using NadekoBot.Modules.CustomReactions;
using NadekoBot.Modules.Games;
using System.Collections.Concurrent;

namespace NadekoBot.Services
{
    public class IGuildUserComparer : IEqualityComparer<IGuildUser>
    {
        public bool Equals(IGuildUser x, IGuildUser y) => x.Id == y.Id;

        public int GetHashCode(IGuildUser obj) => obj.Id.GetHashCode();
    }
    public class CommandHandler
    {
        private ShardedDiscordClient _client;
        private CommandService _commandService;
        private Logger _log;

        private List<IDMChannel> ownerChannels { get; set; }

        public event Func<SocketUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };

        //userid/msg count
        public ConcurrentDictionary<ulong, uint> UserMessagesSent { get; } = new ConcurrentDictionary<ulong, uint>();

        public CommandHandler(ShardedDiscordClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _log = LogManager.GetCurrentClassLogger();
        }
        public async Task StartHandling()
        {
            ownerChannels = (await Task.WhenAll(_client.GetGuilds().SelectMany(g => g.Users)
                                  .Where(u => NadekoBot.Credentials.OwnerIds.Contains(u.Id))
                                  .Distinct(new IGuildUserComparer())
                                  .Select(async u => { try { return await u.CreateDMChannelAsync(); } catch { return null; } })))
                                      .Where(ch => ch != null)
                                      .ToList();

            if (!ownerChannels.Any())
                _log.Warn("No owner channels created! Make sure you've specified correct OwnerId in the credentials.json file.");
            else
                _log.Info($"Created {ownerChannels.Count} out of {NadekoBot.Credentials.OwnerIds.Length} owner message channels.");

            _client.MessageReceived += MessageReceivedHandler;
        }

        private async void MessageReceivedHandler(SocketMessage msg)
        {
            try
            {
                var usrMsg = msg as SocketUserMessage;
                if (usrMsg == null)
                    return;

                //if (!usrMsg.IsAuthor())
                //    UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => ++old);

                if (msg.Author.IsBot || !NadekoBot.Ready) //no bots
                    return;

                var guild = (msg.Channel as SocketTextChannel)?.Guild;

                if (guild != null && guild.OwnerId != msg.Author.Id)
                {
                    //todo split checks into their own modules
                    if (Permissions.FilterCommands.InviteFilteringChannels.Contains(msg.Channel.Id) ||
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
                                _log.Warn("I do not have permission to filter invites in channel with id " + msg.Channel.Id, ex);
                            }
                        }
                    }

                    var filteredWords = Permissions.FilterCommands.FilteredWordsForChannel(msg.Channel.Id, guild.Id).Concat(Permissions.FilterCommands.FilteredWordsForServer(guild.Id));
                    var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
                    if (filteredWords.Any(w => wordsInMessage.Contains(w)))
                    {
                        try
                        {
                            await usrMsg.DeleteAsync().ConfigureAwait(false);
                            return;
                        }
                        catch (HttpException ex)
                        {
                            _log.Warn("I do not have permission to filter words in channel with id " + msg.Channel.Id, ex);
                        }
                    }
                }

                BlacklistItem blacklistedItem;
                if ((blacklistedItem = Permissions.BlacklistCommands.BlacklistedItems.FirstOrDefault(bi =>
                     (bi.Type == BlacklistItem.BlacklistType.Server && bi.ItemId == guild?.Id) ||
                     (bi.Type == BlacklistItem.BlacklistType.Channel && bi.ItemId == msg.Channel.Id) ||
                     (bi.Type == BlacklistItem.BlacklistType.User && bi.ItemId == msg.Author.Id))) != null)
                {
                    return;
                }

#if !GLOBAL_NADEKO
                try
                {
                    var cleverbotExecuted = await Games.CleverBotCommands.TryAsk(usrMsg);
                    if (cleverbotExecuted)
                    {
                        _log.Info($@"CleverBot Executed
        Server: {guild.Name} [{guild.Id}]
        Channel: {usrMsg.Channel?.Name} [{usrMsg.Channel?.Id}]
        UserId: {usrMsg.Author} [{usrMsg.Author.Id}]
        Message: {usrMsg.Content}");
                        return;
                    }
                }
                catch (Exception ex) { _log.Warn(ex, "Error in cleverbot"); }
#endif
                try
                {
                    // maybe this message is a custom reaction
                    var crExecuted = await CustomReactions.TryExecuteCustomReaction(usrMsg).ConfigureAwait(false);

                    //if it was, don't execute the command
                    if (crExecuted)
                        return;
                }
                catch { }

                string messageContent = usrMsg.Content;

                var sw = new Stopwatch();
                sw.Start();
                var exec = await ExecuteCommand(new CommandContext(_client.MainClient, usrMsg), messageContent, DependencyMap.Empty, MultiMatchHandling.Best);
                var command = exec.CommandInfo;
                var permCache = exec.PermissionCache;
                var result = exec.Result;
                sw.Stop();
                var channel = (msg.Channel as ITextChannel);
                if (result.IsSuccess)
                {
                    await CommandExecuted(usrMsg, command);
                    _log.Info("Command Executed after {4}s\n\t" +
                                "User: {0}\n\t" +
                                "Server: {1}\n\t" +
                                "Channel: {2}\n\t" +
                                "Message: {3}",
                                msg.Author + " [" + msg.Author.Id + "]", // {0}
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
                                msg.Author + " [" + msg.Author.Id + "]", // {0}
                                (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                                (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                                usrMsg.Content,// {3}
                                result.ErrorReason, // {4}
                                sw.Elapsed.TotalSeconds // {5}
                                );
                    if (guild != null && command != null && result.Error == CommandError.Exception)
                    {
                        if (permCache != null && permCache.Verbose)
                            try { await msg.Channel.SendMessageAsync("⚠️ " + result.ErrorReason).ConfigureAwait(false); } catch { }
                    }
                }
                else
                {
                    if (msg.Channel is IPrivateChannel)
                    {
                        //rofl, gotta do this to prevent this message from occuring on polls
                        int vote;
                        if (int.TryParse(msg.Content, out vote)) return;

                        await msg.Channel.SendMessageAsync(Help.DMHelpString).ConfigureAwait(false);

                        await DMForwardCommands.HandleDMForwarding(msg, ownerChannels);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Error in CommandHandler");
                if (ex.InnerException != null)
                    _log.Warn(ex.InnerException, "Inner Exception of the error in CommandHandler");
            }

            return;
        }
        public Task<ExecuteCommandResult> ExecuteCommandAsync(CommandContext context, int argPos, IDependencyMap dependencyMap = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteCommand(context, context.Message.Content.Substring(argPos), dependencyMap, multiMatchHandling);


        public async Task<ExecuteCommandResult> ExecuteCommand(CommandContext context, string input, IDependencyMap dependencyMap = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            dependencyMap = dependencyMap ?? DependencyMap.Empty;

            var searchResult = _commandService.Search(context, input);
            if (!searchResult.IsSuccess)
                return new ExecuteCommandResult(null, null, searchResult);

            var commands = searchResult.Commands;
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                var preconditionResult = await commands[i].CheckPreconditionsAsync(context).ConfigureAwait(false);
                if (!preconditionResult.IsSuccess)
                {
                    if (commands.Count == 1)
                        return new ExecuteCommandResult(null, null, preconditionResult);
                    else
                        continue;
                }

                var parseResult = await commands[i].ParseAsync(context, searchResult, preconditionResult).ConfigureAwait(false);
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
                            return new ExecuteCommandResult(null, null, parseResult);
                        else
                            continue;
                    }
                }

                var cmd = commands[i].Command;
                bool resetCommand = cmd.Name == "resetperms";
                var module = cmd.Module.GetTopLevelModule();
                PermissionCache pc;
                if (context.Guild != null)
                {
                    pc = Permissions.Cache.GetOrAdd(context.Guild.Id, (id) =>
                    {
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            var config = uow.GuildConfigs.PermissionsFor(context.Guild.Id);
                            return new PermissionCache()
                            {
                                Verbose = config.VerbosePermissions,
                                RootPermission = config.RootPermission,
                                PermRole = config.PermissionRole.Trim().ToLowerInvariant(),
                            };
                        }
                    });
                    int index;
                    if (!resetCommand && !pc.RootPermission.AsEnumerable().CheckPermissions(context.Message, cmd.Aliases.First(), module.Name, out index))
                    {
                        var returnMsg = $"Permission number #{index + 1} **{pc.RootPermission.GetAt(index).GetCommand((SocketGuild)context.Guild)}** is preventing this action.";
                        return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, returnMsg));
                    }


                    if (module.Name == typeof(Permissions).Name)
                    {
                        if (!((IGuildUser)context.User).GetRoles().Any(r => r.Name.Trim().ToLowerInvariant() == pc.PermRole.Trim().ToLowerInvariant()))
                        {
                            return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, $"You need the **{pc.PermRole}** role in order to use permission commands."));
                        }
                    }
                }


                if (CmdCdsCommands.HasCooldown(cmd, context.Guild, context.User))
                    return new ExecuteCommandResult(cmd, null, SearchResult.FromError(CommandError.Exception, $"That command is on cooldown for you."));

                return new ExecuteCommandResult(cmd, null, await commands[i].ExecuteAsync(context, parseResult, dependencyMap));
            }

            return new ExecuteCommandResult(null, null, SearchResult.FromError(CommandError.UnknownCommand, "This input does not match any overload."));
        }

        public struct ExecuteCommandResult
        {
            public readonly CommandInfo CommandInfo;
            public readonly PermissionCache PermissionCache;
            public readonly IResult Result;

            public ExecuteCommandResult(CommandInfo commandInfo, PermissionCache cache, IResult result)
            {
                this.CommandInfo = commandInfo;
                this.PermissionCache = cache;
                this.Result = result;
            }
        }
    }
}