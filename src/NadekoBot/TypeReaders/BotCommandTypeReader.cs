using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.TypeReaders
{
    public class CommandTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            var cmd = NadekoBot.CommandService.Commands.FirstOrDefault(c => 
                c.Aliases.Select(a => a.ToUpperInvariant()).Contains(input));
            if (cmd == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(cmd));
        }
    }
}
