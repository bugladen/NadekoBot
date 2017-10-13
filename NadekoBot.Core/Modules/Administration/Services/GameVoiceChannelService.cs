using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public class GameVoiceChannelService : INService
    {
        public readonly ConcurrentHashSet<ulong> GameVoiceChannels = new ConcurrentHashSet<ulong>();

        private readonly Logger _log;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public GameVoiceChannelService(DiscordSocketClient client, DbService db, NadekoBot bot)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;

            GameVoiceChannels = new ConcurrentHashSet<ulong>(
                bot.AllGuildConfigs.Where(gc => gc.GameVoiceChannel != null)
                                         .Select(gc => gc.GameVoiceChannel.Value));

            _client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

        }

        private Task Client_UserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    var gUser = usr as SocketGuildUser;
                    if (gUser == null)
                        return;

                    var game = gUser.Game?.Name?.TrimTo(50).ToLowerInvariant();

                    if (oldState.VoiceChannel == newState.VoiceChannel ||
                        newState.VoiceChannel == null)
                        return;

                    if (!GameVoiceChannels.Contains(newState.VoiceChannel.Id) ||
                        string.IsNullOrWhiteSpace(game))
                        return;

                    var vch = gUser.Guild.VoiceChannels
                        .FirstOrDefault(x => x.Name.ToLowerInvariant() == game);

                    if (vch == null)
                        return;

                    await Task.Delay(1000).ConfigureAwait(false);
                    await gUser.ModifyAsync(gu => gu.Channel = vch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });

            return Task.CompletedTask;
        }
    }
}
