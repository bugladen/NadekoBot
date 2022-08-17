﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Common.Trivia;
using NadekoBot.Modules.Games.Services;
using NadekoBot.Core.Common;
using NadekoBot.Core.Modules.Games.Common.Trivia;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class TriviaCommands : NadekoSubmodule<GamesService>
        {
            private readonly IDataCache _cache;
            private readonly ICurrencyService _cs;
            private readonly DiscordSocketClient _client;

            public TriviaCommands(DiscordSocketClient client, IDataCache cache, ICurrencyService cs)
            {
                _cache = cache;
                _cs = cs;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            [NadekoOptionsAttribute(typeof(TriviaOptions))]
            public Task Trivia(params string[] args)
                => InternalTrivia(args);

            public async Task InternalTrivia(params string[] args)
            {
                var channel = (ITextChannel)ctx.Channel;

                var (opts, _) = OptionsParser.ParseFrom(new TriviaOptions(), args);

                if (Bc.BotConfig.MinimumTriviaWinReq > 0 && Bc.BotConfig.MinimumTriviaWinReq > opts.WinRequirement)
                {
                    return;
                }
                var trivia = new TriviaGame(Strings, _client, Bc, _cache, _cs, channel.Guild, channel, opts);
                if (_service.RunningTrivias.TryAdd(channel.Guild.Id, trivia))
                {
                    try
                    {
                        await trivia.StartGame().ConfigureAwait(false);
                    }
                    finally
                    {
                        _service.RunningTrivias.TryRemove(channel.Guild.Id, out trivia);
                        await trivia.EnsureStopped().ConfigureAwait(false);
                    }
                    return;
                }

                await ctx.Channel.SendErrorAsync(GetText("trivia_already_running") + "\n" + trivia.CurrentQuestion)
                    .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tl()
            {
                var channel = (ITextChannel)ctx.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await channel.SendConfirmAsync(GetText("leaderboard"), trivia.GetLeaderboard()).ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalizedAsync("trivia_none").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Tq()
            {
                var channel = (ITextChannel)ctx.Channel;

                if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out TriviaGame trivia))
                {
                    await trivia.StopGame().ConfigureAwait(false);
                    return;
                }

                await ReplyErrorLocalizedAsync("trivia_none").ConfigureAwait(false);
            }
        }
    }
}