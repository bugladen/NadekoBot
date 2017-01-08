using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Commands.Hangman;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class HangmanCommands : ModuleBase
        {
            private static Logger _log { get; }

            //channelId, game
            public static ConcurrentDictionary<ulong, HangmanGame> HangmanGames { get; } = new ConcurrentDictionary<ulong, HangmanGame>();
            private static string typesStr { get; } = "";

            static HangmanCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                typesStr = $"`List of \"{NadekoBot.ModulePrefixes[typeof(Games).Name]}hangman\" term types:`\n" + String.Join(", ", Enum.GetNames(typeof(HangmanTermPool.HangmanTermType)));
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Hangmanlist()
            {
                await Context.Channel.SendConfirmAsync(typesStr);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Hangman(HangmanTermPool.HangmanTermType type = HangmanTermPool.HangmanTermType.All)
            {
                var hm = new HangmanGame(Context.Channel, type);

                if (!HangmanGames.TryAdd(Context.Channel.Id, hm))
                {
                    await Context.Channel.SendErrorAsync("Hangman game already running on this channel.").ConfigureAwait(false);
                    return;
                }

                hm.OnEnded += (g) =>
                {
                    HangmanGame throwaway;
                    HangmanGames.TryRemove(g.GameChannel.Id, out throwaway);
                };
                hm.Start();

                await Context.Channel.SendConfirmAsync("Hangman game started", hm.ScrambledWord + "\n" + hm.GetHangman() + "\n" + hm.ScrambledWord);
            }
        }
    }
}
