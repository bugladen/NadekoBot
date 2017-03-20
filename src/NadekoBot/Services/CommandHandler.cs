using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NLog;
using Discord.Commands;
using NadekoBot.Modules.Permissions;
using Discord.Net;
using NadekoBot.Extensions;
using static NadekoBot.Modules.Permissions.Permissions;
using NadekoBot.Modules.Help;
using static NadekoBot.Modules.Administration.Administration;
using NadekoBot.Modules.CustomReactions;
using NadekoBot.Modules.Games;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using NadekoBot.DataStructures;

namespace NadekoBot.Services
{
    public class GuildUserComparer : IEqualityComparer<IGuildUser>
    {
        public bool Equals(IGuildUser x, IGuildUser y) => x.Id == y.Id;

        public int GetHashCode(IGuildUser obj) => obj.Id.GetHashCode();
    }
    public class CommandHandler
    {
        public const int GlobalCommandsCooldown = 750;

        private readonly DiscordShardedClient _client;
        private readonly CommandService _commandService;
        private readonly Logger _log;

        private List<IDMChannel> ownerChannels { get; set; } = new List<IDMChannel>();

        public event Func<SocketUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };

        //userid/msg count
        public ConcurrentDictionary<ulong, uint> UserMessagesSent { get; } = new ConcurrentDictionary<ulong, uint>();

        public ConcurrentHashSet<ulong> UsersOnShortCooldown { get; } = new ConcurrentHashSet<ulong>();
        private readonly Timer _clearUsersOnShortCooldown;

