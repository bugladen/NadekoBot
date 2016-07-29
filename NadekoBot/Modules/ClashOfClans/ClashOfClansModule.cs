using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes.ClashOfClans;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.ClashOfClans
{
    internal class ClashOfClansModule : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.ClashOfClans;

        public static ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; set; } = new ConcurrentDictionary<ulong, List<ClashWar>>();

        private readonly object writeLock = new object();

        public ClashOfClansModule()
        {
            NadekoBot.OnReady += () => Task.Run(async () =>
            {
                if (File.Exists("data/clashofclans/wars.json"))
                {
                    try
                    {
                        var content = File.ReadAllText("data/clashofclans/wars.json");

                        var dict = JsonConvert.DeserializeObject<Dictionary<ulong, List<ClashWar>>>(content);

                        foreach (var cw in dict)
                        {
                            cw.Value.ForEach(war =>
                            {
                                war.Channel = NadekoBot.Client.GetServer(war.ServerId)?.GetChannel(war.ChannelId);
                                if (war.Channel == null)
                                {
                                    cw.Value.Remove(war);
                                }
                            }
                            );
                        }
                        //urgh
                        ClashWars = new ConcurrentDictionary<ulong, List<ClashWar>>(dict);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Could not load coc wars: " + e.Message);
                    }


                }
                //Can't this be disabled if the modules is disabled too :)
                var callExpire = new TimeSpan(2, 0, 0);
                var warExpire = new TimeSpan(23, 0, 0);
                while (true)
                {
                    try
                    {
                        var hash = ClashWars.GetHashCode();
                        foreach (var cw in ClashWars)
                        {
                            foreach (var war in cw.Value)
                            {
                                await CheckWar(callExpire, war);
                            }
                            List<ClashWar> newVal = new List<ClashWar>();
                            foreach (var w in cw.Value)
                            {
                                //We add when A: the war is not ended
                                if (w.WarState != WarState.Ended)
                                {
                                    //and B: the war has not expired
                                    if ((w.WarState == WarState.Started && DateTime.UtcNow - w.StartedAt <= warExpire) || w.WarState == WarState.Created)
                                    {
                                        newVal.Add(w);
                                    }
                                }
                            }
                            //var newVal = cw.Value.Where(w => !(w.Ended || DateTime.UtcNow - w.StartedAt >= warExpire)).ToList();
                            foreach (var exWar in cw.Value.Except(newVal))
                            {
                                await exWar.Channel.SendMessage($"War against {exWar.EnemyClan} ({exWar.Size}v{exWar.Size}) has ended");
                            }

                            if (newVal.Count == 0)
                            {
                                List<ClashWar> obj;
                                ClashWars.TryRemove(cw.Key, out obj);
                            }
                            else
                            {
                                ClashWars.AddOrUpdate(cw.Key, newVal, (x, s) => newVal);
                            }
                        }
                        if (hash != ClashWars.GetHashCode()) //something changed 
                        {
                            Save();
                        }


                    }
                    catch { }
                    await Task.Delay(5000);
                }
            });
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory("data/clashofclans");
                File.WriteAllText("data/clashofclans/wars.json", JsonConvert.SerializeObject(ClashWars, Formatting.Indented));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task CheckWar(TimeSpan callExpire, ClashWar war)
        {
            var Bases = war.Bases;
            for (var i = 0; i < Bases.Length; i++)
            {
                if (Bases[i] == null) continue;
                if (!Bases[i].BaseDestroyed && DateTime.UtcNow - Bases[i].TimeAdded >= callExpire)
                {
                    await war.Channel.SendMessage($"❗🔰**Claim from @{Bases[i].CallUser} for a war against {war.ShortPrint()} has expired.**").ConfigureAwait(false);
                    Bases[i] = null;
                }
            }
        }

        #region commands
        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                cgb.CreateCommand(Prefix + "createwar")
                      .Alias(Prefix + "cw")
                      .Description($"Creates a new war by specifying a size (>10 and multiple of 5) and enemy clan name. |{Prefix}cw 15 The Enemy Clan")
                      .Parameter("size")
                      .Parameter("enemy_clan", ParameterType.Unparsed)
                      .Do(async e =>
                      {
                          if (!e.User.ServerPermissions.ManageChannels)
                              return;
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
                          List<ClashWar> wars;
                          if (!ClashWars.TryGetValue(e.Server.Id, out wars))
                          {
                              wars = new List<ClashWar>();
                              if (!ClashWars.TryAdd(e.Server.Id, wars))
                                  return;
                          }


                          var cw = new ClashWar(enemyClan, size, e.Server.Id, e.Channel.Id);
                          //cw.Start();

                          wars.Add(cw);
                          await e.Channel.SendMessage($"❗🔰**CREATED CLAN WAR AGAINST {cw.ShortPrint()}**").ConfigureAwait(false);
                          Save();
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
                            war.Start();
                            await e.Channel.SendMessage($"🔰**STARTED WAR AGAINST {war.ShortPrint()}**").ConfigureAwait(false);
                        }
                        catch
                        {
                            await e.Channel.SendMessage($"🔰**WAR AGAINST {war.ShortPrint()} HAS ALREADY STARTED**").ConfigureAwait(false);
                        }
                        Save();
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
                            Save();
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
                    .Description($"Removes your claim from a certain war. Optional second argument denotes a person in whose place to unclaim | {Prefix}uc [war_number] [optional_other_name]")
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
                            Save();
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
                        await e.Channel.SendMessage($"❗🔰**War against {warsInfo.Item1[warsInfo.Item2].ShortPrint()} ended.**").ConfigureAwait(false);

                        var size = warsInfo.Item1[warsInfo.Item2].Size;
                        warsInfo.Item1.RemoveAt(warsInfo.Item2);
                        Save();
                    });
            });

        }
        #endregion


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
                Save();
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
