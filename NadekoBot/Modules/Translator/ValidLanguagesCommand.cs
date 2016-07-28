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
                .Description("List the valid languages for translation. | `{Prefix}translangs` or `{Prefix}translangs language`")
                .Parameter("search", ParameterType.Optional)
                .Do(ListLanguagesFunc());
        }
        private Func<CommandEventArgs, Task> ListLanguagesFunc() => async e =>
        {
            try
            {
                GoogleTranslator.EnsureInitialized();
                string s = e.GetArg("search");
                string ret = "";
                foreach (string key in GoogleTranslator._languageModeMap.Keys)
                {
                    if (!s.Equals(""))
                    {
                        if (key.ToLower().Contains(s))
                        {
                            ret += " " + key + ";";
                        }
                    }
                    else
                    {
                        ret += " " + key + ";";
                    }
                }
                await e.Channel.SendMessage(ret).ConfigureAwait(false);
            }
            catch
            {
                await e.Channel.SendMessage("Bad input format, or sth went wrong...").ConfigureAwait(false);
            }

        };
    }
}
