using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Modules.Games.Commands.Hangman;

namespace NadekoBot.Modules.Games.Hangman
{
    public class HangmanTermPool
    {
        const string termsPath = "data/hangman3.json";
        public static IReadOnlyDictionary<string, HangmanObject[]> data { get; }
        static HangmanTermPool()
        {
            try
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, HangmanObject[]>>(File.ReadAllText(termsPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static HangmanObject GetTerm(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException(nameof(type));

            type = type.Trim();

            var rng = new NadekoRandom();

            if (type == "All") {
                var keys = data.Keys.ToArray();
                type = keys[rng.Next(0, keys.Length)];
            }

            HangmanObject[] termTypes;
            data.TryGetValue(type, out termTypes);

            if (termTypes == null || termTypes.Length == 0)
                return null;

            return termTypes[rng.Next(0, termTypes.Length)];
        }
    }

    public class HangmanGame: IDisposable
    {
        private readonly Logger _log;

        public IMessageChannel GameChannel { get; }
        public HashSet<char> Guesses { get; } = new HashSet<char>();
        public HangmanObject Term { get; private set; }
        public uint Errors { get; private set; } = 0;
        public uint MaxErrors { get; } = 6;
        public uint MessagesSinceLastPost { get; private set; } = 0;
        public string ScrambledWord => "`" + String.Concat(Term.Word.Select(c =>
        {
            if (c == ' ')
                return " \u2000";
            if (!(char.IsLetter(c) || char.IsDigit(c)))
                 return $" {c}";

             c = char.ToUpperInvariant(c);
             return Guesses.Contains(c) ? $" {c}" : " ◯";
         })) + "`";

        public bool GuessedAll => Guesses.IsSupersetOf(Term.Word.ToUpperInvariant()
                                                           .Where(c => char.IsLetter(c) || char.IsDigit(c)));

        public string TermType { get; }

        public event Action<HangmanGame> OnEnded;

        public HangmanGame(IMessageChannel channel, string type)
        {
            _log = LogManager.GetCurrentClassLogger();
            this.GameChannel = channel;
            this.TermType = type.ToTitleCase();
        }

        public void Start()
        {
            this.Term = HangmanTermPool.GetTerm(TermType);

            if (this.Term == null)
                throw new KeyNotFoundException("Can't find a term with that type. Use hangmanlist command.");
            // start listening for answers when game starts
            NadekoBot.Client.MessageReceived += PotentialGuess;
        }

        public async Task End()
        {
            NadekoBot.Client.MessageReceived -= PotentialGuess;
            OnEnded(this);
            var toSend = "Game ended. You **" + (Errors >= MaxErrors ? "LOSE" : "WIN") + "**!\n" + GetHangman();
            var embed = new EmbedBuilder().WithTitle("Hangman Game")
                                          .WithDescription(toSend)
                                          .AddField(efb => efb.WithName("It was").WithValue(Term.Word))
                                          .WithImageUrl(Term.ImageUrl)
                                          .WithFooter(efb => efb.WithText(string.Join(" ", Guesses)));
            if (Errors >= MaxErrors)
                await GameChannel.EmbedAsync(embed.WithErrorColor()).ConfigureAwait(false);
            else
                await GameChannel.EmbedAsync(embed.WithOkColor()).ConfigureAwait(false);
        }

        private async Task PotentialGuess(SocketMessage msg)
        {
            try
            {
                if (!(msg is SocketUserMessage))
                    return;

                if (msg.Channel != GameChannel)
                    return; // message's channel has to be the same as game's
                if (msg.Content.Length == 1) // message must be 1 char long
                {
                    if (++MessagesSinceLastPost > 10)
                    {
                        MessagesSinceLastPost = 0;
                        try
                        {
                            await GameChannel.SendConfirmAsync("Hangman Game",
                                ScrambledWord + "\n" + GetHangman(),
                                footer: string.Join(" ", Guesses)).ConfigureAwait(false);
                        }
                        catch { }
                    }

                    if (!(char.IsLetter(msg.Content[0]) || char.IsDigit(msg.Content[0])))// and a letter or a digit
                        return;

                    var guess = char.ToUpperInvariant(msg.Content[0]);
                    if (Guesses.Contains(guess))
                    {
                        MessagesSinceLastPost = 0;
                        ++Errors;
                        if (Errors < MaxErrors)
                            await GameChannel.SendErrorAsync("Hangman Game", $"{msg.Author} Letter `{guess}` has already been used.\n" + ScrambledWord + "\n" + GetHangman(),
                                footer: string.Join(" ", Guesses)).ConfigureAwait(false);
                        else
                            await End().ConfigureAwait(false);
                        return;
                    }

                    Guesses.Add(guess);

                    if (Term.Word.ToUpperInvariant().Contains(guess))
                    {
                        if (GuessedAll)
                        {
                            try { await GameChannel.SendConfirmAsync("Hangman Game", $"{msg.Author} guessed a letter `{guess}`!").ConfigureAwait(false); } catch { }

                            await End().ConfigureAwait(false);
                            return;
                        }
                        MessagesSinceLastPost = 0;
                        try
                        {
                            await GameChannel.SendConfirmAsync("Hangman Game", $"{msg.Author} guessed a letter `{guess}`!\n" + ScrambledWord + "\n" + GetHangman(),
                          footer: string.Join(" ", Guesses)).ConfigureAwait(false);
                        }
                        catch { }

                    }
                    else
                    {
                        MessagesSinceLastPost = 0;
                        ++Errors;
                        if (Errors < MaxErrors)
                            await GameChannel.SendErrorAsync("Hangman Game", $"{msg.Author} Letter `{guess}` does not exist.\n" + ScrambledWord + "\n" + GetHangman(),
                                footer: string.Join(" ", Guesses)).ConfigureAwait(false);
                        else
                            await End().ConfigureAwait(false);
                    }

                }
            }
            catch (Exception ex) { _log.Warn(ex); }
        }

        public string GetHangman() => $@". ┌─────┐
.┃...............┋
.┃...............┋
.┃{(Errors > 0 ? ".............😲" : "")}
.┃{(Errors > 1 ? "............./" : "")} {(Errors > 2 ? "|" : "")} {(Errors > 3 ? "\\" : "")}
.┃{(Errors > 4 ? "............../" : "")} {(Errors > 5 ? "\\" : "")}
/-\";

        public void Dispose()
        {
            NadekoBot.Client.MessageReceived -= PotentialGuess;
            OnEnded = null;
        }
    }
}