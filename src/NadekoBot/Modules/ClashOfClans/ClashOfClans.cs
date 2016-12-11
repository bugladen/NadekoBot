using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Services;
using NadekoBot.Attributes;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System.Linq;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.ClashOfClans
{
    [NadekoModule("ClashOfClans", ",")]
    public class ClashOfClans : DiscordModule
    {
        public static ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; set; } = new ConcurrentDictionary<ulong, List<ClashWar>>();

        static ClashOfClans()
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                ClashWars = new ConcurrentDictionary<ulong, List<ClashWar>>(
                    uow.ClashOfClans
                        .GetAllWars()
                        .Select(cw =>
                        {
                            cw.Channel = NadekoBot.Client.GetGuild(cw.GuildId)
                                                         ?.GetTextChannel(cw.ChannelId);
                            return cw;
                        })
                        .Where(cw => cw?.Channel != null)
                        .GroupBy(cw => cw.GuildId)
                        .ToDictionary(g => g.Key, g => g.ToList()));
            }
        }
        public ClashOfClans() : base()
        {
        }

        private static async Task CheckWar(TimeSpan callExpire, ClashWar war)
        {
            var Bases = war.Bases;
            for (var i = 0; i < Bases.Count; i++)
            {
                if (Bases[i].CallUser == null) continue;
                if (!Bases[i].BaseDestroyed && DateTime.UtcNow - Bases[i].TimeAdded >= callExpire)
                {
                    Bases[i] = null;
                    try { await war.Channel.SendErrorAsync($"❗🔰**Claim from @{Bases[i].CallUser} for a war against {war.ShortPrint()} has expired.**").ConfigureAwait(false); } catch { }
            }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CreateWar(IUserMessage umsg, int size, [Remainder] string enemyClan = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            if (!(umsg.Author as IGuildUser).GuildPermissions.ManageChannels)
                return;

            if (string.IsNullOrWhiteSpace(enemyClan))
                return;

            if (size < 10 || size > 50 || size % 5 != 0)
            {
                await channel.SendErrorAsync("🔰 Not a Valid war size").ConfigureAwait(false);
                return;
            }
            List<ClashWar> wars;
            if (!ClashWars.TryGetValue(channel.Guild.Id, out wars))
            {
                wars = new List<ClashWar>();
                if (!ClashWars.TryAdd(channel.Guild.Id, wars))
                    return;
            }


            var cw = await CreateWar(enemyClan, size, channel.Guild.Id, umsg.Channel.Id);

            wars.Add(cw);
            await channel.SendConfirmAsync($"❗🔰**CREATED CLAN WAR AGAINST {cw.ShortPrint()}**").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task StartWar(IUserMessage umsg, [Remainder] string number = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            int num = 0;
            int.TryParse(number, out num);

            var warsInfo = GetWarInfo(umsg, num);
            if (warsInfo == null)
            {
                await channel.SendErrorAsync("🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            try
            {
                war.Start();
                await channel.SendConfirmAsync($"🔰**STARTED WAR AGAINST {war.ShortPrint()}**").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendErrorAsync($"🔰**WAR AGAINST {war.ShortPrint()} HAS ALREADY STARTED**").ConfigureAwait(false);
            }
            SaveWar(war);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ListWar(IUserMessage umsg, [Remainder] string number = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            // if number is null, print all wars in a short way
            if (string.IsNullOrWhiteSpace(number))
            {
                //check if there are any wars
                List<ClashWar> wars = null;
                ClashWars.TryGetValue(channel.Guild.Id, out wars);
                if (wars == null || wars.Count == 0)
                {
                    await channel.SendErrorAsync("🔰 **No active wars.**").ConfigureAwait(false);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("🔰 **LIST OF ACTIVE WARS**");
                sb.AppendLine("**-------------------------**");
                for (var i = 0; i < wars.Count; i++)
                {
                    sb.AppendLine($"**#{i + 1}.**  `Enemy:` **{wars[i].EnemyClan}**");
                    sb.AppendLine($"\t\t`Size:` **{wars[i].Size} v {wars[i].Size}**");
                    sb.AppendLine("**-------------------------**");
                }
                await channel.SendConfirmAsync(sb.ToString()).ConfigureAwait(false);
                return;

            }
            var num = 0;
            int.TryParse(number, out num);
            //if number is not null, print the war needed
            var warsInfo = GetWarInfo(umsg, num);
            if (warsInfo == null)
            {
                await channel.SendErrorAsync("🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            await channel.SendConfirmAsync(warsInfo.Item1[warsInfo.Item2].ToPrettyString()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Claim(IUserMessage umsg, int number, int baseNumber, [Remainder] string other_name = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            var warsInfo = GetWarInfo(umsg, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await channel.SendErrorAsync("🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(other_name) ?
                umsg.Author.Username :
                other_name;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                war.Call(usr, baseNumber - 1);
                SaveWar(war);
                await channel.SendConfirmAsync($"🔰**{usr}** claimed a base #{baseNumber} for a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendErrorAsync($"🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish1(IUserMessage umsg, int number, int baseNumber = 0)
        {
            var channel = (ITextChannel)umsg.Channel;
            await FinishClaim(umsg, number, baseNumber - 1, 1);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish2(IUserMessage umsg, int number, int baseNumber = 0)
        {
            var channel = (ITextChannel)umsg.Channel;
            await FinishClaim(umsg, number, baseNumber - 1, 2);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish(IUserMessage umsg, int number, int baseNumber = 0)
        {
            var channel = (ITextChannel)umsg.Channel;
            await FinishClaim(umsg, number, baseNumber - 1);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task EndWar(IUserMessage umsg, int number)
        {
            var channel = (ITextChannel)umsg.Channel;

            var warsInfo = GetWarInfo(umsg,number);
            if (warsInfo == null)
            {
                await channel.SendErrorAsync("🔰 That war does not exist.").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            war.End();
            SaveWar(war);
            await channel.SendConfirmAsync($"❗🔰**War against {warsInfo.Item1[warsInfo.Item2].ShortPrint()} ended.**").ConfigureAwait(false);

            var size = warsInfo.Item1[warsInfo.Item2].Size;
            warsInfo.Item1.RemoveAt(warsInfo.Item2);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Unclaim(IUserMessage umsg, int number, [Remainder] string otherName = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            var warsInfo = GetWarInfo(umsg, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await channel.SendErrorAsync("🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(otherName) ?
                umsg.Author.Username :
                otherName;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                var baseNumber = war.Uncall(usr);
                SaveWar(war);
                await channel.SendConfirmAsync($"🔰 @{usr} has **UNCLAIMED** a base #{baseNumber + 1} from a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendErrorAsync($"🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        private async Task FinishClaim(IUserMessage umsg, int number, int baseNumber, int stars = 3)
        {
            var channel = (ITextChannel)umsg.Channel;
            var warInfo = GetWarInfo(umsg, number);
            if (warInfo == null || warInfo.Item1.Count == 0)
            {
                await channel.SendErrorAsync("🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var war = warInfo.Item1[warInfo.Item2];
            try
            {
                if (baseNumber == -1)
                {
                    baseNumber = war.FinishClaim(umsg.Author.Username, stars);
                    SaveWar(war);
                }
                else
                {
                    war.FinishClaim(baseNumber, stars);
                }
                await channel.SendConfirmAsync($"❗🔰{umsg.Author.Mention} **DESTROYED** a base #{baseNumber + 1} in a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendErrorAsync($"🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        private static Tuple<List<ClashWar>, int> GetWarInfo(IUserMessage umsg, int num)
        {
            var channel = (ITextChannel)umsg.Channel;
            //check if there are any wars
            List<ClashWar> wars = null;
            ClashWars.TryGetValue(channel.Guild.Id, out wars);
            if (wars == null || wars.Count == 0)
            {
                return null;
            }
            // get the number of the war
            else if (num < 1 || num > wars.Count)
            {
                return null;
            }
            num -= 1;
            //get the actual war
            return new Tuple<List<ClashWar>, int>(wars, num);
        }

        public static async Task<ClashWar> CreateWar(string enemyClan, int size, ulong serverId, ulong channelId)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                var cw = new ClashWar
                {
                    EnemyClan = enemyClan,
                    Size = size,
                    Bases = new List<ClashCaller>(size),
                    GuildId = serverId,
                    ChannelId = channelId,
                    Channel = NadekoBot.Client.GetGuild(serverId)
                                       ?.GetTextChannel(channelId)
                };
                cw.Bases.Capacity = size;
                for (int i = 0; i < size; i++)
                {
                    cw.Bases.Add(new ClashCaller()
                    {
                        CallUser = null,
                        SequenceNumber = i,
                    });
                }
                Console.WriteLine(cw.Bases.Capacity);
                uow.ClashOfClans.Add(cw);
                await uow.CompleteAsync();
                return cw;
            }
        }

        public static void SaveWar(ClashWar cw)
        {
            if (cw.WarState == ClashWar.StateOfWar.Ended)
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    uow.ClashOfClans.Remove(cw);
                    uow.CompleteAsync();
                }
                return;
            }


            using (var uow = DbHandler.UnitOfWork())
            {
                uow.ClashOfClans.Update(cw);
                uow.CompleteAsync();
            }
        }
    }
}
