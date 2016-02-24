using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Concurrent;
using Discord;
using System.Threading;

namespace NadekoBot.Commands {
    class ClashOfClans : DiscordCommand {

        private static string prefix = ",";

        public static ConcurrentDictionary<ulong, List<ClashWar>> ClashWars { get; } = new ConcurrentDictionary<ulong, List<ClashWar>>();

        private object writeLock { get; } = new object();

        public ClashOfClans() : base() {

        }

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            if (!e.User.ServerPermissions.ManageChannels)
                return;
            List<ClashWar> wars;
            if (!ClashWars.TryGetValue(e.Server.Id, out wars)) {
                wars = new List<ClashWar>();
                if (!ClashWars.TryAdd(e.Server.Id, wars))
                    return;
            }
            string enemyClan = e.GetArg("enemy_clan");
            if (string.IsNullOrWhiteSpace(enemyClan)) {
                return;
            }
            int size;
            if (!int.TryParse(e.GetArg("size"), out size) || size < 10 || size > 50 || size % 5 != 0) {
                await e.Channel.SendMessage("💢🔰 Not a Valid war size");
                return;
            }
            var cw = new ClashWar(enemyClan, size, e);
            //cw.Start();
            wars.Add(cw);
            cw.OnUserTimeExpired += async (u) => {
                await e.Channel.SendMessage($"❗🔰**Claim from @{u} for a war against {cw.ShortPrint()} has expired.**");
            };
            cw.OnWarEnded += async () => {
                await e.Channel.SendMessage($"❗🔰**War against {cw.ShortPrint()} ended.**");
            };
            await e.Channel.SendMessage($"❗🔰**CREATED CLAN WAR AGAINST {cw.ShortPrint()}**");
            //war with the index X started.
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand(prefix + "createwar")
                .Alias(prefix + "cw")
                .Description($"Creates a new war by specifying a size (>10 and multiple of 5) and enemy clan name.\n**Usage**:{prefix}cw 15 The Enemy Clan")
                .Parameter("size")
                .Parameter("enemy_clan", ParameterType.Unparsed)
                .Do(DoFunc());

            cgb.CreateCommand(prefix + "sw")
                .Alias(prefix + "startwar")
                .Description("Starts a war with a given number.")
                .Parameter("number", ParameterType.Required)
                .Do(async e => {
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    var war = warsInfo.Item1[warsInfo.Item2];
                    try {
                        war.Start();
                        await e.Channel.SendMessage($"🔰**STARTED WAR AGAINST {war.ShortPrint()}**");
                    }
                    catch {
                        await e.Channel.SendMessage($"🔰**WAR AGAINST {war.ShortPrint()} IS ALREADY STARTED**");
                    }
                });

            cgb.CreateCommand(prefix + "listwar")
                .Alias(prefix + "lw")
                .Description($"Shows the active war claims by a number. Shows all wars in a short way if no number is specified.\n**Usage**: {prefix}lw [war_number] or {prefix}lw")
                .Parameter("number", ParameterType.Optional)
                .Do(async e => {
                    // if number is null, print all wars in a short way
                    if (string.IsNullOrWhiteSpace(e.GetArg("number"))) {
                        //check if there are any wars
                        List<ClashWar> wars = null;
                        ClashWars.TryGetValue(e.Server.Id, out wars);
                        if (wars == null || wars.Count == 0) {
                            await e.Channel.SendMessage("🔰 **No active wars.**");
                            return;
                        }

                        var sb = new StringBuilder();
                        sb.AppendLine("🔰 **LIST OF ACTIVE WARS**");
                        sb.AppendLine("**-------------------------**");
                        for (int i = 0; i < wars.Count; i++) {
                            sb.AppendLine($"**#{i + 1}.**  `Enemy:` **{wars[i].EnemyClan}**");
                            sb.AppendLine($"\t\t`Size:` **{wars[i].Size} v {wars[i].Size}**");
                            sb.AppendLine("**-------------------------**");
                        }
                        await e.Channel.SendMessage(sb.ToString());
                        return;
                    }
                    //if number is not null, print the war needed
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    await e.Channel.SendMessage(warsInfo.Item1[warsInfo.Item2].ToString());
                });

            cgb.CreateCommand(prefix + "claim")
                .Alias(prefix + "call")
                .Alias(prefix + "c")
                .Description($"Claims a certain base from a certain war. You can supply a name in the third optional argument to claim in someone else's place. \n**Usage**: {prefix}call [war_number] [base_number] (optional_otheruser)")
                .Parameter("number")
                .Parameter("baseNumber")
                .Parameter("other_name", ParameterType.Unparsed)
                .Do(async e => {
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null || warsInfo.Item1.Count == 0) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    int baseNum;
                    if (!int.TryParse(e.GetArg("baseNumber"), out baseNum)) {
                        await e.Channel.SendMessage("💢🔰 **Invalid base number.**");
                        return;
                    }
                    string usr =
                        string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                        e.User.Name :
                        e.GetArg("other_name");
                    try {
                        var war = warsInfo.Item1[warsInfo.Item2];
                        await war.Call(usr, baseNum - 1);
                        await e.Channel.SendMessage($"🔰**{usr}** claimed a base #{baseNum} for a war against {war.ShortPrint()}");
                    }
                    catch (Exception ex) {
                        await e.Channel.SendMessage($"💢🔰 {ex.Message}");
                    }
                });

