using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules.Pokemon
{
    class PokemonModule : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Pokemon;

        private ConcurrentDictionary<ulong, PokeStats> Stats = new ConcurrentDictionary<ulong, PokeStats>();

        public PokemonModule()
        {

        }

        private int GetDamage(PokemonType usertype, PokemonType targetType)
        {
            var rng = new Random();
            int damage = rng.Next(40, 60);
            foreach (PokemonMultiplier Multiplier in usertype.Multipliers)
            {
                if (Multiplier.Type == targetType.Name)
                {
                    var multiplier = Multiplier.Multiplication;
                    damage = (int)(damage * multiplier);
                }
            }

            return damage;
        }

        private PokemonType GetPokeType(ulong id)
        {

            var db = DbHandler.Instance.GetAllRows<UserPokeTypes>();
            Dictionary<long, string> setTypes = db.ToDictionary(x => x.UserId, y => y.type);
            if (setTypes.ContainsKey((long)id))
            {
                return stringToPokemonType(setTypes[(long)id]);
            }
            int count = NadekoBot.Config.PokemonTypes.Count;

            int remainder = Math.Abs((int)(id % (ulong)count));

            return NadekoBot.Config.PokemonTypes[remainder];
        }



        private PokemonType stringToPokemonType(string v)
        {
            var str = v.ToUpperInvariant();
            var list = NadekoBot.Config.PokemonTypes;
            foreach (PokemonType p in list)
            {
                if (str == p.Name)
                {
                    return p;
                }
            }
            return null;
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "attack")
                    .Description($"Attacks a target with the given move. Use `{Prefix}movelist` to see a list of moves your type can use. | `{Prefix}attack \"vine whip\" @someguy`")
                    .Parameter("move", ParameterType.Required)
                    .Parameter("target", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var move = e.GetArg("move");
                        var targetStr = e.GetArg("target")?.Trim();
                        if (string.IsNullOrWhiteSpace(targetStr))
                            return;
                        var target = e.Server.FindUsers(targetStr).FirstOrDefault();
                        if (target == null)
                        {
                            await e.Channel.SendMessage("No such person.").ConfigureAwait(false);
                            return;
                        }
                        else if (target == e.User)
                        {
                            await e.Channel.SendMessage("You can't attack yourself.").ConfigureAwait(false);
                            return;
                        }
                        // Checking stats first, then move
                        //Set up the userstats
                        PokeStats userStats;
                        userStats = Stats.GetOrAdd(e.User.Id, new PokeStats());

                        //Check if able to move
                        //User not able if HP < 0, has made more than 4 attacks
                        if (userStats.Hp < 0)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} has fainted and was not able to move!").ConfigureAwait(false);
                            return;
                        }
                        if (userStats.MovesMade >= 5)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} has used too many moves in a row and was not able to move!").ConfigureAwait(false);
                            return;
                        }
                        if (userStats.LastAttacked.Contains(target.Id))
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} can't attack again without retaliation!").ConfigureAwait(false);
                            return;
                        }
                        //get target stats
                        PokeStats targetStats;
                        targetStats = Stats.GetOrAdd(target.Id, new PokeStats());

                        //If target's HP is below 0, no use attacking
                        if (targetStats.Hp <= 0)
                        {
                            await e.Channel.SendMessage($"{target.Mention} has already fainted!").ConfigureAwait(false);
                            return;
                        }

                        //Check whether move can be used
                        PokemonType userType = GetPokeType(e.User.Id);

                        var enabledMoves = userType.Moves;
                        if (!enabledMoves.Contains(move.ToLowerInvariant()))
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} was not able to use **{move}**, use `{Prefix}ml` to see moves you can use").ConfigureAwait(false);
                            return;
                        }

                        //get target type
                        PokemonType targetType = GetPokeType(target.Id);
                        //generate damage
                        int damage = GetDamage(userType, targetType);
                        //apply damage to target
                        targetStats.Hp -= damage;

                        var response = $"{e.User.Mention} used **{move}**{userType.Icon} on {target.Mention}{targetType.Icon} for **{damage}** damage";

                        //Damage type
                        if (damage < 40)
                        {
                            response += "\nIt's not effective..";
                        }
                        else if (damage > 60)
                        {
                            response += "\nIt's super effective!";
                        }
                        else
                        {
                            response += "\nIt's somewhat effective";
                        }

                        //check fainted

                        if (targetStats.Hp <= 0)
                        {
                            response += $"\n**{target.Name}** has fainted!";
                        }
                        else
                        {
                            response += $"\n**{target.Name}** has {targetStats.Hp} HP remaining";
                        }

                        //update other stats
                        userStats.LastAttacked.Add(target.Id);
                        userStats.MovesMade++;
                        targetStats.MovesMade = 0;
                        if (targetStats.LastAttacked.Contains(e.User.Id))
                        {
                            targetStats.LastAttacked.Remove(e.User.Id);
                        }

                        //update dictionary
                        //This can stay the same right?
                        Stats[e.User.Id] = userStats;
                        Stats[target.Id] = targetStats;

                        await e.Channel.SendMessage(response).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "movelist")
                    .Alias(Prefix + "ml")
                    .Description($"Lists the moves you are able to use | `{Prefix}ml`")
                    .Do(async e =>
                    {
                        var userType = GetPokeType(e.User.Id);
                        var movesList = userType.Moves;
                        var str = $"**Moves for `{userType.Name}` type.**";
                        foreach (string m in movesList)
                        {
                            str += $"\n{userType.Icon}{m}";
                        }
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "heal")
                    .Description($"Heals someone. Revives those who fainted. Costs a {NadekoBot.Config.CurrencyName} | `{Prefix}heal @someone`")
                    .Parameter("target", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var targetStr = e.GetArg("target")?.Trim();
                        if (string.IsNullOrWhiteSpace(targetStr))
                            return;
                        var usr = e.Server.FindUsers(targetStr).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("No such person.").ConfigureAwait(false);
                            return;
                        }
                        if (Stats.ContainsKey(usr.Id))
                        {

                            var targetStats = Stats[usr.Id];
                            int HP = targetStats.Hp;
                            if (targetStats.Hp == targetStats.MaxHp)
                            {
                                await e.Channel.SendMessage($"{usr.Name} already has full HP!").ConfigureAwait(false);
                                return;
                            }
                            //Payment~
                            var amount = 1;
                            var pts = Classes.DbHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                            if (pts < amount)
                            {
                                await e.Channel.SendMessage($"{e.User.Mention} you don't have enough {NadekoBot.Config.CurrencyName}s! \nYou still need {amount - pts} {NadekoBot.Config.CurrencySign} to be able to do this!").ConfigureAwait(false);
                                return;
                            }
                            var target = (usr.Id == e.User.Id) ? "yourself" : usr.Name;
                            await FlowersHandler.RemoveFlowers(e.User, $"Poke-Heal {target}", amount).ConfigureAwait(false);
                            //healing
                            targetStats.Hp = targetStats.MaxHp;
                            if (HP < 0)
                            {
                                //Could heal only for half HP?
                                Stats[usr.Id].Hp = (targetStats.MaxHp / 2);
                                await e.Channel.SendMessage($"{e.User.Name} revived {usr.Name} with one {NadekoBot.Config.CurrencySign}").ConfigureAwait(false);
                                return;
                            }
                            var vowelFirst = new[] { 'a', 'e', 'i', 'o', 'u' }.Contains(NadekoBot.Config.CurrencyName[0]);
                            await e.Channel.SendMessage($"{e.User.Name} healed {usr.Name} for {targetStats.MaxHp - HP} HP with {(vowelFirst ? "an" : "a")} {NadekoBot.Config.CurrencySign}").ConfigureAwait(false);
                            return;
                        }
                        else
                        {
                            await e.Channel.SendMessage($"{usr.Name} already has full HP!").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "type")
                    .Description($"Get the poketype of the target. | `{Prefix}type @someone`")
                    .Parameter("target", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usrStr = e.GetArg("target")?.Trim();
                        if (string.IsNullOrWhiteSpace(usrStr))
                            return;
                        var usr = e.Server.FindUsers(usrStr).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("No such person.").ConfigureAwait(false);
                            return;
                        }
                        var pType = GetPokeType(usr.Id);
                        await e.Channel.SendMessage($"Type of {usr.Name} is **{pType.Name.ToLowerInvariant()}**{pType.Icon}").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "settype")
                    .Description($"Set your poketype. Costs a {NadekoBot.Config.CurrencyName}. | `{Prefix}settype fire`")
                    .Parameter("targetType", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var targetTypeStr = e.GetArg("targetType")?.ToUpperInvariant();
                        if (string.IsNullOrWhiteSpace(targetTypeStr))
                            return;
                        var targetType = stringToPokemonType(targetTypeStr);
                        if (targetType == null)
                        {
                            await e.Channel.SendMessage("Invalid type specified. Type must be one of:\n" + string.Join(", ", NadekoBot.Config.PokemonTypes.Select(t => t.Name.ToUpperInvariant()))).ConfigureAwait(false);
                            return;
                        }
                        if (targetType == GetPokeType(e.User.Id))
                        {
                            await e.Channel.SendMessage($"Your type is already {targetType.Name.ToLowerInvariant()}{targetType.Icon}").ConfigureAwait(false);
                            return;
                        }

                        //Payment~
                        var amount = 1;
                        var pts = DbHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                        if (pts < amount)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} you don't have enough {NadekoBot.Config.CurrencyName}s! \nYou still need {amount - pts} {NadekoBot.Config.CurrencySign} to be able to do this!").ConfigureAwait(false);
                            return;
                        }
                        await FlowersHandler.RemoveFlowers(e.User, $"set usertype to {targetTypeStr}", amount).ConfigureAwait(false);
                        //Actually changing the type here
                        var preTypes = DbHandler.Instance.GetAllRows<UserPokeTypes>();
                        Dictionary<long, int> Dict = preTypes.ToDictionary(x => x.UserId, y => y.Id.Value);
                        if (Dict.ContainsKey((long)e.User.Id))
                        {
                            //delete previous type
                            DbHandler.Instance.Delete<UserPokeTypes>(Dict[(long)e.User.Id]);
                        }

                        DbHandler.Instance.Connection.Insert(new UserPokeTypes
                        {
                            UserId = (long)e.User.Id,
                            type = targetType.Name
                        }, typeof(UserPokeTypes));

                        //Now for the response

                        await e.Channel.SendMessage($"Set type of {e.User.Mention} to {targetTypeStr}{targetType.Icon} for a {NadekoBot.Config.CurrencySign}").ConfigureAwait(false);
                    });
            });
        }
    }
}




