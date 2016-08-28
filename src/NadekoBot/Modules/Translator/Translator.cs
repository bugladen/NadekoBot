using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using NadekoBot.Services;
using Discord.WebSocket;

namespace NadekoBot.Modules.Translator
{
    [Module("~", AppendSpace = false)]
    public class Translator : DiscordModule
    {
        public Translator(ILocalization loc, CommandService cmds, DiscordSocketClient client) : base(loc, cmds, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Translate(IUserMessage umsg, string langs, [Remainder] string text = null)
        {
            var channel = (ITextChannel)umsg.Channel;

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

                await umsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
                string translation = await GoogleTranslator.Instance.Translate(text, from, to).ConfigureAwait(false);
                await channel.SendMessageAsync(translation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await channel.SendMessageAsync("Bad input format, or something went wrong...").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Translangs(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendTableAsync(GoogleTranslator.Instance.Languages, str => $"{str,-15}", columns: 3);
        }

    }
}
