using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public class AdministrationService : INService
    {
        public readonly ConcurrentHashSet<ulong> DeleteMessagesOnCommand;
        private readonly Logger _log;

        public AdministrationService(IEnumerable<GuildConfig> gcs, CommandHandler cmdHandler)
        {
            _log = LogManager.GetCurrentClassLogger();

            DeleteMessagesOnCommand = new ConcurrentHashSet<ulong>(gcs.Where(g => g.DeleteMessageOnCommand).Select(g => g.GuildId));
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
                    if (DeleteMessagesOnCommand.Contains(channel.Guild.Id) && cmd.Name != "prune" && cmd.Name != "pick")
                        await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn("Delmsgoncmd errored...");
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }
    }
}
