using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System.Linq;

namespace NadekoBot.Modules.Administration.Commands
{
    class SelfCommands : DiscordCommand
    {
        public SelfCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "leave")
                .Description($"Makes Nadeko leave the server. Either name or id required. | `{Prefix}leave 123123123331`")
                .Parameter("arg", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var arg = e.GetArg("arg").Trim();
                    var server = NadekoBot.Client.Servers.FirstOrDefault(s => s.Id.ToString() == arg) ??
                                 NadekoBot.Client.FindServers(arg).FirstOrDefault();
                    if (server == null)
                    {
                        await e.Channel.SendMessage("Cannot find that server").ConfigureAwait(false);
                        return;
                    }
                    if (!server.IsOwner)
                    {
                        await server.Leave().ConfigureAwait(false);
                    }
                    else
                    {
                        await server.Delete().ConfigureAwait(false);
                    }
                    await NadekoBot.SendMessageToOwner("Left server " + server.Name).ConfigureAwait(false);
                });
        }
    }
}
