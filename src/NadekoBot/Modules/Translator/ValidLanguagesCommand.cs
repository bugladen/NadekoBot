using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Translator.Helpers;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Translator
{
    class ValidLanguagesCommand : DiscordCommand
    {
        public ValidLanguagesCommand(DiscordModule module) : base(module) { }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "translangs")
                .Description($"List the valid languages for translation. | `{Prefix}translangs` or `{Prefix}translangs language`")
                .Parameter("search", ParameterType.Optional)
                .Do(ListLanguagesFunc());
        }
        private Func<CommandEventArgs, Task> ListLanguagesFunc() => async e =>
        {

        };
    }
}
