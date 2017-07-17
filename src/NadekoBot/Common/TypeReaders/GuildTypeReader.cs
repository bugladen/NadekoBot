using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace NadekoBot.Common.TypeReaders
{
    public class GuildTypeReader : TypeReader
    {
        private readonly DiscordSocketClient _client;

        public GuildTypeReader(DiscordSocketClient client)
        {
            _client = client;
        }
        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider _)
        {
            input = input.Trim().ToLowerInvariant();
            var guilds = _client.Guilds;
            var guild = guilds.FirstOrDefault(g => g.Id.ToString().Trim().ToLowerInvariant() == input) ?? //by id
                        guilds.FirstOrDefault(g => g.Name.Trim().ToLowerInvariant() == input); //by name

            if (guild != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(guild));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No guild by that name or Id found"));
        }
    }
}
