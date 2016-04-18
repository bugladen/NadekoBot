using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Games.Commands.Trivia;
using System.Collections.Concurrent;
using System.Linq;

namespace NadekoBot.Modules.Games.Commands
{
    internal class TriviaCommands : DiscordCommand
    {
        public static ConcurrentDictionary<ulong, TriviaGame> RunningTrivias = new ConcurrentDictionary<ulong, TriviaGame>();

        public TriviaCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "t")
                .Description($"Starts a game of trivia. You can add nohint to prevent hints." +
                              "First player to get to 10 points wins. 30 seconds per question." +
                              $"\n**Usage**:`{Module.Prefix}t nohint`")
                .Parameter("args", ParameterType.Multiple)
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (!RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                    {
                        var showHints = !e.Args.Contains("nohint");
                        var triviaGame = new TriviaGame(e, showHints);
                        if (RunningTrivias.TryAdd(e.Server.Id, triviaGame))
                            await e.Channel.SendMessage("**Trivia game started!**").ConfigureAwait(false);
                        else
                            await triviaGame.StopGame().ConfigureAwait(false);
                    }
                    else
                        await e.Channel.SendMessage("Trivia game is already running on this server.\n" + trivia.CurrentQuestion).ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "tl")
                .Description("Shows a current trivia leaderboard.")
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                        await e.Channel.SendMessage(trivia.GetLeaderboard()).ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "tq")
                .Description("Quits current trivia after current question.")
                .Do(async e =>
                {
                    TriviaGame trivia;
                    if (RunningTrivias.TryGetValue(e.Server.Id, out trivia))
                    {
                        await trivia.StopGame().ConfigureAwait(false);
                    }
                    else
                        await e.Channel.SendMessage("No trivia is running on this server.").ConfigureAwait(false);
                });
        }
    }
}
