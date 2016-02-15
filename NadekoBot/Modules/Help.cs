using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Modules;

namespace NadekoBot.Modules {
    class Help : DiscordModule {

        public Help() : base() {
            commands.Add(new HelpCommand());
        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);
                commands.ForEach(com => com.Init(cgb));
            });
        }
    }
}
