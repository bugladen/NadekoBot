using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using NadekoBot.Services;

namespace NadekoBot.Modules.Translator
{
    public class TranslatorModule : DiscordModule
    {
        public TranslatorModule(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Translate(IMessage imsg, string langs, [Remainder] string text)
        {
            var channel = imsg.Channel as IGuildChannel;

            try
            {
                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    return;
                string from = langarr[0];
                string to = langarr[1];
                text = text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                await imsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
                string translation = await GoogleTranslator.Instance.Translate(text, from, to).ConfigureAwait(false);
                await imsg.Channel.SendMessageAsync(translation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await imsg.Channel.SendMessageAsync("Bad input format, or something went wrong...").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Translangs(IMessage imsg)
        {
            var channel = imsg.Channel as IGuildChannel;

            await imsg.Channel.SendTableAsync(GoogleTranslator.Instance.Languages, str => str, columns: 4);
        }

    }
}
