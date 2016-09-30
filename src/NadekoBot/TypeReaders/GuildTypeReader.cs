using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace NadekoBot.TypeReaders
{
    public class GuildTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(IUserMessage context, string input)
        {
            input = input.Trim().ToLowerInvariant();
            var guild = NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Id.ToString().Trim().ToLowerInvariant() == input) ?? //by id
                NadekoBot.Client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToLowerInvariant() == input);//by name

            if (guild != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(guild));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No guild by that name or Id found"));
        }
    }
}
