using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Games.Common;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class SpeedTypingCommands : NadekoSubmodule<GamesService>
        {
            private readonly GamesService _games;
            private readonly DiscordSocketClient _client;

            public SpeedTypingCommands(DiscordSocketClient client, GamesService games)
            {
                _games = games;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStart()
            {
                var channel = (ITextChannel)Context.Channel;

                var game = _service.RunningContests.GetOrAdd(channel.Guild.Id, id => new TypingGame(_games, _client, channel, Prefix));

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
                if (_service.RunningContests.TryRemove(channel.Guild.Id, out TypingGame game))
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
                if (string.IsNullOrWhiteSpace(text))
                    return;

                _games.AddTypingArticle(Context.User, text);                

                await channel.SendConfirmAsync("Added new article for typing game.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Typelist(int page = 1)
            {
                var channel = (ITextChannel)Context.Channel;

                if (page < 1)
                    return;

                var articles = _games.TypingArticles.Skip((page - 1) * 15).Take(15).ToArray();

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
                if (index < 0 || index >= _games.TypingArticles.Count)
                    return;

                var removed = _games.TypingArticles[index];
                _games.TypingArticles.RemoveAt(index);

                File.WriteAllText(_games.TypingArticlesPath, JsonConvert.SerializeObject(_games.TypingArticles));

                await channel.SendConfirmAsync($"`Removed typing article:` #{index + 1} - {removed.Text.TrimTo(50)}")
                             .ConfigureAwait(false);
            }
        }
    }
}