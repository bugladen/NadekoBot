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
using NadekoBot.Classes.Permissions;
using System.Collections.Concurrent;
using NadekoBot.Modules.Pokemon.PokeTypes;

namespace NadekoBot.Modules.Pokemon
{

    class PokemonGame : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Pokemon;
        public readonly int BASEHEALTH = 500;
        //private Dictionary<ulong, Pokestats> stats = new Dictionary<ulong, Pokestats>();
        private ConcurrentDictionary<ulong, Pokestats> stats = new ConcurrentDictionary<ulong, Pokestats>();


        public PokemonGame()
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
                .Parameter("target", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var move = e.GetArg("move");
                    Discord.User target = null;
                    if (!string.IsNullOrWhiteSpace(e.GetArg("target")))
                    {

                        target = e.Server.FindUsers(e.GetArg("target")).FirstOrDefault();
                        if (target == null)
                        {
                            await e.Channel.SendMessage("No such person.");
                            return;
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessage("No such person.");
                        return;
                    }
                    // Checking stats first, then move
                    //Set up the userstats
                    Pokestats userstats;
                    userstats = stats.GetOrAdd(e.User.Id, defaultStats());

                    //Check if able to move
                    //User not able if HP < 0, has made more than 4 attacks
                    if (userstats.HP < 0)
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} has fainted and was not able to move!");
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
                    targetstats = stats.GetOrAdd(target.Id, defaultStats());

                    //If target's HP is below 0, no use attacking
                    if (targetstats.HP <= 0)
                    {
                        await e.Channel.SendMessage($"{target.Mention} has already fainted!");
                        return;
                    }

                    //Check whether move can be used
                    IPokeType usertype = getPokeType(e.User.Id);

                    var EnabledMoves = usertype.getMoves();
                    if (!EnabledMoves.Contains(move.ToLowerInvariant()))
                    {
                        await e.Channel.SendMessage($"{e.User.Mention} was not able to use **{move}**, use {Prefix}listmoves to see moves you can use");
                        return;
                    }

                    //get target type
                    IPokeType targetType = getPokeType(target.Id);
                    //generate damage
                    int damage = getDamage(usertype, targetType);
                    //apply damage to target
                    targetstats.HP -= damage;

                    var response = $"{e.User.Mention} used **{move}**{usertype.getImage()} on {target.Mention}{targetType.getImage()} for **{damage}** damage";

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

                    if (targetstats.HP <= 0)
                    {
                        response += $"\n**{target.Name}** has fainted!";
                    }
                    else
                    {
                        response += $"\n**{target.Name}** has {targetstats.HP} HP remaining";
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
                    //This can stay the same right?
                    stats[e.User.Id] = userstats;
                    stats[target.Id] = targetstats;

                    await e.Channel.SendMessage(response);
                });

