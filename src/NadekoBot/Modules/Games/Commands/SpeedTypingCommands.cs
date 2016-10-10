using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        public class TypingGame
        {
            public const float WORD_VALUE = 4.5f;
            private readonly ITextChannel channel;
            public string CurrentSentence;
            public bool IsActive;
            private readonly Stopwatch sw;
            private readonly List<ulong> finishedUserIds;
            private Logger _log { get; }

            public TypingGame(ITextChannel channel)
            {
                _log = LogManager.GetCurrentClassLogger();
                this.channel = channel;
                IsActive = false;
                sw = new Stopwatch();
                finishedUserIds = new List<ulong>();
            }

            public ITextChannel Channel { get; set; }

            public async Task<bool> Stop()
            {
                if (!IsActive) return false;
                NadekoBot.Client.MessageReceived -= AnswerReceived;
                finishedUserIds.Clear();
                IsActive = false;
                sw.Stop();
                sw.Reset();
                try { await channel.SendMessageAsync("Typing contest stopped").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                    await channel.SendMessageAsync($@":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.").ConfigureAwait(false);


                    var msg = await channel.SendMessageAsync("Starting new typing contest in **3**...").ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                    try
                    {
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **2**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **1**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }

                    await msg.ModifyAsync(m => m.Content = $"**{Format.Sanitize(CurrentSentence.Replace(" ", " \x200B")).SanitizeMentions()}").ConfigureAwait(false);
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
                using (var uow = DbHandler.UnitOfWork())
                {
                    return uow.TypingArticles.GetRandom()?.Text ?? $"No typing articles found. Use {NadekoBot.ModulePrefixes[typeof(Games).Name]}typeadd command to add a new article for typing.";
                }

            }

            private void HandleAnswers()
            {
                NadekoBot.Client.MessageReceived += AnswerReceived;
            }

            private Task AnswerReceived(IMessage imsg)
            {
                if (imsg.Author.IsBot)
                    return Task.CompletedTask;
                var msg = imsg as IUserMessage;
                if (msg == null)
                    return Task.CompletedTask;
                var t = Task.Run(async () =>
                {
                    try
                    {
                        if (channel == null || channel.Id != channel.Id || msg.Author.Id == NadekoBot.Client.GetCurrentUser().Id) return;

                        var guess = msg.Content;

                        var distance = CurrentSentence.LevenshteinDistance(guess);
                        var decision = Judge(distance, guess.Length);
                        if (decision && !finishedUserIds.Contains(msg.Author.Id))
                        {
                            finishedUserIds.Add(msg.Author.Id);
                            await channel.SendMessageAsync($"{msg.Author.Mention} finished in **{sw.Elapsed.Seconds}** seconds with { distance } errors, **{ CurrentSentence.Length / WORD_VALUE / sw.Elapsed.Seconds * 60 }** WPM!").ConfigureAwait(false);
                            if (finishedUserIds.Count % 2 == 0)
                            {
                                await channel.SendMessageAsync($":exclamation: `A lot of people finished, here is the text for those still typing:`\n\n**{Format.Sanitize(CurrentSentence.Replace(" ", " \x200B")).SanitizeMentions()}**").ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            private bool Judge(int errors, int textLength) => errors <= textLength / 25;

        }

        [Group]
        public class SpeedTypingCommands
        {

            public static ConcurrentDictionary<ulong, TypingGame> RunningContests;

            public SpeedTypingCommands()
            {
                RunningContests = new ConcurrentDictionary<ulong, TypingGame>();
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStart(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;

                var game = RunningContests.GetOrAdd(channel.Guild.Id, id => new TypingGame(channel));

                if (game.IsActive)
                {
                    await channel.SendMessageAsync(
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
            public async Task TypeStop(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                TypingGame game;
                if (RunningContests.TryRemove(channel.Guild.Id, out game))
                {
                    await game.Stop().ConfigureAwait(false);
                    return;
                }
                await channel.SendMessageAsync("No contest to stop on this channel.").ConfigureAwait(false);
            }

            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Typeadd(IUserMessage imsg, [Remainder] string text)
            {
                var channel = (ITextChannel)imsg.Channel;

                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.TypingArticles.Add(new Services.Database.Models.TypingArticle
                    {
                        Author = imsg.Author.Username,
                        Text = text.SanitizeMentions(),
                    });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await channel.SendMessageAsync("Added new article for typing game.").ConfigureAwait(false);
            }
        }
    }
}