        public CommandHandler(DiscordShardedClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _log = LogManager.GetCurrentClassLogger();

            _clearUsersOnShortCooldown = new Timer(_ =>
            {
                UsersOnShortCooldown.Clear();
            }, null, GlobalCommandsCooldown, GlobalCommandsCooldown);
        }
        public Task StartHandling()
        {
            var _ = Task.Run(async () =>
            {
                await Task.Delay(5000).ConfigureAwait(false);
                ownerChannels = (await Task.WhenAll(_client.GetGuilds().SelectMany(g => g.Users)
                        .Where(u => NadekoBot.Credentials.OwnerIds.Contains(u.Id))
                        .Distinct(new GuildUserComparer())
                        .Select(async u =>
                        {
                            try
                            {
                                return await u.CreateDMChannelAsync();
                            }
                            catch
                            {
                                return null;
                            }
                        })))
                    .Where(ch => ch != null)
                    .ToList();

                if (!ownerChannels.Any())
                    _log.Warn("No owner channels created! Make sure you've specified correct OwnerId in the credentials.json file.");
                else
                    _log.Info($"Created {ownerChannels.Count} out of {NadekoBot.Credentials.OwnerIds.Count} owner message channels.");
            });

            _client.MessageReceived += MessageReceivedHandler;
            _client.MessageUpdated += (oldmsg, newMsg) =>
            {
                var ignore = Task.Run(async () =>
                {
                    try
                    {
                        var usrMsg = newMsg as SocketUserMessage;
                        var guild = (usrMsg?.Channel as ITextChannel)?.Guild;

                        if (guild != null && !await InviteFiltered(guild, usrMsg).ConfigureAwait(false))
                            await WordFiltered(guild, usrMsg).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            };
            return Task.CompletedTask;
        }

        private async Task<bool> TryRunCleverbot(SocketUserMessage usrMsg, IGuild guild)
        {
            if (guild == null)
                return false;
            try
            {
                var cleverbotExecuted = await Games.CleverBotCommands.TryAsk(usrMsg).ConfigureAwait(false);
                if (cleverbotExecuted)
                {
                    _log.Info($@"CleverBot Executed
        Server: {guild.Name} [{guild.Id}]
        Channel: {usrMsg.Channel?.Name} [{usrMsg.Channel?.Id}]
        UserId: {usrMsg.Author} [{usrMsg.Author.Id}]
        Message: {usrMsg.Content}");
                    return true;
                }
            }
            catch (Exception ex) { _log.Warn(ex, "Error in cleverbot"); }
            return false;
        }

        private bool IsBlacklisted(IGuild guild, SocketUserMessage usrMsg) =>
            usrMsg.Author?.Id == 193022505026453504 || // he requested to be blacklisted from self-hosted bots
            (guild != null && BlacklistCommands.BlacklistedGuilds.Contains(guild.Id)) ||
            BlacklistCommands.BlacklistedChannels.Contains(usrMsg.Channel.Id) ||
            BlacklistCommands.BlacklistedUsers.Contains(usrMsg.Author.Id);

        private const float _oneThousandth = 1.0f / 1000;
        private Task LogSuccessfulExecution(SocketUserMessage usrMsg, ExecuteCommandResult exec, SocketTextChannel channel, int exec1, int exec2, int exec3, int total)
        {
            _log.Info("Command Executed after {4}/{5}/{6}/{7}s\n\t" +
                        "User: {0}\n\t" +
                        "Server: {1}\n\t" +
                        "Channel: {2}\n\t" +
                        "Message: {3}",
                        usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                        (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                        (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                        usrMsg.Content, // {3}
                        exec1 * _oneThousandth, // {4}
                        exec2 * _oneThousandth, // {5}
                        exec3 * _oneThousandth, // {6}
                        total * _oneThousandth // {7}
                        );
            return Task.CompletedTask;
        }

        private void LogErroredExecution(SocketUserMessage usrMsg, ExecuteCommandResult exec, SocketTextChannel channel, int exec1, int exec2, int exec3, int total)
        {
            _log.Warn("Command Errored after {5}/{6}/{7}/{8}s\n\t" +
                        "User: {0}\n\t" +
                        "Server: {1}\n\t" +
                        "Channel: {2}\n\t" +
                        "Message: {3}\n\t" +
                        "Error: {4}",
                        usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                        (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                        (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                        usrMsg.Content,// {3}
                        exec.Result.ErrorReason, // {4}
                        exec1 * _oneThousandth, // {5}
                        exec2 * _oneThousandth, // {6}
                        exec3 * _oneThousandth, // {7}
                        total * _oneThousandth // {8}
                        );
        }

        private async Task<bool> InviteFiltered(IGuild guild, SocketUserMessage usrMsg)
        {
            if ((Permissions.FilterCommands.InviteFilteringChannels.Contains(usrMsg.Channel.Id) ||
                Permissions.FilterCommands.InviteFilteringServers.Contains(guild.Id)) &&
                    usrMsg.Content.IsDiscordInvite())
            {
                try
                {
                    await usrMsg.DeleteAsync().ConfigureAwait(false);
                    return true;
                }
                catch (HttpException ex)
                {
                    _log.Warn("I do not have permission to filter invites in channel with id " + usrMsg.Channel.Id, ex);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> WordFiltered(IGuild guild, SocketUserMessage usrMsg)
        {
            var filteredChannelWords = Permissions.FilterCommands.FilteredWordsForChannel(usrMsg.Channel.Id, guild.Id) ?? new ConcurrentHashSet<string>();
            var filteredServerWords = Permissions.FilterCommands.FilteredWordsForServer(guild.Id) ?? new ConcurrentHashSet<string>();
            var wordsInMessage = usrMsg.Content.ToLowerInvariant().Split(' ');
            if (filteredChannelWords.Count != 0 || filteredServerWords.Count != 0)
            {
                foreach (var word in wordsInMessage)
                {
                    if (filteredChannelWords.Contains(word) ||
                        filteredServerWords.Contains(word))
                    {
                        try
                        {
                            await usrMsg.DeleteAsync().ConfigureAwait(false);
                        }
                        catch (HttpException ex)
                        {
                            _log.Warn("I do not have permission to filter words in channel with id " + usrMsg.Channel.Id, ex);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private Task MessageReceivedHandler(SocketMessage msg)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (msg.Author.IsBot || !NadekoBot.Ready) //no bots, wait until bot connected and initialized
                        return;

                    var execTime = Environment.TickCount;

                    var usrMsg = msg as SocketUserMessage;
                    if (usrMsg == null) //has to be an user message, not system/other messages.
                        return;

                    if (usrMsg.Author.Id == 193022505026453504)
                        return;
#if !GLOBAL_NADEKO
                    // track how many messagges each user is sending
                    UserMessagesSent.AddOrUpdate(usrMsg.Author.Id, 1, (key, old) => ++old);
#endif

                    var channel = msg.Channel as SocketTextChannel;
                    var guild = channel?.Guild;

                    if (guild != null && guild.OwnerId != msg.Author.Id)
                    {
                        if (await InviteFiltered(guild, usrMsg).ConfigureAwait(false))
                            return;

                        if (await WordFiltered(guild, usrMsg).ConfigureAwait(false))
                            return;
                    }

                    if (IsBlacklisted(guild, usrMsg))
                        return;

                    var exec1 = Environment.TickCount - execTime;

                    var cleverBotRan = await Task.Run(() => TryRunCleverbot(usrMsg, guild)).ConfigureAwait(false);
                    if (cleverBotRan)
                        return;

                    var exec2 = Environment.TickCount - execTime;

                    // maybe this message is a custom reaction
                    // todo log custom reaction executions. return struct with info
                    var cr = await Task.Run(() => CustomReactions.TryGetCustomReaction(usrMsg)).ConfigureAwait(false);
                    if (cr != null) //if it was, don't execute the command
                    {
                        try
                        {
                            if (guild != null)
                            {
                                PermissionCache pc;
                                if (!Permissions.Cache.TryGetValue(guild.Id, out pc))
                                {
                                    using (var uow = DbHandler.UnitOfWork())
                                    {
                                        var config = uow.GuildConfigs.For(guild.Id,
                                            set => set.Include(x => x.Permissions));
                                        Permissions.UpdateCache(config);
                                    }
                                    Permissions.Cache.TryGetValue(guild.Id, out pc);
                                    if (pc == null)
                                        throw new Exception("Cache is null.");
                                }
                                int index;
                                if (
                                    !pc.Permissions.CheckPermissions(usrMsg, cr.Trigger, "ActualCustomReactions",
                                        out index))
                                {
                                    //todo print in guild actually
                                    var returnMsg =
                                        $"Permission number #{index + 1} **{pc.Permissions[index].GetCommand(guild)}** is preventing this action.";
                                    _log.Info(returnMsg);
                                    return;
                                }
                            }
                            await cr.Send(usrMsg).ConfigureAwait(false);

                            if (cr.AutoDeleteTrigger)
                            {
                                try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn("Sending CREmbed failed");
                            _log.Warn(ex);
                        }
                        return;
                    }

                    var exec3 = Environment.TickCount - execTime;

                    string messageContent = usrMsg.Content;
                    if (guild != null)
                    {
                        ConcurrentDictionary<string, string> maps;
                        if (Modules.Utility.Utility.CommandMapCommands.AliasMaps.TryGetValue(guild.Id, out maps))
                        {
                            string newMessageContent;
                            if (maps.TryGetValue(messageContent.Trim().ToLowerInvariant(), out newMessageContent))
                            {
                                _log.Info(@"--Mapping Command--
    GuildId: {0}
    Trigger: {1}
    Mapping: {2}", guild.Id, messageContent, newMessageContent);
                                var oldMessageContent = messageContent;
                                messageContent = newMessageContent;

                                try { await usrMsg.Channel.SendConfirmAsync($"{oldMessageContent} => {newMessageContent}").ConfigureAwait(false); } catch { }
                            }
                        }
                    }
                    

                    // execute the command and measure the time it took
                    var exec = await Task.Run(() => ExecuteCommand(new CommandContext(_client, usrMsg), messageContent, DependencyMap.Empty, MultiMatchHandling.Best)).ConfigureAwait(false);
                    execTime = Environment.TickCount - execTime;

                    if (exec.Result.IsSuccess)
                    {
                        await CommandExecuted(usrMsg, exec.CommandInfo).ConfigureAwait(false);
                        await LogSuccessfulExecution(usrMsg, exec, channel, exec1, exec2, exec3, execTime).ConfigureAwait(false);
                    }
                    else if (!exec.Result.IsSuccess && exec.Result.Error != CommandError.UnknownCommand)
                    {
                        LogErroredExecution(usrMsg, exec, channel, exec1, exec2, exec3, execTime);
                        if (guild != null && exec.CommandInfo != null && exec.Result.Error == CommandError.Exception)
                        {
                            if (exec.PermissionCache != null && exec.PermissionCache.Verbose)
                                try { await msg.Channel.SendMessageAsync("⚠️ " + exec.Result.ErrorReason).ConfigureAwait(false); } catch { }
                        }
                    }
                    else
                    {
                        if (msg.Channel is IPrivateChannel)
                        {
                            // rofl, gotta do this to prevent dm help message being sent to 
                            // users who are voting on private polls (sending a number in a DM)
                            int vote;
                            if (int.TryParse(msg.Content, out vote)) return;

                            await msg.Channel.SendMessageAsync(Help.DMHelpString).ConfigureAwait(false);

                            await SelfCommands.HandleDmForwarding(msg, ownerChannels).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Error in CommandHandler");
                    _log.Warn(ex);
                    if (ex.InnerException != null)
                    {
                        _log.Warn("Inner Exception of the error in CommandHandler");
                        _log.Warn(ex.InnerException);
                    }
                }
            });
            return Task.CompletedTask;
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
                var resetCommand = cmd.Name == "resetperms";
                var module = cmd.Module.GetTopLevelModule();
                PermissionCache pc;
                if (context.Guild != null)
                {
                    //todo move to permissions module?
                    if (!Permissions.Cache.TryGetValue(context.Guild.Id, out pc))
                    {
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            var config = uow.GuildConfigs.GcWithPermissionsv2For(context.Guild.Id);
                            Permissions.UpdateCache(config);
                        }
                        Permissions.Cache.TryGetValue(context.Guild.Id, out pc);
                        if(pc == null)
                            throw new Exception("Cache is null.");
                    }
                    int index;
                    if (!resetCommand && !pc.Permissions.CheckPermissions(context.Message, cmd.Aliases.First(), module.Name, out index))
                    {
                        var returnMsg = $"Permission number #{index + 1} **{pc.Permissions[index].GetCommand((SocketGuild)context.Guild)}** is preventing this action.";
                        return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, returnMsg));
                    }


                    if (module.Name == typeof(Permissions).Name)
                    {
                        var guildUser = (IGuildUser)context.User;
                        if (!guildUser.GetRoles().Any(r => r.Name.Trim().ToLowerInvariant() == pc.PermRole.Trim().ToLowerInvariant()) && guildUser.Id != guildUser.Guild.OwnerId)
                        {
                            return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, $"You need the **{pc.PermRole}** role in order to use permission commands."));
                        }
                    }

                    int price;
                    if (Permissions.CommandCostCommands.CommandCosts.TryGetValue(cmd.Aliases.First().Trim().ToLowerInvariant(), out price) && price > 0)
                    {
                        var success = await CurrencyHandler.RemoveCurrencyAsync(context.User.Id, $"Running {cmd.Name} command.", price).ConfigureAwait(false);
                        if (!success)
                        {
                            return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, $"Insufficient funds. You need {price}{NadekoBot.BotConfig.CurrencySign} to run this command."));
                        }
                    }
                }

                // Bot will ignore commands which are ran more often than what specified by
                // GlobalCommandsCooldown constant (miliseconds)
                if (!UsersOnShortCooldown.Add(context.Message.Author.Id))
                    return new ExecuteCommandResult(cmd, null, SearchResult.FromError(CommandError.Exception, "You are on a global cooldown."));

                if (CmdCdsCommands.HasCooldown(cmd, context.Guild, context.User))
                    return new ExecuteCommandResult(cmd, null, SearchResult.FromError(CommandError.Exception, "That command is on a cooldown for you."));

                return new ExecuteCommandResult(cmd, null, await commands[i].ExecuteAsync(context, parseResult, dependencyMap));
            }

            return new ExecuteCommandResult(null, null, SearchResult.FromError(CommandError.UnknownCommand, "This input does not match any overload."));
        }
    }
}