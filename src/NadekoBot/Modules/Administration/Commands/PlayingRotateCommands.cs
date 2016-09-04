using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//todo owner only
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands
        {
            private Logger _log { get; }

            public PlayingRotateCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                Task.Run(async () =>
                {
                    var index = 0;
                    do
                    {
                        try
                        {
                            BotConfig conf;
                            using (var uow = DbHandler.UnitOfWork())
                            {
                                conf = uow.BotConfig.GetOrCreate();
                            }

                            if (!conf.RotatingStatuses)
                                continue;
                            else
                            {
                                if (index >= conf.RotatingStatusMessages.Count)
                                    index = 0;

                                if (!conf.RotatingStatusMessages.Any())
                                    continue;

                                await NadekoBot.Client
                                    .GetCurrentUser()
                                    .ModifyStatusAsync(mpp => mpp.Game = new Game(conf.RotatingStatusMessages[index++].Status))
                                    .ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn("Rotating playing status errored.\n" + ex);
                        }
                        finally
                        {
                            await Task.Delay(15000);
                        }
                    } while (true);
                });
            }

            public static Dictionary<string, Func<string>> PlayingPlaceholders { get; } =
                new Dictionary<string, Func<string>> {
                    {"%servers%", () => NadekoBot.Client.GetGuilds().Count().ToString()},
                    {"%users%", () => NadekoBot.Client.GetGuilds().Select(s => s.GetUsers().Count).Sum().ToString()},
                    {"%playing%", () => {
                            var cnt = Music.Music.MusicPlayers.Count(kvp => kvp.Value.CurrentSong != null);
                            if (cnt != 1) return cnt.ToString();
                            try {
                                var mp = Music.Music.MusicPlayers.FirstOrDefault();
                                return mp.Value.CurrentSong.SongInfo.Title;
                            }
                            catch {
                                return "No songs";
                            }
                        }
                    },
                    {"%queued%", () => Music.Music.MusicPlayers.Sum(kvp => kvp.Value.Playlist.Count).ToString()}
                };

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task RotatePlaying(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                bool status;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    status = config.RotatingStatuses = !config.RotatingStatuses;
                    await uow.CompleteAsync();
                }
                if (status)
                    await channel.SendMessageAsync("`Rotating playing status enabled.`");
                else
                    await channel.SendMessageAsync("`Rotating playing status disabled.`");
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task AddPlaying(IUserMessage umsg, [Remainder] string status)
            {
                var channel = (ITextChannel)umsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    config.RotatingStatusMessages.Add(new PlayingStatus { Status = status });
                    await uow.CompleteAsync();
                }

                await channel.SendMessageAsync("`Added.`").ConfigureAwait(false);
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task ListPlaying(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                List<PlayingStatus> statuses;
                using (var uow = DbHandler.UnitOfWork())
                {
                    statuses = uow.BotConfig.GetOrCreate().RotatingStatusMessages;
                }

                if (!statuses.Any())
                    await channel.SendMessageAsync("`No rotating playing statuses set.`");
                else
                {
                    var i = 1;
                    await channel.SendMessageAsync($"{umsg.Author.Mention} `Here is a list of rotating statuses:`\n\n\t" + string.Join("\n\t", statuses.Select(rs => $"`{i++}.` {rs.Status}")));
                }

            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
            [RequireContext(ContextType.Guild)]
            public async Task RemovePlaying(IUserMessage umsg, int index)
            {
                var channel = (ITextChannel)umsg.Channel;
                index -= 1;

                string msg = "";
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    if (index >= config.RotatingStatusMessages.Count)
                        return;
                    msg = config.RotatingStatusMessages[index].Status;
                    config.RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }
                await channel.SendMessageAsync($"`Removed the the playing message:` {msg}").ConfigureAwait(false);
            }
        }
    }
}