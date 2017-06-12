using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services.Utility;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class VerboseCommandErrors : NadekoSubmodule
        {
            private readonly VerboseErrorsService _ves;

            public VerboseCommandErrors(VerboseErrorsService ves)
            {
                _ves = ves;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(Discord.GuildPermission.ManageMessages)]
            public async Task VerboseError()
            {
                var state = _ves.ToggleVerboseErrors(Context.Guild.Id);

                if (state)
                    await ReplyConfirmLocalized("verbose_errors_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("verbose_errors_disabled").ConfigureAwait(false);
            }
        }
    }
}
