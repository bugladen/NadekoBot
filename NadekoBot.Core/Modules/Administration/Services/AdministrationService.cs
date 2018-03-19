using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NLog;
using System.Collections.Concurrent;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Administration.Services
{
    public class AdministrationService : INService
    {
        public ConcurrentHashSet<ulong> DeleteMessagesOnCommand { get; }
        public ConcurrentDictionary<ulong, bool> DeleteMessagesOnCommandChannels { get; }

        private readonly Logger _log;
        private readonly NadekoBot _bot;
        private readonly DbService _db;

        public AdministrationService(NadekoBot bot, CommandHandler cmdHandler, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _bot = bot;
            _db = db;

            DeleteMessagesOnCommand = new ConcurrentHashSet<ulong>(bot.AllGuildConfigs
                .Where(g => g.DeleteMessageOnCommand)
                .Select(g => g.GuildId));

            DeleteMessagesOnCommandChannels = new ConcurrentDictionary<ulong, bool>(bot.AllGuildConfigs
                .SelectMany(x => x.DelMsgOnCmdChannels)
                .ToDictionary(x => x.ChannelId, x => x.State)
                .ToConcurrent());

            cmdHandler.CommandExecuted += DelMsgOnCmd_Handler;
        }

        private Task DelMsgOnCmd_Handler(IUserMessage msg, CommandInfo cmd)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var channel = msg.Channel as SocketTextChannel;
                    if (channel == null)
                        return;

                    if (DeleteMessagesOnCommandChannels.TryGetValue(channel.Id, out var state))
                    {
                        if (state)
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                        }
                        //if state is false, that means do not do it
                    }
                    else if (DeleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune" && cmd.Name != "pick")
                    {
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Delmsgoncmd errored...");
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }

        public bool ToggleDeleteMessageOnCommand(ulong guildId)
        {
            bool enabled;
            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.GuildConfigs.For(guildId, set => set);
                enabled = conf.DeleteMessageOnCommand = !conf.DeleteMessageOnCommand;

                uow.Complete();
            }
            return enabled;
        }
    }
}
