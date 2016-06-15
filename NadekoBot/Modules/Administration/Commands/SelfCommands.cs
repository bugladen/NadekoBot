using NadekoBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Modules.Permissions.Classes;

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
                .Description("Makes Nadeko leave the server. Either name or id required.\n**Usage**:.leave NSFW")
                .Parameter("arg", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    var arg = e.GetArg("arg")?.Trim();
                    var server = NadekoBot.Client.Servers.FirstOrDefault(s => s.Id.ToString() == arg) ??
                                 NadekoBot.Client.FindServers(arg.Trim()).FirstOrDefault();
                    if (server == null)
                    {
                        await e.Channel.SendMessage("Cannot find that server").ConfigureAwait(false);
                        return;
                    }
                    if (!server.IsOwner)
                    {
                        await server.Leave();
                    }
                    else
                    {
                        await server.Delete();
                    }
                    await NadekoBot.SendMessageToOwner("Left server " + server.Name);
                });
        }
    }
}
