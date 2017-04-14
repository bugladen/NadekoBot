using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Modules.CustomReactions;
using NadekoBot.Services.Database.Models;

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

    public class CommandOrCrTypeReader : CommandTypeReader
    {
        public override async Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            
            if (CustomReactions.GlobalReactions.Any(x => x.Trigger.ToUpperInvariant() == input))
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
            }
            var guild = context.Guild;
            if (guild != null)
            {
                CustomReaction[] crs;
                if (CustomReactions.GuildReactions.TryGetValue(guild.Id, out crs))
                {
                    if (crs.Any(x => x.Trigger.ToUpperInvariant() == input))
                    {
                        return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
                    }
                }
            }

            var cmd = await base.Read(context, input);
            if (cmd.IsSuccess)
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Aliases.First()));
            }
            return TypeReaderResult.FromError(CommandError.ParseFailed, "No such command or cr found.");
        }
    }

    public class CommandOrCrInfo
    {
        public string Name { get; set; }

        public CommandOrCrInfo(string input)
        {
            this.Name = input;
        }
    }
}
