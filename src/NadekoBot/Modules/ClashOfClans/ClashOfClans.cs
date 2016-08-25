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
using NadekoBot.Services.Database;

//todo DB
namespace NadekoBot.Modules.ClashOfClans
{
    [Module(",", AppendSpace = false)]
    public class ClashOfClans : DiscordModule
    {
        public static ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; set; } = new ConcurrentDictionary<ulong, List<ClashWar>>();

        public ClashOfClans(ILocalization loc, CommandService cmds, IBotConfiguration config, DiscordSocketClient client) : base(loc, cmds, config, client)
        {
            using (var uow = DbHandler.UnitOfWork())
            {
                ClashWars = new ConcurrentDictionary<ulong, List<ClashWar>>(
                    uow.ClashOfClans
                        .GetAll()
                        .Select(cw => {
                            cw.Channel = NadekoBot.Client.GetGuilds()
                                                        .FirstOrDefault(s => s.Id == cw.GuildId)?
                                                        .GetChannels()
                                                        .FirstOrDefault(c => c.Id == cw.ChannelId)
                                                            as ITextChannel;
                            cw.Bases.Capacity = cw.Size;
                            return cw;
                        })
                        .GroupBy(cw => cw.GuildId)
                        .ToDictionary(g => g.Key, g => g.ToList()));
            }
        }

