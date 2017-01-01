using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System.Linq;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NLog;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Concurrent;
using static NadekoBot.Modules.Gambling.Gambling;

namespace NadekoBot.Modules.Pokemon
{
    [NadekoModule("Pokemon", ">")]
    public partial class Pokemon : DiscordModule
    {
        private static List<PokemonType> PokemonTypes = new List<PokemonType>();
        private static ConcurrentDictionary<ulong, PokeStats> Stats = new ConcurrentDictionary<ulong, PokeStats>();
        
        public const string PokemonTypesFile = "data/pokemon_types.json";

        private static new Logger _log { get; }

        static Pokemon()
        {
            _log = LogManager.GetCurrentClassLogger();
            if (File.Exists(PokemonTypesFile))
            {
                PokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                _log.Warn(PokemonTypesFile + " is missing. Pokemon types not loaded.");
            }
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

            Dictionary<ulong, string> setTypes;
            using (var uow = DbHandler.UnitOfWork())
            {
                setTypes = uow.PokeGame.GetAll().ToDictionary(x => x.UserId, y => y.type);
            }

            if (setTypes.ContainsKey(id))
            {
                return StringToPokemonType(setTypes[id]);
            }
            int count = PokemonTypes.Count;

            int remainder = Math.Abs((int)(id % (ulong)count));

            return PokemonTypes[remainder];
        }



