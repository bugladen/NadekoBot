using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands
        {
            private static Logger _log { get; }
            public static List<PlayingStatus> RotatingStatusMessages { get; }
            public static bool RotatingStatuses { get; private set; } = false;

            static PlayingRotateCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var conf = uow.BotConfig.GetOrCreate();
                    RotatingStatusMessages = conf.RotatingStatusMessages;
                    RotatingStatuses = conf.RotatingStatuses;
                }

                _log = LogManager.GetCurrentClassLogger();
                var t = Task.Run(async () =>
                {
                    var index = 0;
                    do
                    {
                        try
                        {
                            if (!RotatingStatuses)
                                continue;
                            else
                            {
                                if (index >= RotatingStatusMessages.Count)
                                    index = 0;

                                if (!RotatingStatusMessages.Any())
                                    continue;
                                var status = RotatingStatusMessages[index++].Status;
                                if (string.IsNullOrWhiteSpace(status))
                                    continue;
                                PlayingPlaceholders.ForEach(e => status = status.Replace(e.Key, e.Value()));
                                await NadekoBot.Client.SetGame(status);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn("Rotating playing status errored.\n" + ex);
                        }
                        finally
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1));
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

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying(IUserMessage umsg)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    RotatingStatuses = config.RotatingStatuses = !config.RotatingStatuses;
                    await uow.CompleteAsync();
                }
                if (RotatingStatuses)
                    await umsg.Channel.SendConfirmAsync("🆗 **Rotating playing status enabled.**").ConfigureAwait(false);
                else
                    await umsg.Channel.SendConfirmAsync("ℹ️ **Rotating playing status disabled.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task AddPlaying(IUserMessage umsg, [Remainder] string status)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();
                    var toAdd = new PlayingStatus { Status = status };
                    config.RotatingStatusMessages.Add(toAdd);
                    RotatingStatusMessages.Add(toAdd);
                    await uow.CompleteAsync();
                }

                await umsg.Channel.SendConfirmAsync("✅ **Added.**").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying(IUserMessage umsg)
            {
                if (!RotatingStatusMessages.Any())
                    await umsg.Channel.SendErrorAsync("❎ **No rotating playing statuses set.**");
                else
                {
                    var i = 1;
                    await umsg.Channel.SendConfirmAsync($"ℹ️ {umsg.Author.Mention} `Here is a list of rotating statuses:`\n\n\t" + string.Join("\n\t", RotatingStatusMessages.Select(rs => $"`{i++}.` {rs.Status}")));
                }

            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(IUserMessage umsg, int index)
            {
                index -= 1;

                string msg = "";
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.BotConfig.GetOrCreate();

                    if (index >= config.RotatingStatusMessages.Count)
                        return;
                    msg = config.RotatingStatusMessages[index].Status;
                    config.RotatingStatusMessages.RemoveAt(index);
                    RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }
                await umsg.Channel.SendConfirmAsync($"🗑 **Removed the the playing message:** {msg}").ConfigureAwait(false);
            }
        }
    }
}
