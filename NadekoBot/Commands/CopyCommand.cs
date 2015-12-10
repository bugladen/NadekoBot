using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot
{
    class CopyCommand : DiscordCommand
    {
        private List<long> CopiedUsers;

        public CopyCommand() : base()
        {
            CopiedUsers = new List<long>();
            client.MessageReceived += Client_MessageReceived;
        }

        private async void Client_MessageReceived(object sender, Discord.MessageEventArgs e)
        {
            if (CopiedUsers.Contains(e.User.Id)) {
                await e.Send( e.Message.Text);
            }
        }

        public override Func<CommandEventArgs, Task> DoFunc()
        {
            return async e =>
            {
                if (CopiedUsers.Contains(e.User.Id)) return;

                CopiedUsers.Add(e.User.Id);
                await e.Send( " I'll start copying you now.");
                return;
            };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("copyme")
                .Alias("cm")
                .Description("Nadeko starts copying everything you say. Disable with cs")
                .Do(DoFunc());

            cgb.CreateCommand("cs")
                .Alias("copystop")
                .Description("Nadeko stops copying you")
                .Do(StopCopy());
        }

        private Func<CommandEventArgs, Task> StopCopy()
        {
            return async e =>
            {
                if (!CopiedUsers.Contains(e.User.Id)) return;

                CopiedUsers.Remove(e.User.Id);
                await e.Send( " I wont copy anymore.");
                return;
            };
        }
    }
}
