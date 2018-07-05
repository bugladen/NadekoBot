using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PrefixCommands : NadekoSubmodule
        {
            [NadekoCommand, Usage, Description, Aliases]
            [Priority(1)]
            public new async Task Prefix()
            {
                await ReplyConfirmLocalized("prefix_current", Format.Code(CmdHandler.GetPrefix(Context.Guild))).ConfigureAwait(false);
                return;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(0)]
            public new async Task Prefix([Remainder]string prefix)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                    return;

                var oldPrefix = base.Prefix;
                var newPrefix = CmdHandler.SetPrefix(Context.Guild, prefix);

                await ReplyConfirmLocalized("prefix_new", Format.Code(oldPrefix), Format.Code(newPrefix)).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task DefPrefix([Remainder]string prefix = null)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    await ReplyConfirmLocalized("defprefix_current", CmdHandler.DefaultPrefix).ConfigureAwait(false);
                    return;
                }

                var oldPrefix = CmdHandler.DefaultPrefix;
                var newPrefix = CmdHandler.SetDefaultPrefix(prefix);

                await ReplyConfirmLocalized("defprefix_new", Format.Code(oldPrefix), Format.Code(newPrefix)).ConfigureAwait(false);
            }
        }
    }
}
