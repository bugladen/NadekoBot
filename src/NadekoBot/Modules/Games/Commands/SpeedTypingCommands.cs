using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Commands.Models;
using NadekoBot.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        public class TypingGame
        {
            public const float WORD_VALUE = 4.5f;
            public ITextChannel Channel { get; }
            public string CurrentSentence { get; private set; }
            public bool IsActive { get; private set; }
            private readonly Stopwatch sw;
            private readonly List<ulong> finishedUserIds;
            private Logger _log { get; }

            public TypingGame(ITextChannel channel)
            {
                _log = LogManager.GetCurrentClassLogger();
                this.Channel = channel;
                IsActive = false;
                sw = new Stopwatch();
                finishedUserIds = new List<ulong>();
            }

            public async Task<bool> Stop()
            {
                if (!IsActive) return false;
                NadekoBot.Client.MessageReceived -= AnswerReceived;
                finishedUserIds.Clear();
                IsActive = false;
                sw.Stop();
                sw.Reset();
                try { await Channel.SendConfirmAsync("Typing contest stopped.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                return true;
            }

            public async Task Start()
            {
                if (IsActive) return; // can't start running game
                IsActive = true;
                CurrentSentence = GetRandomSentence();
                var i = (int)(CurrentSentence.Length / WORD_VALUE * 1.7f);
                try
                {
                    await Channel.SendConfirmAsync($@":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.").ConfigureAwait(false);


                    var msg = await Channel.SendMessageAsync("Starting new typing contest in **3**...").ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                    try
                    {
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **2**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **1**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }

                    await msg.ModifyAsync(m => m.Content = Format.Bold(Format.Sanitize(CurrentSentence.Replace(" ", " \x200B")).SanitizeMentions())).ConfigureAwait(false);
                    sw.Start();
                    HandleAnswers();

                    while (i > 0)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        i--;
                        if (!IsActive)
                            return;
                    }

                }
                catch { }
                finally
                {
                    await Stop().ConfigureAwait(false);
                }
            }

            public string GetRandomSentence()
            {
                if (SpeedTypingCommands.TypingArticles.Any())
                    return SpeedTypingCommands.TypingArticles[new NadekoRandom().Next(0, SpeedTypingCommands.TypingArticles.Count)].Text;
                else
                    return $"No typing articles found. Use {NadekoBot.ModulePrefixes[typeof(Games).Name]}typeadd command to add a new article for typing.";

            }

            private void HandleAnswers()
            {
                NadekoBot.Client.MessageReceived += AnswerReceived;
            }

            private async Task AnswerReceived(SocketMessage imsg)
            {
                try
                {
                    if (imsg.Author.IsBot)
                        return;
                    var msg = imsg as SocketUserMessage;
                    if (msg == null)
                        return;

                    if (this.Channel == null || this.Channel.Id != msg.Channel.Id) return;

                    var guess = msg.Content;

                    var distance = CurrentSentence.LevenshteinDistance(guess);
                    var decision = Judge(distance, guess.Length);
                    if (decision && !finishedUserIds.Contains(msg.Author.Id))
                    {
                        var elapsed = sw.Elapsed;
                        var wpm = CurrentSentence.Length / WORD_VALUE / elapsed.TotalSeconds * 60;
                        finishedUserIds.Add(msg.Author.Id);
                        await this.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle($"{msg.Author} finished the race!")
                            .AddField(efb => efb.WithName("Place").WithValue($"#{finishedUserIds.Count}").WithIsInline(true))
                            .AddField(efb => efb.WithName("WPM").WithValue($"{wpm:F1} *[{elapsed.TotalSeconds:F2}sec]*").WithIsInline(true))
                            .AddField(efb => efb.WithName("Errors").WithValue(distance.ToString()).WithIsInline(true)))
                                .ConfigureAwait(false);
                        if (finishedUserIds.Count % 4 == 0)
                        {
                            await this.Channel.SendConfirmAsync($":exclamation: A lot of people finished, here is the text for those still typing:\n\n**{Format.Sanitize(CurrentSentence.Replace(" ", " \x200B")).SanitizeMentions()}**").ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex) { _log.Warn(ex); }
            }

            private bool Judge(int errors, int textLength) => errors <= textLength / 25;

        }

        [Group]
        public class SpeedTypingCommands : NadekoSubmodule
        {
            public static List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

            private const string _typingArticlesPath = "data/typing_articles2.json";

            static SpeedTypingCommands()
            {
                try { TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(_typingArticlesPath)); } catch { }
            }
            public static ConcurrentDictionary<ulong, TypingGame> RunningContests = new ConcurrentDictionary<ulong, TypingGame>();

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStart()
            {
                var channel = (ITextChannel)Context.Channel;

                var game = RunningContests.GetOrAdd(channel.Guild.Id, id => new TypingGame(channel));

                if (game.IsActive)
                {
                    await channel.SendErrorAsync(
                            $"Contest already running in " +
                            $"{game.Channel.Mention} channel.")
                                .ConfigureAwait(false);
                }
                else
                {
                    await game.Start().ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStop()
            {
                var channel = (ITextChannel)Context.Channel;
                TypingGame game;
                if (RunningContests.TryRemove(channel.Guild.Id, out game))
                {
                    await game.Stop().ConfigureAwait(false);
                    return;
                }
                await channel.SendErrorAsync("No contest to stop on this channel.").ConfigureAwait(false);
            }


            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Typeadd([Remainder] string text)
            {
                var channel = (ITextChannel)Context.Channel;

                TypingArticles.Add(new TypingArticle
                {
                    Title = $"Text added on {DateTime.UtcNow} by {Context.User}",
                    Text = text.SanitizeMentions(),
                });

                File.WriteAllText(_typingArticlesPath, JsonConvert.SerializeObject(TypingArticles));

                await channel.SendConfirmAsync("Added new article for typing game.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Typelist(int page = 1)
            {
                var channel = (ITextChannel)Context.Channel;

                if (page < 1)
                    return;

                var articles = TypingArticles.Skip((page - 1) * 15).Take(15).ToArray();

                if (!articles.Any())
                {
                    await channel.SendErrorAsync($"{Context.User.Mention} `No articles found on that page.`").ConfigureAwait(false);
                    return;
                }
                var i = (page - 1) * 15;
                await channel.SendConfirmAsync("List of articles for Type Race", string.Join("\n", articles.Select(a => $"`#{++i}` - {a.Text.TrimTo(50)}")))
                             .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Typedel(int index)
            {
                var channel = (ITextChannel)Context.Channel;

                index -= 1;
                if (index < 0 || index >= TypingArticles.Count)
                    return;

                var removed = TypingArticles[index];
                TypingArticles.RemoveAt(index);

                File.WriteAllText(_typingArticlesPath, JsonConvert.SerializeObject(TypingArticles));

                await channel.SendConfirmAsync($"`Removed typing article:` #{index + 1} - {removed.Text.TrimTo(50)}")
                             .ConfigureAwait(false);
            }
        }
    }
}