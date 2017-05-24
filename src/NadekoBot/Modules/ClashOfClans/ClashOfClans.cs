using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Attributes;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Services.ClashOfClans;

namespace NadekoBot.Modules.ClashOfClans
{
    public class ClashOfClans : NadekoTopLevelModule
    {
        private readonly ClashOfClansService _service;

        public ClashOfClans(ClashOfClansService service)
        {
            _service = service;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CreateWar(int size, [Remainder] string enemyClan = null)
        {
            if (string.IsNullOrWhiteSpace(enemyClan))
                return;

            if (size < 10 || size > 50 || size % 5 != 0)
            {
                await ReplyErrorLocalized("invalid_size").ConfigureAwait(false);
                return;
            }
            List<ClashWar> wars;
            if (!_service.ClashWars.TryGetValue(Context.Guild.Id, out wars))
            {
                wars = new List<ClashWar>();
                if (!_service.ClashWars.TryAdd(Context.Guild.Id, wars))
                    return;
            }


            var cw = await _service.CreateWar(enemyClan, size, Context.Guild.Id, Context.Channel.Id);

            wars.Add(cw);
            await ReplyErrorLocalized("war_created", _service.ShortPrint(cw)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task StartWar([Remainder] string number = null)
        {
            int num = 0;
            int.TryParse(number, out num);

            var warsInfo = _service.GetWarInfo(Context.Guild, num);
            if (warsInfo == null)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            try
            {
                war.Start();
                await ReplyConfirmLocalized("war_started", _service.ShortPrint(war)).ConfigureAwait(false);
            }
            catch
            {
                await ReplyErrorLocalized("war_already_started", _service.ShortPrint(war)).ConfigureAwait(false);
            }
            _service.SaveWar(war);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListWar([Remainder] string number = null)
        {

            // if number is null, print all wars in a short way
            if (string.IsNullOrWhiteSpace(number))
            {
                //check if there are any wars
                List<ClashWar> wars = null;
                _service.ClashWars.TryGetValue(Context.Guild.Id, out wars);
                if (wars == null || wars.Count == 0)
                {
                    await ReplyErrorLocalized("no_active_wars").ConfigureAwait(false);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("**-------------------------**");
                for (var i = 0; i < wars.Count; i++)
                {
                    sb.AppendLine($"**#{i + 1}.**  `{GetText("enemy")}:` **{wars[i].EnemyClan}**");
                    sb.AppendLine($"\t\t`{GetText("size")}:` **{wars[i].Size} v {wars[i].Size}**");
                    sb.AppendLine("**-------------------------**");
                }
                await Context.Channel.SendConfirmAsync(GetText("list_active_wars"), sb.ToString()).ConfigureAwait(false);
                return;
            }
            var num = 0;
            int.TryParse(number, out num);
            //if number is not null, print the war needed
            var warsInfo = _service.GetWarInfo(Context.Guild, num);
            if (warsInfo == null)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            await Context.Channel.SendConfirmAsync(_service.Localize(war, "info_about_war", $"`{war.EnemyClan}` ({war.Size} v {war.Size})"), _service.ToPrettyString(war)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Claim(int number, int baseNumber, [Remainder] string other_name = null)
        {
            var warsInfo = _service.GetWarInfo(Context.Guild, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(other_name) ?
                Context.User.Username :
                other_name;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                _service.Call(war, usr, baseNumber - 1);
                _service.SaveWar(war);
                await ConfirmLocalized("claimed_base", Format.Bold(usr.ToString()), baseNumber, _service.ShortPrint(war)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish1(int number, int baseNumber = 0)
        {
            await FinishClaim(number, baseNumber - 1, 1);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish2(int number, int baseNumber = 0)
        {
            await FinishClaim(number, baseNumber - 1, 2);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish(int number, int baseNumber = 0)
        {
            await FinishClaim(number, baseNumber - 1);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task EndWar(int number)
        {
            var warsInfo = _service.GetWarInfo(Context.Guild, number);
            if (warsInfo == null)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            war.End();
            _service.SaveWar(war);
            await ReplyConfirmLocalized("war_ended", _service.ShortPrint(warsInfo.Item1[warsInfo.Item2])).ConfigureAwait(false);

            warsInfo.Item1.RemoveAt(warsInfo.Item2);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Unclaim(int number, [Remainder] string otherName = null)
        {
            var warsInfo = _service.GetWarInfo(Context.Guild, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(otherName) ?
                Context.User.Username :
                otherName;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                var baseNumber = _service.Uncall(war, usr);
                _service.SaveWar(war);
                await ReplyConfirmLocalized("base_unclaimed", usr, baseNumber + 1, _service.ShortPrint(war)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        private async Task FinishClaim(int number, int baseNumber, int stars = 3)
        {
            var warInfo = _service.GetWarInfo(Context.Guild, number);
            if (warInfo == null || warInfo.Item1.Count == 0)
            {
                await ReplyErrorLocalized("war_not_exist").ConfigureAwait(false);
                return;
            }
            var war = warInfo.Item1[warInfo.Item2];
            try
            {
                if (baseNumber == -1)
                {
                    baseNumber = _service.FinishClaim(war, Context.User.Username, stars);
                    _service.SaveWar(war);
                }
                else
                {
                    _service.FinishClaim(war, baseNumber, stars);
                }
                await ReplyConfirmLocalized("base_destroyed", baseNumber + 1, _service.ShortPrint(war)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync($"🔰 {ex.Message}").ConfigureAwait(false);
            }
        }
    }
}