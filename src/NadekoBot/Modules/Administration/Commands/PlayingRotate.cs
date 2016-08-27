using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
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

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task RotatePlaying(IMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);

                    config.RotatingStatuses = !config.RotatingStatuses;
                    await uow.CompleteAsync();
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task AddPlaying(IMessage imsg, string status)
            {
                var channel = (ITextChannel)imsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);
                    config.RotatingStatusMessages.Add(new PlayingStatus { Status = status });
                    await uow.CompleteAsync();
                }

            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task ListPlaying(IMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                List<PlayingStatus> statuses;
                using (var uow = DbHandler.UnitOfWork())
                {
                    statuses = uow.GuildConfigs.For(channel.Guild.Id).RotatingStatusMessages;
                }

                if (!statuses.Any())
                    await channel.SendMessageAsync("`No rotating playing statuses set.`");
                else
                {
                    var i = 1;
                    await channel.SendMessageAsync($"{imsg.Author.Mention} Here is a list of rotating statuses:\n" + string.Join("\n", statuses.Select(rs => $"`{i++}.` {rs.Status}\n")));
                }

            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task RemovePlaying(IMessage imsg, int index)
            {
                var channel = (ITextChannel)imsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(channel.Guild.Id);

                    if (index >= config.RotatingStatusMessages.Count)
                        return;

                    config.RotatingStatusMessages.RemoveAt(index);
                    await uow.CompleteAsync();
                }
            }
        }
    }
}