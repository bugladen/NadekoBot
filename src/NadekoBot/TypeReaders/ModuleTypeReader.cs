using Discord.Commands;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.TypeReaders
{
    public class ModuleTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            var module = NadekoBot.CommandService.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToUpperInvariant() == input)?.Key;
            if (module == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(module));
        }
    }

    public class ModuleOrCrTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            input = input.ToLowerInvariant();
            var module = NadekoBot.CommandService.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToLowerInvariant() == input)?.Key;
            if (module == null && input != "actualcustomreactions")
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(new ModuleOrCrInfo
            {
                Name = input,
            }));
        }
    }

    public class ModuleOrCrInfo
    {
        public string Name { get; set; }
    }
}