                cgb.CreateCommand(Prefix + "listmoves")
                .Description("Lists the moves you are able to use")
                .Do(async e =>
                {
                    var userType = getPokeType(e.User.Id);
                    List<string> movesList = userType.getMoves();
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
                .Do(async e =>
                {
                    //Implement NadekoFlowers????
                    string newMove = e.GetArg("movename").ToLowerInvariant();
                    var newType = PokemonTypesMain.stringToPokeType(e.GetArg("movetype").ToUpperInvariant());
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

                cgb.CreateCommand(Prefix + "defaultmoves")
                .Description($"Sets the moves DB to the default state **OWNER ONLY**")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    //clear DB
                    var db = DbHandler.Instance.GetAllRows<PokeMoves>();
                    foreach (PokeMoves p in db)
                    {
                        DbHandler.Instance.Delete<PokeMoves>(p.Id);
                    }

                    Dictionary<string, int> defaultmoves = new Dictionary<string, int>()
                    {
                        {"sonic boom",0},
                        {"quick attack",0},
                        {"doubleslap",0},
                        {"headbutt",0},
                        {"incinerate",1},
                        {"ember",1},
                        {"fire punch",1},
                        {"fiery dance",1},
                        {"bubblebeam",2},
                        {"dive",2},
                        {"whirlpool",2},
                        {"aqua tail",2},
                        {"nuzzle",3},
                        {"thunderbolt",3},
                        {"thundershock",3},
                        {"discharge",3},
                        {"absorb",4},
                        {"mega drain",4},
                        {"vine whip",4},
                        {"razor leaf",4},
                        {"ice ball",5},
                        {"powder snow",5},
                        {"avalanche",5},
                        {"icy wind",5},
                        {"low kick",6},
                        {"force palm",6},
                        {"mach punch",6},
                        {"double kick",6},
                        {"acid",7},
                        {"smog",7},
                        {"sludge",7},
                        {"poison jab",7},
                        {"mud-slap",8},
                        {"boomerang",8},
                        {"bulldoze",8},
                        {"dig",8},
                        {"peck",9},
                        {"pluck",9},
                        {"gust",9},
                        {"aerial ace",9},
                        {"confusion",10},
                        {"psybeam",10},
                        {"psywave",10},
                        {"heart stamp",10},
                        {"bug bite",11},
                        {"infestation",11},
                        {"x-scissor",11},
                        {"twineedle",11},
                        {"rock throw",12},
                        {"rollout",12},
                        {"rock tomb",12},
                        {"rock blast",12},
                        {"astonish",13},
                        {"night shade",13},
                        {"lick",13},
                        {"ominous wind",13},
                        {"hex",13},
                        {"dragon tail",14},
                        {"dragon rage",14},
                        {"dragonbreath",14},
                        {"twister",14},
                        {"pursuit",15},
                        {"assurance",15},
                        {"bite",15},
                        {"faint attack",15},
                        {"bullet punch",16},
                        {"metal burst",16},
                        {"gear grind",16},
                        {"magnet bomb",16}
                    };

                    foreach (KeyValuePair<string, int> entry in defaultmoves)
                    {
                        DbHandler.Instance.InsertData(new Classes._DataModels.PokeMoves
                        {
                            move = entry.Key,
                            type = entry.Value
                        });
                    }

                    var str = "Reset moves.\n**Moves:**";
                    //could sort, but meh
                    var dbMoves = DbHandler.Instance.GetAllRows<PokeMoves>();
                    foreach (PokeMoves m in dbMoves)
                    {
                        var t = PokemonTypesMain.intToPokeType(m.type);

                        str += $"\n{t.getImage()}{m.move}";
                    }

                    await e.Channel.SendMessage(str);

                });

                cgb.CreateCommand(Prefix + "settype")
                .Description($"Set your poketype. Costs a NadekoFlower.\n**Usage**: {Prefix}settype fire")
                .Parameter("targetType", ParameterType.Required)
                .Do(async e =>
                {
                    var targetTypeString = e.GetArg("targetType");
                    var targetType = PokemonTypesMain.stringToPokeType(targetTypeString.ToUpperInvariant());
                    if (targetType == null)
                    {
                        await e.Channel.SendMessage("Invalid type specified. Type must be one of:\nNORMAL, FIRE, WATER, ELECTRIC, GRASS, ICE, FIGHTING, POISON, GROUND, FLYING, PSYCHIC, BUG, ROCK, GHOST, DRAGON, DARK, STEEL");
                        return;
                    }
                    if (targetType == getPokeType(e.User.Id))
                    {
                        await e.Channel.SendMessage($"Your type is already {targetType.getName().ToLowerInvariant()}{targetType.getImage()}");
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
                    await FlowersHandler.RemoveFlowersAsync(e.User, $"set usertype to {targetTypeString}", amount);
                    //Actually changing the type here
                    var preTypes = DbHandler.Instance.GetAllRows<userPokeTypes>();
                    Dictionary<long, int> Dict = preTypes.ToDictionary(x => x.UserId, y => y.Id);
                    if (Dict.ContainsKey((long)e.User.Id))
                    {
                        //delete previous type
                        DbHandler.Instance.Delete<userPokeTypes>(Dict[(long)e.User.Id]);
                    }

                    DbHandler.Instance.InsertData(new Classes._DataModels.userPokeTypes
                    {
                        UserId = (long)e.User.Id,
                        type = targetType.getNum()
                    });

                    //Now for the response

                    await e.Channel.SendMessage($"Set type of {e.User.Mention} to {targetTypeString}{targetType.getImage()} for a 🌸");
                });
            });
        }




        private int getDamage(IPokeType usertype, IPokeType targetType)
        {
            Random rng = new Random();
            int damage = rng.Next(40, 60);
            double multiplier = 1;
            multiplier = usertype.getMagnifier(targetType);
            damage = (int)(damage * multiplier);
            return damage;
        }

        private IPokeType getPokeType(ulong id)
        {

            var db = DbHandler.Instance.GetAllRows<userPokeTypes>();
            Dictionary<long, int> setTypes = db.ToDictionary(x => x.UserId, y => y.type);
            if (setTypes.ContainsKey((long)id))
            {
                return PokemonTypesMain.intToPokeType(setTypes[(long)id]);
            }

            int remainder = (int)id % 16;

            return PokemonTypesMain.intToPokeType(remainder);


        }

        private Pokestats defaultStats()
        {
            Pokestats s = new Pokestats();
            s.HP = BASEHEALTH;
            return s;
        }


    }
    class Pokestats
    {
        //Health left
        public int HP { get; set; } = 500;
        //Amount of moves made since last time attacked
        public int movesMade { get; set; } = 0;
        //Last people attacked
        public List<ulong> lastAttacked { get; set; } = new List<ulong>();
    }
}


//Not sure this is what you wanted?




