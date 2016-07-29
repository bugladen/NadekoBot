using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Games.Commands
{
    /// <summary>
    /// Flower picking/planting idea is given to me by its
    /// inceptor Violent Crumble from Game Developers League discord server
    /// (he has !cookie and !nom) Thanks a lot Violent!
    /// Check out GDL (its a growing gamedev community):
    /// https://discord.gg/0TYNJfCU4De7YIk8
    /// </summary>
    class PlantPick : DiscordCommand
    {

        private Random rng;
        public PlantPick(DiscordModule module) : base(module)
        {
            NadekoBot.Client.MessageReceived += PotentialFlowerGeneration;
            rng = new Random();
        }

        private static readonly ConcurrentDictionary<ulong, DateTime> plantpickCooldowns = new ConcurrentDictionary<ulong, DateTime>();

        private async void PotentialFlowerGeneration(object sender, Discord.MessageEventArgs e)
        {
            try
            {
                if (e.Server == null || e.Channel.IsPrivate || e.Message.IsAuthor)
                    return;
                var config = Classes.SpecificConfigurations.Default.Of(e.Server.Id);
                var now = DateTime.Now;
                int cd;
                DateTime lastSpawned;
                if (config.GenerateCurrencyChannels.TryGetValue(e.Channel.Id, out cd))
                    if (!plantpickCooldowns.TryGetValue(e.Channel.Id, out lastSpawned) || (lastSpawned + new TimeSpan(0, cd, 0)) < now)
                    {
                        var rnd = Math.Abs(rng.Next(0,101));
                        if (rnd == 0)
                        {
                            var msgs = new[] { await e.Channel.SendFile(GetRandomCurrencyImagePath()), await e.Channel.SendMessage($"❗ A random {NadekoBot.Config.CurrencyName} appeared! Pick it up by typing `>pick`") };
                            plantedFlowerChannels.AddOrUpdate(e.Channel.Id, msgs, (u, m) => { m.ForEach(async msgToDelete => { try { await msgToDelete.Delete(); } catch { } }); return msgs; });
                            plantpickCooldowns.AddOrUpdate(e.Channel.Id, now, (i, d) => now);
                        }
                    }
            }
            catch { }
        }
        //channelid/messageid pair
        ConcurrentDictionary<ulong, IEnumerable<Message>> plantedFlowerChannels = new ConcurrentDictionary<ulong, IEnumerable<Message>>();

        private SemaphoreSlim locker = new SemaphoreSlim(1,1);

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "pick")
                .Description($"Picks a flower planted in this channel. | `{Prefix}pick`")
                .Do(async e =>
                {
                    IEnumerable<Message> msgs;

                    await e.Message.Delete().ConfigureAwait(false);
                    if (!plantedFlowerChannels.TryRemove(e.Channel.Id, out msgs))
                        return;

                    foreach(var msgToDelete in msgs)
                        await msgToDelete.Delete().ConfigureAwait(false);

                    await FlowersHandler.AddFlowersAsync(e.User, "Picked a flower.", 1, true).ConfigureAwait(false);
                    var msg = await e.Channel.SendMessage($"**{e.User.Name}** picked a {NadekoBot.Config.CurrencyName}!").ConfigureAwait(false);
                    ThreadPool.QueueUserWorkItem(async (state) =>
                    {
                        try
                        {
                            await Task.Delay(10000).ConfigureAwait(false);
                            await msg.Delete().ConfigureAwait(false);
                        }
                        catch { }
                    });
                });

            cgb.CreateCommand(Module.Prefix + "plant")
                .Description($"Spend a flower to plant it in this channel. (If bot is restarted or crashes, flower will be lost) | `{Prefix}plant`")
                .Do(async e =>
                {
                    await locker.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (plantedFlowerChannels.ContainsKey(e.Channel.Id))
                        {
                            await e.Channel.SendMessage($"There is already a {NadekoBot.Config.CurrencyName} in this channel.").ConfigureAwait(false);
                            return;
                        }
                        var removed = await FlowersHandler.RemoveFlowers(e.User, "Planted a flower.", 1, true).ConfigureAwait(false);
                        if (!removed)
                        {
                            await e.Channel.SendMessage($"You don't have any {NadekoBot.Config.CurrencyName}s.").ConfigureAwait(false);
                            return;
                        }

                        var file = GetRandomCurrencyImagePath();
                        Message msg;
                        if (file == null)
                            msg = await e.Channel.SendMessage(NadekoBot.Config.CurrencySign).ConfigureAwait(false);
                        else
                            msg = await e.Channel.SendFile(file).ConfigureAwait(false);
                        var vowelFirst = new[] { 'a', 'e', 'i', 'o', 'u' }.Contains(NadekoBot.Config.CurrencyName[0]);
                        var msg2 = await e.Channel.SendMessage($"Oh how Nice! **{e.User.Name}** planted {(vowelFirst ? "an" : "a")} {NadekoBot.Config.CurrencyName}. Pick it using {Module.Prefix}pick").ConfigureAwait(false);
                        plantedFlowerChannels.TryAdd(e.Channel.Id, new[] { msg, msg2 });
                    }
                    finally { locker.Release();  }
                });

            cgb.CreateCommand(Prefix + "gencurrency")
                .Alias(Prefix + "gc")
                .Description($"Toggles currency generation on this channel. Every posted message will have 2% chance to spawn a {NadekoBot.Config.CurrencyName}. Optional parameter cooldown time in minutes, 5 minutes by default. Requires Manage Messages permission. | `{Prefix}gc` or `{Prefix}gc 60`")
                .AddCheck(SimpleCheckers.ManageMessages())
                .Parameter("cd", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var cdStr = e.GetArg("cd");
                    int cd = 2;
                    if (!int.TryParse(cdStr, out cd) || cd < 0)
                    {
                        cd = 2;
                    }
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    int throwaway;
                    if (config.GenerateCurrencyChannels.TryRemove(e.Channel.Id, out throwaway))
                    {
                        await e.Channel.SendMessage("`Currency generation disabled on this channel.`").ConfigureAwait(false);
                    }
                    else
                    {
                        if (config.GenerateCurrencyChannels.TryAdd(e.Channel.Id, cd))
                            await e.Channel.SendMessage($"`Currency generation enabled on this channel. Cooldown is {cd} minutes.`").ConfigureAwait(false);
                    }
                });
        }

        private string GetRandomCurrencyImagePath() =>
            Directory.GetFiles("data/currency_images").OrderBy(s => rng.Next()).FirstOrDefault();

        int GetRandomNumber()
        {
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[4];
                rg.GetBytes(rno);
                int randomvalue = BitConverter.ToInt32(rno, 0);
                return randomvalue;
            }
        }
    }
}
