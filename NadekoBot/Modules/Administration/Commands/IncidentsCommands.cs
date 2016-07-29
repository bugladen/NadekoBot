using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Modules.Permissions.Classes;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class IncidentsCommands : DiscordCommand
    {
        public IncidentsCommands(DiscordModule module) : base(module) { }
        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "listincidents")
                .Alias(Prefix + "lin")
                .Description($"List all UNREAD incidents and flags them as read. | `{Prefix}lin`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(async e =>
                {
                    var sid = (long)e.Server.Id;
                    var incs = DbHandler.Instance.FindAll<Incident>(i => i.ServerId == sid && i.Read == false);
                    DbHandler.Instance.Connection.UpdateAll(incs.Select(i => { i.Read = true; return i; }));

                    await e.User.SendMessage(string.Join("\n----------------------", incs.Select(i => i.Text)));
                });

            cgb.CreateCommand(Module.Prefix + "listallincidents")
                .Alias(Prefix + "lain")
                .Description($"Sends you a file containing all incidents and flags them as read. | `{Prefix}lain`")
                .AddCheck(SimpleCheckers.ManageServer())
                .Do(async e =>
                {
                    var sid = (long)e.Server.Id;
                    var incs = DbHandler.Instance.FindAll<Incident>(i => i.ServerId == sid);
                    DbHandler.Instance.Connection.UpdateAll(incs.Select(i => { i.Read = true; return i; }));
                    var data = string.Join("\n----------------------\n", incs.Select(i => i.Text));
                    MemoryStream ms = new MemoryStream();
                    var sw = new StreamWriter(ms);
                    sw.WriteLine(data);
                    sw.Flush();
                    sw.BaseStream.Position = 0;
                    await e.User.SendFile("incidents.txt", sw.BaseStream);
                });
        }
    }
}
