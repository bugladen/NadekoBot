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

        private List<IDMChannel> OwnerChannels { get; set; } = new List<IDMChannel>();

        public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };

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

        public async Task ExecuteExternal(ulong? guildId, ulong channelId, string commandText)
        {
            if (guildId != null)
            {
                var guild = NadekoBot.Client.GetGuild(guildId.Value);
                var channel = guild?.GetChannel(channelId) as SocketTextChannel;
                if (channel == null)
                {
                    _log.Warn("Channel for external execution not found.");
                    return;
                }

                try
                {
                    IUserMessage msg = await channel.SendMessageAsync(commandText).ConfigureAwait(false);
                    msg = (IUserMessage)await channel.GetMessageAsync(msg.Id).ConfigureAwait(false);
                    await TryRunCommand(guild, channel, msg).ConfigureAwait(false);
                    //msg.DeleteAfter(5);
                }
                catch { }
            }
        }

        public Task StartHandling()
        {
            var _ = Task.Run(async () =>
            {
                await Task.Delay(5000).ConfigureAwait(false);
                OwnerChannels = (await Task.WhenAll(_client.Guilds.SelectMany(g => g.Users)
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
                    .OrderBy(x => NadekoBot.Credentials.OwnerIds.IndexOf(x.Id))
                    .ToList();

                if (!OwnerChannels.Any())
                    _log.Warn("No owner channels created! Make sure you've specified correct OwnerId in the credentials.json file.");
                else
                    _log.Info($"Created {OwnerChannels.Count} out of {NadekoBot.Credentials.OwnerIds.Length} owner message channels.");
            });

            _client.MessageReceived += MessageReceivedHandler;
            _client.MessageUpdated += (oldmsg, newMsg, channel) =>
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

        private async Task<bool> TryRunCleverbot(IUserMessage usrMsg, SocketGuild guild)
        {
            if (guild == null)
                return false;
            try
            {
                var message = Games.CleverBotCommands.PrepareMessage(usrMsg, out Games.ChatterBotSession cbs);
                if (message == null || cbs == null)
                    return false;

                PermissionCache pc = Permissions.GetCache(guild.Id);
                if (!pc.Permissions.CheckPermissions(usrMsg,
                    NadekoBot.ModulePrefixes[typeof(Games).Name] + "cleverbot",
                    typeof(Games).Name,
                    out int index))
                {
                    //todo print in guild actually
                    var returnMsg =
                        $"Permission number #{index + 1} **{pc.Permissions[index].GetCommand(guild)}** is preventing this action.";
                    _log.Info(returnMsg);
                    return true;
                }

                var cleverbotExecuted = await Games.CleverBotCommands.TryAsk(cbs, (ITextChannel)usrMsg.Channel, message).ConfigureAwait(false);
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

        private bool IsBlacklisted(IGuild guild, IUserMessage usrMsg) =>
            (guild != null && BlacklistCommands.BlacklistedGuilds.Contains(guild.Id)) ||
            BlacklistCommands.BlacklistedChannels.Contains(usrMsg.Channel.Id) ||
            BlacklistCommands.BlacklistedUsers.Contains(usrMsg.Author.Id);

        private const float _oneThousandth = 1.0f / 1000;
        private Task LogSuccessfulExecution(IUserMessage usrMsg, ExecuteCommandResult exec, ITextChannel channel, int exec1, int exec2, int exec3, int total)
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

        private void LogErroredExecution(IUserMessage usrMsg, ExecuteCommandResult exec, ITextChannel channel, int exec1, int exec2, int exec3, int total)
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

        private async Task<bool> InviteFiltered(IGuild guild, IUserMessage usrMsg)
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

        private async Task<bool> WordFiltered(IGuild guild, IUserMessage usrMsg)
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

                    await TryRunCommand(guild, channel, usrMsg);
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

        public async Task TryRunCommand(SocketGuild guild, ITextChannel channel, IUserMessage usrMsg)
        {
            var execTime = Environment.TickCount;

            if (guild != null && guild.OwnerId != usrMsg.Author.Id)
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
                        PermissionCache pc = Permissions.GetCache(guild.Id);

                        if (!pc.Permissions.CheckPermissions(usrMsg, cr.Trigger, "ActualCustomReactions",
                            out int index))
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
                        try { await usrMsg.DeleteAsync().ConfigureAwait(false); } catch { }
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
                if (Modules.Utility.Utility.CommandMapCommands.AliasMaps.TryGetValue(guild.Id, out ConcurrentDictionary<string, string> maps))
                {

                    var keys = maps.Keys
                        .OrderByDescending(x => x.Length);

                    var lowerMessageContent = messageContent.ToLowerInvariant();
                    foreach (var k in keys)
                    {
                        string newMessageContent;
                        if (lowerMessageContent.StartsWith(k + " "))
                            newMessageContent = maps[k] + messageContent.Substring(k.Length, messageContent.Length - k.Length);
                        else if (lowerMessageContent == k)
                            newMessageContent = maps[k];
                        else
                            continue;

                        _log.Info(@"--Mapping Command--
    GuildId: {0}
    Trigger: {1}
    Mapping: {2}", guild.Id, messageContent, newMessageContent);
                        var oldMessageContent = messageContent;
                        messageContent = newMessageContent;

                        try { await usrMsg.Channel.SendConfirmAsync($"{oldMessageContent} => {newMessageContent}").ConfigureAwait(false); } catch { }
                        break;
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
                        try { await usrMsg.Channel.SendMessageAsync("⚠️ " + exec.Result.ErrorReason).ConfigureAwait(false); } catch { }
                }
            }
            else
            {
                if (usrMsg.Channel is IPrivateChannel)
                {
                    // rofl, gotta do this to prevent dm help message being sent to 
                    // users who are voting on private polls (sending a number in a DM)
                    if (int.TryParse(usrMsg.Content, out int vote)) return;

                    await usrMsg.Channel.SendMessageAsync(Help.DMHelpString).ConfigureAwait(false);

                    await SelfCommands.HandleDmForwarding(usrMsg, OwnerChannels).ConfigureAwait(false);
                }
            }
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
                if (context.Guild != null)
                {
                    PermissionCache pc = Permissions.GetCache(context.Guild.Id);
                    if (!resetCommand && !pc.Permissions.CheckPermissions(context.Message, cmd.Aliases.First(), module.Name, out int index))
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

                    //int price;
                    //if (Permissions.CommandCostCommands.CommandCosts.TryGetValue(cmd.Aliases.First().Trim().ToLowerInvariant(), out price) && price > 0)
                    //{
                    //    var success = await CurrencyHandler.RemoveCurrencyAsync(context.User.Id, $"Running {cmd.Name} command.", price).ConfigureAwait(false);
                    //    if (!success)
                    //    {
                    //        return new ExecuteCommandResult(cmd, pc, SearchResult.FromError(CommandError.Exception, $"Insufficient funds. You need {price}{NadekoBot.BotConfig.CurrencySign} to run this command."));
                    //    }
                    //}
                }

                if (cmd.Name != "resetglobalperms" && 
                    (GlobalPermissionCommands.BlockedCommands.Contains(cmd.Aliases.First().ToLowerInvariant()) ||
                    GlobalPermissionCommands.BlockedModules.Contains(module.Name.ToLowerInvariant())))
                {
                    return new ExecuteCommandResult(cmd, null, SearchResult.FromError(CommandError.Exception, $"Command or module is blocked globally by the bot owner."));
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