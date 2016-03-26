using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Modules;
using Discord.Commands;
using NadekoBot.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using NadekoBot.Classes._DataModels;

namespace NadekoBot.Modules.pokegame
{
    class Pokegame : DiscordModule
    {
        public override string Prefix { get; } = "poke";
        public readonly int BASEHEALTH = 500;
        private Dictionary<ulong, Pokestats> stats = new Dictionary<ulong, Pokestats>();

        public Pokegame()
        {
            //Something?
        }
        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "attack")
                .Description("Attacks a target with the given move")
                .Parameter("move", ParameterType.Required)
                .Parameter("target", ParameterType.Required)
                .Do(async e =>
                {
                    var move = e.GetArg("move");
                    var target = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                    if (target == null)
                    {
                        await e.Channel.SendMessage("No such person.");
                        return;
                    }
                    // Checking stats first, then move
                    //Set up the userstats
                    Pokestats userstats;
                    if (stats.ContainsKey(e.User.Id))
                    {
                        userstats = stats[e.User.Id];
                    } else
                    {
                        //not really necessary now
                        userstats = defaultStats();
                        stats.Add(e.User.Id, userstats);
                    }
                    //Check if able to move
                    //User not able if HP < 0, has made more than 4 attacks
                    if (userstats.HP < 0)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} is fainted and was not able to move!");
                        return;
                    }
                    if (userstats.movesMade >= 5)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} has used too many moves in a row and was not able to move!");
                        return;
                    }
                    if (userstats.lastAttacked.Contains(target.Id))
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} can't attack again without retaliation!");
                        return;
                    }
                    //get target stats
                    Pokestats targetstats;
                    if (stats.ContainsKey(target.Id))
                    {
                        targetstats = stats[target.Id];
                    }
                    else
                    {
                        targetstats = defaultStats();
                        stats.Add(target.Id, targetstats);
                    }
                    //If target's HP is below 0, no use attacking
                    if (targetstats.HP < 0)
                    {
                        await e.Channel.SendMessage($"{target.Name} has already fainted!");
                        return;
                    }

                    //Check whether move can be used
                    pokegame.PokemonTypes.PokeType usertype = getPokeType(e.User.Id);

                    var EnabledMoves = usertype.getMoves();
                    if (!EnabledMoves.Contains(move.ToLowerInvariant()))
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} was not able to use {move}, use {Prefix}listmoves to see moves you can use");
                        return;
                    }

                    //get target type
                    pokegame.PokemonTypes.PokeType targetType = getPokeType(target.Id);
                    //generate damage
                    int damage = getDamage(usertype, targetType);
                    //apply damage to target
                    targetstats.HP -= damage;

                    var response = $"{e.User.Mention} used {move} on {target.Name} for {damage} damage";
                    
                    //Damage type
                    if (damage < 40)
                    {
                        response += "\nIt's not effective..";
                    } else if (damage > 60)
                    {
                        response += "\nIt's super effective!";
                    }
                    else
                    {
                        response += "\nIt's somewhat effective";
                    }
                    
                    //check fainted
                    
                    if (targetstats.HP < 0)
                    {
                        response += $"\n{target.Name} has fainted!";
                    }
                    else
                    {
                        response += $"\n{target.Name} has {targetstats.HP} HP remaining";
                    }

                    //update other stats
                    userstats.lastAttacked.Add(target.Id);
                    userstats.movesMade++;
                    targetstats.movesMade = 0;
                    if (targetstats.lastAttacked.Contains(e.User.Id))
                    {
                        targetstats.lastAttacked.Remove(e.User.Id);
                    }

                    //update dictionary
                    stats[e.User.Id] = userstats;
                    stats[target.Id] = targetstats;

                    await e.Channel.SendMessage(response);
                });

                cgb.CreateCommand(Prefix + "listmoves")
                .Description("Lists the moves you are able to use")
                .Do(async e =>
                {
                    var userType = getPokeType(e.User.Id);
                    var movesList = userType.getMoves();
                    var str = "**Moves:**";
                    foreach (string m in movesList)
                    {
                        str += $"\n{userType.getImage()}{m}";
                    }
                    await e.Channel.SendMessage(str);
                });

                cgb.CreateCommand(Prefix + "addmove")
                .Description($"Adds move given to database.\n**Usage**: {Prefix}addmove flame fire")
                .Parameter("movename", ParameterType.Required)
                .Parameter("movetype", ParameterType.Required)
                .Do(async e => {
                    //Implement NadekoFlowers????
                    string newMove = e.GetArg("move");
                    var newType = PokemonTypes.stringToPokeType( e.GetArg("type").ToUpperInvariant());
                    int typeNum = newType.getNum();
                    var db = DbHandler.Instance.GetAllRows<PokeMoves>().Select(x => x.move);
                    if (db.Contains(newMove))
                    {
                        await e.Channel.SendMessage($"{newMove} already exists");
                        return;
                    }
                    await Task.Run(() =>
                    {
                        DbHandler.Instance.InsertData(new Classes._DataModels.PokeMoves
                        {
                            move = newMove,
                            type = typeNum
                        });
                    });
                    await e.Channel.SendMessage($"Added {newType.getImage()}{newMove}");
                });

                cgb.CreateCommand(Prefix + "heal")
                 .Description($"Heals someone. Revives those that fainted. Costs a NadekoFlower \n**Usage**:{Prefix}revive @someone")
                 .Parameter("target", ParameterType.Required)
                 .Do(async e =>
                 {
                     var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                     if (usr == null)
                     {
                         await e.Channel.SendMessage("No such person.");
                         return;
                     }
                     if (stats.ContainsKey(usr.Id))
                     {
                         
                         var targetStats = stats[usr.Id];
                         int HP = targetStats.HP;
                         if (targetStats.HP == BASEHEALTH)
                         {
                             await e.Channel.SendMessage($"{usr.Name} already has full HP!");
                             return;
                         }
                         //Payment~
                         var amount = 1;
                         var pts = Classes.DbHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                         if (pts < amount)
                         {
                             await e.Channel.SendMessage($"{e.User.Mention} you don't have enough NadekoFlowers! \nYou still need {amount - pts} to be able to do this!");
                             return;
                         }
                         var up = (usr.Id == e.User.Id) ? "yourself" : usr.Name;
                         await FlowersHandler.RemoveFlowersAsync(e.User, $"heal {up}", amount);
                         //healing
                         targetStats.HP = BASEHEALTH;
                         if (HP < 0)
                         {
                             //Could heal only for half HP?
                             stats[usr.Id].HP = (BASEHEALTH / 2);
                             await e.Channel.SendMessage($"{e.User.Name} revived {usr.Name} for 🌸");
                             return;
                         }
                         await e.Channel.SendMessage($"{e.User.Name} healed {usr.Name} for {BASEHEALTH - HP} HP with a 🌸");
                         return;
                     }
                     else
                     {
                         await e.Channel.SendMessage($"{usr.Name} already has full HP!");
                     }
                 });

                cgb.CreateCommand(Prefix + "type")
                .Description($"Get the poketype of the target.\n**Usage**: {Prefix}type @someone")
                .Parameter("target", ParameterType.Required)
                .Do(async e =>
                {
                    var usr = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                    if (usr == null)
                    {
                        await e.Channel.SendMessage("No such person.");
                        return;
                    }
                    var pType = getPokeType(usr.Id);
                    await e.Channel.SendMessage($"Type of {usr.Name} is **{pType.getName().ToLowerInvariant()}**{pType.getImage()}");

                });

                cgb.CreateCommand(Prefix + "settype")
                .Description($"Set your poketype. Costs a NadekoFlower.\n**Usage**: {Prefix}settype fire")
                .Parameter("targetType", ParameterType.Required)
                .Do(async e =>
                {
                    var targetTypeString = e.GetArg("targetType");
                    var targetType = PokemonTypes.stringToPokeType(targetTypeString);
                    if (targetType == null)
                    {
                        await e.Channel.SendMessage("Invalid type specified. Type must be one of:\nNORMAL, FIRE, WATER, ELECTRIC, GRASS, ICE, FIGHTING, POISON, GROUND, FLYING, PSYCHIC, BUG, ROCK, GHOST, DRAGON, DARK, STEEL");
                    }
                    //Payment~
                    var amount = 1;
                    var pts = Classes.DbHandler.Instance.GetStateByUserId((long)e.User.Id)?.Value ?? 0;
                    if (pts < amount)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} you don't have enough NadekoFlowers! \nYou still need {amount - pts} to be able to do this!");
                        return;
                    }
                    await FlowersHandler.RemoveFlowersAsync(e.User, $"set usertype to {targetTypeString}", amount);
                    //Actually changing the type here
                    var preTypes = DbHandler.Instance.GetAllRows<PokeTypes>();
                    Dictionary<long, int> Dict = preTypes.ToDictionary(x => x.UserId, y => y.Id);
                    if (Dict.ContainsKey((long)e.User.Id))
                    {
                        //delete previous type
                        DbHandler.Instance.Delete<PokeTypes>(Dict[(long)e.User.Id]);
                    }

                    DbHandler.Instance.InsertData(new Classes._DataModels.PokeTypes
                    {
                        UserId = (long)e.User.Id,
                        type = targetType.getNum()
                    });
                });
            });
        }

        private int getDamage(PokemonTypes.PokeType usertype, PokemonTypes.PokeType targetType)
        {
            Random rng = new Random();
            int damage = rng.Next(40, 60);
            double multiplier = 1;
            multiplier = usertype.getMagnifier(targetType);
            damage = (int)(damage * multiplier);
            return damage;
        }

        private pokegame.PokemonTypes.PokeType getPokeType(ulong id)
        {

            var db = DbHandler.Instance.GetAllRows<PokeTypes>();
            Dictionary<long, int> setTypes = db.ToDictionary(x => x.UserId, y => y.type);
            if (setTypes.ContainsKey((long)id))
            {
                return PokemonTypes.intToPokeType(setTypes[(long) id]);
            }

            int remainder = (int) id % 16;

            return PokemonTypes.intToPokeType(remainder);
           

        }

        private Pokestats defaultStats()
        {
            Pokestats s = new Pokestats();
            s.HP = BASEHEALTH;
            return s;
        }

        
    }
}
