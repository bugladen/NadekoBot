using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NadekoBot.Modules.Games.Hangman;
using Discord;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class HangmanCommands : NadekoSubmodule
        {
            //channelId, game
            public static ConcurrentDictionary<ulong, HangmanGame> HangmanGames { get; } = new ConcurrentDictionary<ulong, HangmanGame>();
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Hangmanlist()
            {
                await Context.Channel.SendConfirmAsync(Format.Code(GetText("hangman_types", Prefix)) + "\n" + string.Join(", ", HangmanTermPool.data.Keys));
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Hangman([Remainder]string type = "All")
            {
                var hm = new HangmanGame(Context.Channel, type);

                if (!HangmanGames.TryAdd(Context.Channel.Id, hm))
                {
                    await ReplyErrorLocalized("hangman_running").ConfigureAwait(false);
                    return;
                }

                hm.OnEnded += g =>
                {
                    HangmanGame throwaway;
                    HangmanGames.TryRemove(g.GameChannel.Id, out throwaway);
                };
                try
                {
                    hm.Start();
                }
                catch (Exception ex)
                {
                    try { await Context.Channel.SendErrorAsync(GetText("hangman_start_errored") + " " + ex.Message).ConfigureAwait(false); } catch { }
                    HangmanGame throwaway;
                    HangmanGames.TryRemove(Context.Channel.Id, out throwaway);
                    throwaway.Dispose();
                    return;
                }

                await Context.Channel.SendConfirmAsync(GetText("hangman_game_started"), hm.ScrambledWord + "\n" + hm.GetHangman());
            }
        }
    }
}
