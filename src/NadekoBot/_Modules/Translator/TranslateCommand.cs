using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Translator.Helpers;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Translator
{
    class TranslateCommand : DiscordCommand
    {
        public TranslateCommand(DiscordModule module) : base(module) { }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "translate")
                .Alias(Module.Prefix + "trans")
                .Description($"Translates from>to text. From the given language to the destiation language. | `{Module.Prefix}trans en>fr Hello`")
                .Parameter("langs", ParameterType.Required)
                .Parameter("text", ParameterType.Unparsed)
                .Do(TranslateFunc());
        }
        private GoogleTranslator t = new GoogleTranslator();
        private Func<CommandEventArgs, Task> TranslateFunc() => async e =>
        {
            try
            {
                await e.Channel.SendIsTyping().ConfigureAwait(false);
                string from = e.GetArg("langs").ToLowerInvariant().Split('>')[0];
                string to = e.GetArg("langs").ToLowerInvariant().Split('>')[1];
                var text = e.GetArg("text")?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                string translation = await t.Translate(text, from, to).ConfigureAwait(false);
                await e.Channel.SendMessage(translation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await e.Channel.SendMessage("Bad input format, or something went wrong...").ConfigureAwait(false);
            }

        };
    }
}
