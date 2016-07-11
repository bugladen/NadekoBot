using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes.ClashOfClans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.ClashOfClans
{
    internal class ClashOfClansModule : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.ClashOfClans;

        public static ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; } = new ConcurrentDictionary<ulong, List<ClashWar>>();

        private readonly object writeLock = new object();

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.CreateCommand(Prefix + "createwar")
                    .Alias(Prefix + "cw")
                    .Description(
                        $"Creates a new war by specifying a size (>10 and multiple of 5) and enemy clan name. |{Prefix}cw 15 The Enemy Clan")
                    .Parameter("size")
                    .Parameter("enemy_clan", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageChannels)
                            return;
                        List<ClashWar> wars;
                        if (!ClashWars.TryGetValue(e.Server.Id, out wars))
                        {
                            wars = new List<ClashWar>();
                            if (!ClashWars.TryAdd(e.Server.Id, wars))
                                return;
                        }
                        var enemyClan = e.GetArg("enemy_clan");
                        if (string.IsNullOrWhiteSpace(enemyClan))
                        {
                            return;
                        }
                        int size;
                        if (!int.TryParse(e.GetArg("size"), out size) || size < 10 || size > 50 || size % 5 != 0)
                        {
                            await e.Channel.SendMessage("💢🔰 Not a Valid war size").ConfigureAwait(false);
                            return;
                        }
                        var cw = new ClashWar(enemyClan, size, e);
                        //cw.Start();
                        wars.Add(cw);
                        cw.OnUserTimeExpired += async (u) =>
                        {
                            try
                            {
                                await
                                    e.Channel.SendMessage(
                                        $"❗🔰**Claim from @{u} for a war against {cw.ShortPrint()} has expired.**")
                                        .ConfigureAwait(false);
                            }
                            catch { }
                        };
                        cw.OnWarEnded += async () =>
                        {
                            try
                            {
                                await e.Channel.SendMessage($"❗🔰**War against {cw.ShortPrint()} ended.**").ConfigureAwait(false);
                            }
                            catch { }
                        };
                        await e.Channel.SendMessage($"❗🔰**CREATED CLAN WAR AGAINST {cw.ShortPrint()}**").ConfigureAwait(false);
                        //war with the index X started.
                    });

                cgb.CreateCommand(Prefix + "startwar")
                    .Alias(Prefix + "sw")
                    .Description("Starts a war with a given number.")
                    .Parameter("number", ParameterType.Required)
                    .Do(async e =>
                    {
                        var warsInfo = GetInfo(e);
                        if (warsInfo == null)
                        {
                            await e.Channel.SendMessage("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                            return;
                        }
                        var war = warsInfo.Item1[warsInfo.Item2];
                        try
                        {
                            var startTask = war.Start();
                            await e.Channel.SendMessage($"🔰**STARTED WAR AGAINST {war.ShortPrint()}**").ConfigureAwait(false);
                            await startTask.ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage($"🔰**WAR AGAINST {war.ShortPrint()} IS ALREADY STARTED**").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "listwar")
                    .Alias(Prefix + "lw")
                    .Description($"Shows the active war claims by a number. Shows all wars in a short way if no number is specified. | {Prefix}lw [war_number] or {Prefix}lw")
                    .Parameter("number", ParameterType.Optional)
                    .Do(async e =>
                    {
                        // if number is null, print all wars in a short way
                        if (string.IsNullOrWhiteSpace(e.GetArg("number")))
                        {
                            //check if there are any wars
                            List<ClashWar> wars = null;
                            ClashWars.TryGetValue(e.Server.Id, out wars);
                            if (wars == null || wars.Count == 0)
                            {
                                await e.Channel.SendMessage("🔰 **No active wars.**").ConfigureAwait(false);
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
                            await e.Channel.SendMessage(sb.ToString()).ConfigureAwait(false);
                            return;
                        }
                        //if number is not null, print the war needed
                        var warsInfo = GetInfo(e);
                        if (warsInfo == null)
                        {
                            await e.Channel.SendMessage("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(warsInfo.Item1[warsInfo.Item2].ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "claim")
                    .Alias(Prefix + "call")
                    .Alias(Prefix + "c")
                    .Description($"Claims a certain base from a certain war. You can supply a name in the third optional argument to claim in someone else's place.  | {Prefix}call [war_number] [base_number] [optional_other_name]")
                    .Parameter("number")
                    .Parameter("baseNumber")
                    .Parameter("other_name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var warsInfo = GetInfo(e);
                        if (warsInfo == null || warsInfo.Item1.Count == 0)
                        {
                            await e.Channel.SendMessage("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                            return;
                        }
                        int baseNum;
                        if (!int.TryParse(e.GetArg("baseNumber"), out baseNum))
                        {
                            await e.Channel.SendMessage("💢🔰 **Invalid base number.**").ConfigureAwait(false);
                            return;
                        }
                        var usr =
                            string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                            e.User.Name :
                            e.GetArg("other_name");
                        try
                        {
                            var war = warsInfo.Item1[warsInfo.Item2];
                            war.Call(usr, baseNum - 1);
                            await e.Channel.SendMessage($"🔰**{usr}** claimed a base #{baseNum} for a war against {war.ShortPrint()}").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢🔰 {ex.Message}").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "claimfinish")
                    .Alias(Prefix + "cf")
                    .Alias(Prefix + "cf3")
                    .Alias(Prefix + "claimfinish3")
                    .Description($"Finish your claim with 3 stars if you destroyed a base. Optional second argument finishes for someone else. | {Prefix}cf [war_number] [optional_other_name]")
                    .Parameter("number", ParameterType.Required)
                    .Parameter("other_name", ParameterType.Unparsed)
                    .Do(e => FinishClaim(e));

                cgb.CreateCommand(Prefix + "claimfinish2")
                    .Alias(Prefix + "cf2")
                    .Description($"Finish your claim with 2 stars if you destroyed a base. Optional second argument finishes for someone else. | {Prefix}cf [war_number] [optional_other_name]")
                    .Parameter("number", ParameterType.Required)
                    .Parameter("other_name", ParameterType.Unparsed)
                    .Do(e => FinishClaim(e, 2));

                cgb.CreateCommand(Prefix + "claimfinish1")
                    .Alias(Prefix + "cf1")
                    .Description($"Finish your claim with 1 stars if you destroyed a base. Optional second argument finishes for someone else. | {Prefix}cf [war_number] [optional_other_name]")
                    .Parameter("number", ParameterType.Required)
                    .Parameter("other_name", ParameterType.Unparsed)
                    .Do(e => FinishClaim(e, 1));

                cgb.CreateCommand(Prefix + "unclaim")
                    .Alias(Prefix + "uncall")
                    .Alias(Prefix + "uc")
                    .Description($"Removes your claim from a certain war. Optional second argument denotes a person in whos place to unclaim | {Prefix}uc [war_number] [optional_other_name]")
                    .Parameter("number", ParameterType.Required)
                    .Parameter("other_name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var warsInfo = GetInfo(e);
                        if (warsInfo == null || warsInfo.Item1.Count == 0)
                        {
                            await e.Channel.SendMessage("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                            return;
                        }
                        var usr =
                            string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                            e.User.Name :
                            e.GetArg("other_name");
                        try
                        {
                            var war = warsInfo.Item1[warsInfo.Item2];
                            var baseNumber = war.Uncall(usr);
                            await e.Channel.SendMessage($"🔰 @{usr} has **UNCLAIMED** a base #{baseNumber + 1} from a war against {war.ShortPrint()}").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢🔰 {ex.Message}").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "endwar")
                    .Alias(Prefix + "ew")
                    .Description($"Ends the war with a given index. |{Prefix}ew [war_number]")
                    .Parameter("number")
                    .Do(async e =>
                    {
                        var warsInfo = GetInfo(e);
                        if (warsInfo == null)
                        {
                            await e.Channel.SendMessage("💢🔰 That war does not exist.").ConfigureAwait(false);
                            return;
                        }
                        warsInfo.Item1[warsInfo.Item2].End();

                        var size = warsInfo.Item1[warsInfo.Item2].Size;
                        warsInfo.Item1.RemoveAt(warsInfo.Item2);
                    });
            });
        }

        private async Task FinishClaim(CommandEventArgs e, int stars = 3)
        {
            var warInfo = GetInfo(e);
            if (warInfo == null || warInfo.Item1.Count == 0)
            {
                await e.Channel.SendMessage("💢🔰 **That war does not exist.**").ConfigureAwait(false);
                return;
            }
            var usr =
                string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                e.User.Name :
                e.GetArg("other_name");

            var war = warInfo.Item1[warInfo.Item2];
            try
            {
                var baseNum = war.FinishClaim(usr, stars);
                await e.Channel.SendMessage($"❗🔰{e.User.Mention} **DESTROYED** a base #{baseNum + 1} in a war against {war.ShortPrint()}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await e.Channel.SendMessage($"💢🔰 {ex.Message}").ConfigureAwait(false);
            }
        }

        private static Tuple<List<ClashWar>, int> GetInfo(CommandEventArgs e)
        {
            //check if there are any wars
            List<ClashWar> wars = null;
            ClashWars.TryGetValue(e.Server.Id, out wars);
            if (wars == null || wars.Count == 0)
            {
                return null;
            }
            // get the number of the war
            int num;
            if (string.IsNullOrWhiteSpace(e.GetArg("number")))
                num = 0;
            else if (!int.TryParse(e.GetArg("number"), out num) || num > wars.Count)
            {
                return null;
            }
            num -= 1;
            //get the actual war
            return new Tuple<List<ClashWar>, int>(wars, num);
        }
    }
}