        private static async Task CheckWar(TimeSpan callExpire, ClashWar war)
        {
            var Bases = war.Bases;
            for (var i = 0; i < Bases.Capacity; i++)
            {
                if (Bases[i] == null) continue;
                if (!Bases[i].BaseDestroyed && DateTime.UtcNow - Bases[i].TimeAdded >= callExpire)
                {
                    await war.Channel.SendMessageAsync($"❗🔰**Claim from @{Bases[i].CallUser} for a war against {war.ShortPrint()} has expired.**").ConfigureAwait(false);
                    Bases[i] = null;
                }
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task CreateWar(IMessage imsg, int size, [Remainder] string enemyClan = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            if (!(imsg.Author as IGuildUser).GuildPermissions.ManageChannels)
                return;

            if (string.IsNullOrWhiteSpace(enemyClan))
                return;

            if (size < 10 || size > 50 || size % 5 != 0)
            {
                await channel.SendMessageAsync("💢🔰 Not a Valid war size").ConfigureAwait(false);
                return;
            }
            List<ClashWar> wars;
            if (!ClashWars.TryGetValue(channel.Guild.Id, out wars))
            {
                wars = new List<ClashWar>();
                if (!ClashWars.TryAdd(channel.Guild.Id, wars))
                    return;
            }


            var cw = await CreateWar(enemyClan, size, channel.Guild.Id, imsg.Channel.Id);
            //cw.Start();

            wars.Add(cw);
            await channel.SendMessageAsync($"❗🔰**CREATED CLAN WAR AGAINST {cw.ShortPrint()}**").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task StartWar(IMessage imsg, [Remainder] string number = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            int num = 0;
            int.TryParse(number, out num);

            var warsInfo = GetWarInfo(imsg, num);
            if (warsInfo == null)
            {
                await channel.SendMessageAsync("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            try
            {
                war.Start();
                await channel.SendMessageAsync($"🔰**STARTED WAR AGAINST {war.ShortPrint()}**").ConfigureAwait(false);
            }
            catch
            {
                await channel.SendMessageAsync($"🔰**WAR AGAINST {war.ShortPrint()} HAS ALREADY STARTED**").ConfigureAwait(false);
            }
            SaveWar(war);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ListWar(IMessage imsg, [Remainder] string number = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            // if number is null, print all wars in a short way
            if (string.IsNullOrWhiteSpace(number))
            {
                //check if there are any wars
                List<ClashWar> wars = null;
                ClashWars.TryGetValue(channel.Guild.Id, out wars);
                if (wars == null || wars.Count == 0)
                {
                    await channel.SendMessageAsync("🔰 **No active wars.**").ConfigureAwait(false);
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
                await channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                return;

            }
            var num = 0;
            int.TryParse(number, out num);
            //if number is not null, print the war needed
            var warsInfo = GetWarInfo(imsg, num);
            if (warsInfo == null)
            {
                await channel.SendMessageAsync("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            await channel.SendMessageAsync(warsInfo.Item1[warsInfo.Item2].ToPrettyString()).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Claim(IMessage imsg, int number, int baseNumber, [Remainder] string other_name = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            var warsInfo = GetWarInfo(imsg, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await channel.SendMessageAsync("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(other_name) ?
                imsg.Author.Username :
                other_name;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                war.Call(usr, baseNumber - 1);
                SaveWar(war);
                await channel.SendMessageAsync($"🔰**{usr}** claimed a base #{baseNumber} for a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync($"💢🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish1(IMessage imsg, int number, int baseNumber, [Remainder] string other_name = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            await FinishClaim(imsg, number, baseNumber, other_name, 1);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish2(IMessage imsg, int number, int baseNumber, [Remainder] string other_name = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            await FinishClaim(imsg, number, baseNumber, other_name, 2);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimFinish(IMessage imsg, int number, int baseNumber, [Remainder] string other_name = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            await FinishClaim(imsg, number, baseNumber, other_name);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task EndWar(IMessage imsg, int number)
        {
            var channel = (ITextChannel)imsg.Channel;

            var warsInfo = GetWarInfo(imsg,number);
            if (warsInfo == null)
            {
                await channel.SendMessageAsync("💢🔰 That war does not exist.").ConfigureAwait(false);
                return;
            }
            var war = warsInfo.Item1[warsInfo.Item2];
            war.End();
            SaveWar(war);
            await channel.SendMessageAsync($"❗🔰**War against {warsInfo.Item1[warsInfo.Item2].ShortPrint()} ended.**").ConfigureAwait(false);

            var size = warsInfo.Item1[warsInfo.Item2].Size;
            warsInfo.Item1.RemoveAt(warsInfo.Item2);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Unclaim(IMessage imsg, int number, [Remainder] string otherName = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            var warsInfo = GetWarInfo(imsg, number);
            if (warsInfo == null || warsInfo.Item1.Count == 0)
            {
                await channel.SendMessageAsync("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(otherName) ?
                imsg.Author.Username :
                otherName;
            try
            {
                var war = warsInfo.Item1[warsInfo.Item2];
                var baseNumber = war.Uncall(usr);
                SaveWar(war);
                await channel.SendMessageAsync($"🔰 @{usr} has **UNCLAIMED** a base #{baseNumber + 1} from a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync($"💢🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        private async Task FinishClaim(IMessage imsg, int number, int baseNumber, [Remainder] string other_name, int stars = 3)
        {
            var channel = (ITextChannel)imsg.Channel;
            var warInfo = GetWarInfo(imsg, number);
            if (warInfo == null || warInfo.Item1.Count == 0)
            {
                await channel.SendMessageAsync("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(other_name) ?
                imsg.Author.Username :
                other_name;

            var war = warInfo.Item1[warInfo.Item2];
            try
            {
                var baseNum = war.FinishClaim(usr, stars);
                SaveWar(war);
                await channel.SendMessageAsync($"❗🔰{imsg.Author.Mention} **DESTROYED** a base #{baseNum + 1} in a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync($"💢🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        private static Tuple<List<ClashWar>, int> GetWarInfo(IMessage imsg, int num)
        {
            var channel = (ITextChannel)imsg.Channel;
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
                    Channel = NadekoBot.Client.GetGuilds()
                                    .FirstOrDefault(s => s.Id == serverId)?
                                    .GetChannels()
                                    .FirstOrDefault(c => c.Id == channelId)
                                        as ITextChannel
                };
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