            cgb.CreateCommand(prefix + "cf")
                .Alias(prefix + "claimfinish")
                .Description($"Finish your claim if you destroyed a base. Optional second argument finishes for someone else.\n**Usage**: {prefix}cf [war_number] (optional_other_name)")
                .Parameter("number", ParameterType.Required)
                .Parameter("other_name", ParameterType.Unparsed)
                .Do(async e => {
                    var warInfo = GetInfo(e);
                    if (warInfo == null || warInfo.Item1.Count == 0) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    string usr =
                        string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                        e.User.Name :
                        e.GetArg("other_name");

                    var war = warInfo.Item1[warInfo.Item2];
                    try {
                        var baseNum = war.FinishClaim(usr);
                        await e.Channel.SendMessage($"❗🔰{e.User.Mention} **DESTROYED** a base #{baseNum + 1} in a war against {war.ShortPrint()}");
                    }
                    catch (Exception ex) {
                        await e.Channel.SendMessage($"💢🔰 {ex.Message}");
                    }
                });

            cgb.CreateCommand(prefix + "unclaim")
                .Alias(prefix + "uncall")
                .Alias(prefix + "uc")
                .Description($"Removes your claim from a certain war. Optional second argument denotes a person in whos place to unclaim\n**Usage**: {prefix}uc [war_number] (optional_other_name)")
                .Parameter("number", ParameterType.Required)
                .Parameter("other_name", ParameterType.Unparsed)
                .Do(async e => {
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null || warsInfo.Item1.Count == 0) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    string usr =
                        string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                        e.User.Name :
                        e.GetArg("other_name");
                    try {
                        var war = warsInfo.Item1[warsInfo.Item2];
                        int baseNumber = war.Uncall(usr);
                        await e.Channel.SendMessage($"🔰 @{usr} has **UNCLAIMED** a base #{baseNumber + 1} from a war against {war.ShortPrint()}");
                    }
                    catch (Exception ex) {
                        await e.Channel.SendMessage($"💢🔰 {ex.Message}");
                    }
                });
            /*
            cgb.CreateCommand(prefix + "forceunclaim")
                .Alias(prefix + "forceuncall")
                .Alias(prefix + "fuc")
                .Description($"Force removes a base claim from a certain war from a certain base. \n**Usage**: {prefix}fuc [war_number] [base_number]")
                .Parameter("number", ParameterType.Required)
                .Parameter("other_name", ParameterType.Unparsed)
                .Do(async e => {
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null || warsInfo.Item1.Count == 0) {
                        await e.Channel.SendMessage("💢🔰 **That war does not exist.**");
                        return;
                    }
                    string usr =
                        string.IsNullOrWhiteSpace(e.GetArg("other_name")) ?
                        e.User.Name :
                        e.GetArg("other_name").Trim();
                    try {
                        var war = warsInfo.Item1[warsInfo.Item2];
                        int baseNumber = war.Uncall(usr);
                        await e.Channel.SendMessage($"🔰 @{usr} has **UNCLAIMED** a base #{baseNumber + 1} from a war against {war.ShortPrint()}");
                    }
                    catch (Exception ex) {
                        await e.Channel.SendMessage($"💢🔰 {ex.Message}");
                    }
                });
            */

            cgb.CreateCommand(prefix + "endwar")
                .Alias(prefix + "ew")
                .Description($"Ends the war with a given index.\n**Usage**:{prefix}ew [war_number]")
                .Parameter("number")
                .Do(async e => {
                    var warsInfo = GetInfo(e);
                    if (warsInfo == null) {
                        await e.Channel.SendMessage("💢🔰 That war does not exist.");
                        return;
                    }
                    warsInfo.Item1[warsInfo.Item2].End();

                    int size = warsInfo.Item1[warsInfo.Item2].Size;
                    warsInfo.Item1.RemoveAt(warsInfo.Item2);
                });
        }

        private Tuple<List<ClashWar>, int> GetInfo(CommandEventArgs e) {
            //check if there are any wars
            List<ClashWar> wars = null;
            ClashWars.TryGetValue(e.Server.Id, out wars);
            if (wars == null || wars.Count == 0) {
                return null;
            }
            // get the number of the war
            int num;
            if (string.IsNullOrWhiteSpace(e.GetArg("number")))
                num = 0;
            else if (!int.TryParse(e.GetArg("number"), out num) || num > wars.Count) {
                return null;
            }
            num -= 1;
            //get the actual war
            return new Tuple<List<ClashWar>, int>(wars, num);
        }
    }

    internal class Caller {
        private string _user;

        public string CallUser
        {
            get { return _user; }
            set { _user = value; }
        }

        private DateTime timeAdded;

        public DateTime TimeAdded
        {
            get { return timeAdded; }
            set { timeAdded = value; }
        }

        public bool BaseDestroyed { get; internal set; }
    }

    internal class ClashWar {

        public static TimeSpan callExpire => new TimeSpan(2, 0, 0);

        private CommandEventArgs e;
        private string enemyClan;
        public string EnemyClan => enemyClan;
        private int size;
        public int Size => size;
        private Caller[] bases;
        private CancellationTokenSource[] baseCancelTokens;
        private CancellationTokenSource endTokenSource = new CancellationTokenSource();
        public Action<string> OnUserTimeExpired { get; set; } = null;
        public Action OnWarEnded { get; set; } = null;
        public bool Started { get; set; } = false;

        public ClashWar(string enemyClan, int size, CommandEventArgs e) {
            this.enemyClan = enemyClan;
            this.size = size;
            this.bases = new Caller[size];
            this.baseCancelTokens = new CancellationTokenSource[size];
        }

        internal void End() {
            if (!endTokenSource.Token.IsCancellationRequested) {
                endTokenSource.Cancel();
                if (OnWarEnded != null)
                    OnWarEnded();
            }
        }

        internal async Task Call(string u, int baseNumber) {
            if (baseNumber < 0 || baseNumber >= bases.Length)
                throw new ArgumentException("Invalid base number");
            if (bases[baseNumber] != null)
                throw new ArgumentException("That base is already claimed.");
            for (int i = 0; i < bases.Length; i++) {
                if (bases[i]?.BaseDestroyed == false && bases[i]?.CallUser == u)
                    throw new ArgumentException($"@{u} You already claimed a base #{i + 1}. You can't claim a new one.");
            }

            bases[baseNumber] = new Caller { CallUser = u.Trim(), TimeAdded = DateTime.Now, BaseDestroyed = false };
        }

        internal async void Start() {
            if (Started)
                throw new InvalidOperationException();
            try {
                Started = true;
                for (int i = 0; i < bases.Length; i++) {
                    if (bases[i] != null)
                        bases[i].TimeAdded = DateTime.Now;
                }
                Task.Run(async () => await ClearArray());
                await Task.Delay(new TimeSpan(24, 0, 0), endTokenSource.Token);
            }
            catch (Exception) { }
            finally {
                End();
            }
        }
        internal int Uncall(string user) {
            user = user.Trim();
            for (int i = 0; i < bases.Length; i++) {
                if (bases[i]?.CallUser == user) {
                    bases[i] = null;
                    return i;
                }
            }
            throw new InvalidOperationException("You are not participating in that war.");
        }

        private async Task ClearArray() {
            while (!endTokenSource.IsCancellationRequested) {
                await Task.Delay(5000);
                for (int i = 0; i < bases.Length; i++) {
                    if (bases[i] == null) continue;
                    if (!bases[i].BaseDestroyed && DateTime.Now - bases[i].TimeAdded >= callExpire) {
                        Console.WriteLine($"Removing user {bases[i].CallUser}");
                        if (OnUserTimeExpired != null)
                            OnUserTimeExpired(bases[i].CallUser);
                        bases[i] = null;
                    }
                }
            }
            Console.WriteLine("Out of clear array");
        }

        public string ShortPrint() =>
            $"`{enemyClan}` ({size} v {size})";

        public override string ToString() {
            var sb = new StringBuilder();
            
            sb.AppendLine($"🔰**WAR AGAINST `{enemyClan}` ({size} v {size}) INFO:**");
            if (!Started)
                sb.AppendLine("`not started`");
            for (int i = 0; i < bases.Length; i++) {
                if (bases[i] == null) {
                    sb.AppendLine($"`{i + 1}.` ❌*unclaimed*");
                }
                else {
                    if (bases[i].BaseDestroyed) {
                        sb.AppendLine($"`{i + 1}.` ✅ `{bases[i].CallUser}` ⭐ ⭐ ⭐");
                    }
                    else {
                        var left = Started ? callExpire - (DateTime.Now - bases[i].TimeAdded) : callExpire;
                        sb.AppendLine($"`{i + 1}.` ✅ `{bases[i].CallUser}` {left.Hours}h {left.Minutes}m {left.Seconds}s left");
                    }
                }

            }
            return sb.ToString();
        }

        internal int FinishClaim(string user) {
            user = user.Trim();
            for (int i = 0; i < bases.Length; i++) {
                if (bases[i]?.BaseDestroyed == false && bases[i]?.CallUser == user) {
                    bases[i].BaseDestroyed = true;
                    return i;
                }
            }
            throw new InvalidOperationException($"@{user} You are either not participating in that war, or you already destroyed a base.");
        }
    }
}
