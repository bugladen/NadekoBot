using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Common.TypeReaders;
using Discord.WebSocket;

namespace NadekoBot.Common.TypeReaders
{
    public class ModuleTypeReader : NadekoTypeReader<ModuleInfo>
    {
        private readonly CommandService _cmds;

        public ModuleTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider _)
        {
            input = input.ToUpperInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToUpperInvariant() == input)?.Key;
            if (module == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such module found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(module));
        }
    }

    public class ModuleOrCrTypeReader : NadekoTypeReader<ModuleOrCrInfo>
    {
        private readonly CommandService _cmds;

        public ModuleOrCrTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
            _cmds = cmds;
        }

        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider _)
        {
            input = input.ToLowerInvariant();
            var module = _cmds.Modules.GroupBy(m => m.GetTopLevelModule()).FirstOrDefault(m => m.Key.Name.ToLowerInvariant() == input)?.Key;
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
