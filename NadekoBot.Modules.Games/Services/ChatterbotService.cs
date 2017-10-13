using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using NadekoBot.Modules.Games.Common.ChatterBot;

namespace NadekoBot.Modules.Games.Services
{
    public class ChatterBotService : IEarlyBlockingExecutor, INService
    {
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;
        private readonly PermissionService _perms;
        private readonly CommandHandler _cmd;
        private readonly NadekoStrings _strings;
        private readonly IBotCredentials _creds;

        public ConcurrentDictionary<ulong, Lazy<IChatterBotSession>> ChatterBotGuilds { get; }

        public ChatterBotService(DiscordSocketClient client, PermissionService perms, 
            NadekoBot bot, CommandHandler cmd, NadekoStrings strings, 
            IBotCredentials creds)
        {
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _perms = perms;
            _cmd = cmd;
            _strings = strings;
            _creds = creds;

            ChatterBotGuilds = new ConcurrentDictionary<ulong, Lazy<IChatterBotSession>>(
                    bot.AllGuildConfigs
                        .Where(gc => gc.CleverbotEnabled)
                        .ToDictionary(gc => gc.GuildId, gc => new Lazy<IChatterBotSession>(() => CreateSession(), true)));
        }

        public IChatterBotSession CreateSession()
        {
            if (string.IsNullOrWhiteSpace(_creds.CleverbotApiKey))
                return new ChatterBotSession();
            else
                return new OfficialCleverbotSession(_creds.CleverbotApiKey);
        }

        public string PrepareMessage(IUserMessage msg, out IChatterBotSession cleverbot)
        {
            var channel = msg.Channel as ITextChannel;
            cleverbot = null;

            if (channel == null)
                return null;

            if (!ChatterBotGuilds.TryGetValue(channel.Guild.Id, out Lazy<IChatterBotSession> lazyCleverbot))
                return null;

            cleverbot = lazyCleverbot.Value;

            var nadekoId = _client.CurrentUser.Id;
            var normalMention = $"<@{nadekoId}> ";
            var nickMention = $"<@!{nadekoId}> ";
            string message;
            if (msg.Content.StartsWith(normalMention))
            {
                message = msg.Content.Substring(normalMention.Length).Trim();
            }
            else if (msg.Content.StartsWith(nickMention))
            {
                message = msg.Content.Substring(nickMention.Length).Trim();
            }
            else
            {
                return null;
            }

            return message;
        }

        public async Task<bool> TryAsk(IChatterBotSession cleverbot, ITextChannel channel, string message)
        {
            await channel.TriggerTypingAsync().ConfigureAwait(false);

            var response = await cleverbot.Think(message).ConfigureAwait(false);
            try
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false);
            }
            catch
            {
                await channel.SendConfirmAsync(response.SanitizeMentions()).ConfigureAwait(false); // try twice :\
            }
            return true;
        }

        public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage usrMsg)
        {
            if (!(guild is SocketGuild sg))
                return false;
            try
            {
                var message = PrepareMessage(usrMsg, out IChatterBotSession cbs);
                if (message == null || cbs == null)
                    return false;
                
                var pc = _perms.GetCache(guild.Id);
                if (!pc.Permissions.CheckPermissions(usrMsg,
                    "cleverbot",
                    "Games".ToLowerInvariant(),
                    out int index))
                {
                    if (pc.Verbose)
                    {
                        var returnMsg = _strings.GetText("trigger", guild.Id, "Permissions".ToLowerInvariant(), index + 1, Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), (SocketGuild)guild)));
                        try { await usrMsg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { }
                        _log.Info(returnMsg);
                    }
                    return true;
                }

                var cleverbotExecuted = await TryAsk(cbs, (ITextChannel)usrMsg.Channel, message).ConfigureAwait(false);
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
    }
}
