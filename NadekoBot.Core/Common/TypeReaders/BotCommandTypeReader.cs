using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Core.Services;
using NadekoBot.Modules.CustomReactions.Services;
using NadekoBot.Core.Common.TypeReaders;
using Discord.WebSocket;

namespace NadekoBot.Common.TypeReaders
{
    public class CommandTypeReader : NadekoTypeReader<CommandInfo>
    {
        public CommandTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
        }

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var _cmds = ((INServiceProvider)services).GetService<CommandService>();
            var _cmdHandler = ((INServiceProvider)services).GetService<CommandHandler>();
            input = input.ToUpperInvariant();
            var prefix = _cmdHandler.GetPrefix(context.Guild);
            if (!input.StartsWith(prefix.ToUpperInvariant()))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            input = input.Substring(prefix.Length);

            var cmd = _cmds.Commands.FirstOrDefault(c => 
                c.Aliases.Select(a => a.ToUpperInvariant()).Contains(input));
            if (cmd == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No such command found."));

            return Task.FromResult(TypeReaderResult.FromSuccess(cmd));
        }
    }

    public class CommandOrCrTypeReader : NadekoTypeReader<CommandOrCrInfo>
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmds;
        public CommandOrCrTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
            _client = client;
            _cmds = cmds;
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            input = input.ToUpperInvariant();

            var _crs = ((INServiceProvider)services).GetService<CustomReactionsService>();

            if (_crs.GlobalReactions.Any(x => x.Trigger.ToUpperInvariant() == input))
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
            }
            var guild = context.Guild;
            if (guild != null)
            {
                if (_crs.GuildReactions.TryGetValue(guild.Id, out var crs))
                {
                    if (crs.Concat(_crs.GlobalReactions).Any(x => x.Trigger.ToUpperInvariant() == input))
                    {
                        return TypeReaderResult.FromSuccess(new CommandOrCrInfo(input));
                    }
                }
            }

            var cmd = await new CommandTypeReader(_client, _cmds).ReadAsync(context, input, services);
            if (cmd.IsSuccess)
            {
                return TypeReaderResult.FromSuccess(new CommandOrCrInfo(((CommandInfo)cmd.Values.First().Value).Name));
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