        private PokemonType StringToPokemonType(string v)
        {
            var str = v?.ToUpperInvariant();
            var list = PokemonTypes;
            foreach (PokemonType p in list)
            {
                if (str == p.Name)
                {
                    return p;
                }
            }
            return null;
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Attack(string move, IGuildUser targetUser = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            if (string.IsNullOrWhiteSpace(move)) {
                return;
            }

            if (targetUser == null)
            {
                await Context.Channel.SendMessageAsync("No such person.").ConfigureAwait(false);
                return;
            }
            else if (targetUser == user)
            {
                await Context.Channel.SendMessageAsync("You can't attack yourself.").ConfigureAwait(false);
                return;
            }

                   
            // Checking stats first, then move
            //Set up the userstats
            PokeStats userStats;
            userStats = Stats.GetOrAdd(user.Id, new PokeStats());

            //Check if able to move
            //User not able if HP < 0, has made more than 4 attacks
            if (userStats.Hp < 0)
            {
                await Context.Channel.SendMessageAsync($"{user.Mention} has fainted and was not able to move!").ConfigureAwait(false);
                return;
            }
            if (userStats.MovesMade >= 5)
            {
                await Context.Channel.SendMessageAsync($"{user.Mention} has used too many moves in a row and was not able to move!").ConfigureAwait(false);
                return;
            }
            if (userStats.LastAttacked.Contains(targetUser.Id))
            {
                await Context.Channel.SendMessageAsync($"{user.Mention} can't attack again without retaliation!").ConfigureAwait(false);
                return;
            }
            //get target stats
            PokeStats targetStats;
            targetStats = Stats.GetOrAdd(targetUser.Id, new PokeStats());

            //If target's HP is below 0, no use attacking
            if (targetStats.Hp <= 0)
            {
                await Context.Channel.SendMessageAsync($"{targetUser.Mention} has already fainted!").ConfigureAwait(false);
                return;
            }

            //Check whether move can be used
            PokemonType userType = GetPokeType(user.Id);

            var enabledMoves = userType.Moves;
            if (!enabledMoves.Contains(move.ToLowerInvariant()))
            {
                await Context.Channel.SendMessageAsync($"{user.Mention} is not able to use **{move}**. Type {NadekoBot.ModulePrefixes[typeof(Pokemon).Name]}ml to see moves").ConfigureAwait(false);
                return;
            }

            //get target type
            PokemonType targetType = GetPokeType(targetUser.Id);
            //generate damage
            int damage = GetDamage(userType, targetType);
            //apply damage to target
            targetStats.Hp -= damage;

            var response = $"{user.Mention} used **{move}**{userType.Icon} on {targetUser.Mention}{targetType.Icon} for **{damage}** damage";

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
                response += $"\n**{targetUser.Mention}** has fainted!";
            }
            else
            {
                response += $"\n**{targetUser.Mention}** has {targetStats.Hp} HP remaining";
            }

            //update other stats
            userStats.LastAttacked.Add(targetUser.Id);
            userStats.MovesMade++;
            targetStats.MovesMade = 0;
            if (targetStats.LastAttacked.Contains(user.Id))
            {
                targetStats.LastAttacked.Remove(user.Id);
            }

            //update dictionary
            //This can stay the same right?
            Stats[user.Id] = userStats;
            Stats[targetUser.Id] = targetStats;

            await Context.Channel.SendMessageAsync(response).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Movelist()
        {
            IGuildUser user = (IGuildUser)Context.User;

            var userType = GetPokeType(user.Id);
            var movesList = userType.Moves;
            var str = $"**Moves for `{userType.Name}` type.**";
            foreach (string m in movesList)
            {
                str += $"\n{userType.Icon}{m}";
            }
            await Context.Channel.SendMessageAsync(str).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Heal(IGuildUser targetUser = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            if (targetUser == null) {
                await Context.Channel.SendMessageAsync("No such person.").ConfigureAwait(false);
                return;
            }

            if (Stats.ContainsKey(targetUser.Id))
            {
                var targetStats = Stats[targetUser.Id];
                if (targetStats.Hp == targetStats.MaxHp)
                {
                    await Context.Channel.SendMessageAsync($"{targetUser.Mention} already has full HP!").ConfigureAwait(false);
                    return;
                }
                //Payment~
                var amount = 1;

                var target = (targetUser.Id == user.Id) ? "yourself" : targetUser.Mention;
                if (amount > 0)
                {
                        if (!await CurrencyHandler.RemoveCurrencyAsync(user, $"Poke-Heal {target}", amount, true).ConfigureAwait(false))
                        {
                            try { await Context.Channel.SendMessageAsync($"{user.Mention} You don't have enough {CurrencyName}s.").ConfigureAwait(false); } catch { }
                            return;
                        }
                }

                //healing
                targetStats.Hp = targetStats.MaxHp;
                if (targetStats.Hp < 0)
                {
                    //Could heal only for half HP?
                    Stats[targetUser.Id].Hp = (targetStats.MaxHp / 2);
                    if (target == "yourself")
                    {
                        await Context.Channel.SendMessageAsync($"You revived yourself with one {CurrencySign}").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{user.Mention} revived {targetUser.Mention} with one {CurrencySign}").ConfigureAwait(false);
                    }
                   return;
                }
                await Context.Channel.SendMessageAsync($"{user.Mention} healed {targetUser.Mention} with one {CurrencySign}").ConfigureAwait(false);
                return;
            }
            else
            {
                await Context.Channel.SendMessageAsync($"{targetUser.Mention} already has full HP!").ConfigureAwait(false);
            }
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Type(IGuildUser targetUser = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            if (targetUser == null)
            {
                return;
            }

            var pType = GetPokeType(targetUser.Id);
            await Context.Channel.SendMessageAsync($"Type of {targetUser.Mention} is **{pType.Name.ToLowerInvariant()}**{pType.Icon}").ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Settype([Remainder] string typeTargeted = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            var targetType = StringToPokemonType(typeTargeted);
            if (targetType == null)
            {
                await Context.Channel.EmbedAsync(PokemonTypes.Aggregate(new EmbedBuilder().WithDescription("List of the available types:"), 
                        (eb, pt) => eb.AddField(efb => efb.WithName(pt.Name)
                                                          .WithValue(pt.Icon)
                                                          .WithIsInline(true)))
                            .WithColor(NadekoBot.OkColor)).ConfigureAwait(false);
                return;
            }
            if (targetType == GetPokeType(user.Id))
            {
                await Context.Channel.SendMessageAsync($"Your type is already {targetType.Name.ToLowerInvariant()}{targetType.Icon}").ConfigureAwait(false);
                return;
            }

            //Payment~
            var amount = 1;
            if (amount > 0)
            {
                if (!await CurrencyHandler.RemoveCurrencyAsync(user, $"{user.Mention} change type to {typeTargeted}", amount, true).ConfigureAwait(false))
                {
                    try { await Context.Channel.SendMessageAsync($"{user.Mention} You don't have enough {CurrencyName}s.").ConfigureAwait(false); } catch { }
                    return;
                }
            }

            //Actually changing the type here
            Dictionary<ulong, string> setTypes;

            using (var uow = DbHandler.UnitOfWork())
            {
                var pokeUsers = uow.PokeGame.GetAll();
                setTypes = pokeUsers.ToDictionary(x => x.UserId, y => y.type);
                var pt = new UserPokeTypes
                {
                    UserId = user.Id,
                    type = targetType.Name,
                };
                if (!setTypes.ContainsKey(user.Id))
                {
                    //create user in db
                    uow.PokeGame.Add(pt);
                }
                else
                {
                    //update user in db
                    var pokeUserCmd = pokeUsers.Where(p => p.UserId == user.Id).FirstOrDefault();
                    pokeUserCmd.type = targetType.Name;
                    uow.PokeGame.Update(pokeUserCmd);
                }
                await uow.CompleteAsync();
            }

            //Now for the response
            await Context.Channel.SendMessageAsync($"Set type of {user.Mention} to {typeTargeted}{targetType.Icon} for a {CurrencySign}").ConfigureAwait(false);
        }

    }
}




