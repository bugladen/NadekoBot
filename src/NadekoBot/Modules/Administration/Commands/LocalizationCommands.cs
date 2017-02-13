using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration.Commands
{
    public partial class Administration
    {
        [Group]
        public class LocalizationCommands : ModuleBase
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task SetLocale([Remainder] string name)
            {
                CultureInfo ci = null;
                try
                {
                    if(name.Trim().ToLowerInvariant() == "default")
                    {
                        NadekoBot.Localization.RemoveGuildCulture(Context.Guild);
                    }
                    ci = new CultureInfo(name);
                    NadekoBot.Localization.SetGuildCulture(Context.Guild, ci);

                    await Context.Channel.SendConfirmAsync($"Your guild's locale is now {ci}.").ConfigureAwait(false);
                }
                catch(Exception) {

                    //_log.warn(ex);
                    await Context.Channel.SendConfirmAsync($"Failed setting locale. Revisit this command's help.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetDefaulLocale(string name)
            {
                CultureInfo ci = null;
                try
                {
                    if (name.Trim().ToLowerInvariant() == "default")
                    {
                        NadekoBot.Localization.RemoveGuildCulture(Context.Guild);
                    }
                    ci = new CultureInfo(name);
                    NadekoBot.Localization.SetGuildCulture(Context.Guild, ci);

                    await Context.Channel.SendConfirmAsync($"Your guild's locale is now {ci}.").ConfigureAwait(false);
                }
                catch (Exception)
                {
                    //_log.warn(ex);
                    await Context.Channel.SendConfirmAsync($"Failed setting locale. Revisit this command's help.").ConfigureAwait(false);
                }
            }
        }
    }
}
