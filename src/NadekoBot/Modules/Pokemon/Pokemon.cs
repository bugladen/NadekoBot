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

namespace NadekoBot.Modules.Pokemon
{
    [NadekoModule("Pokemon", ">")]
    public class Pokemon : NadekoModule
    {
        private static readonly List<PokemonType> _pokemonTypes = new List<PokemonType>();
        private static readonly ConcurrentDictionary<ulong, PokeStats> _stats = new ConcurrentDictionary<ulong, PokeStats>();
        
        public const string PokemonTypesFile = "data/pokemon_types.json";

        private new static Logger _log { get; }

        static Pokemon()
        {
            _log = LogManager.GetCurrentClassLogger();
            if (File.Exists(PokemonTypesFile))
            {
                _pokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                _log.Warn(PokemonTypesFile + " is missing. Pokemon types not loaded.");
            }
        }


        private int GetDamage(PokemonType usertype, PokemonType targetType)
        {
            var rng = new Random();
            var damage = rng.Next(40, 60);
            foreach (var multiplierObj in usertype.Multipliers)
            {
                if (multiplierObj.Type != targetType.Name) continue;
                damage = (int)(damage * multiplierObj.Multiplication);
            }

            return damage;
        }
            

        private static PokemonType GetPokeType(ulong id)
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
            var count = _pokemonTypes.Count;

            var remainder = Math.Abs((int)(id % (ulong)count));

            return _pokemonTypes[remainder];
        }
        
        private static PokemonType StringToPokemonType(string v)
        {
            var str = v?.ToUpperInvariant();
            var list = _pokemonTypes;
            foreach (var p in list)
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
                await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
                return;
            }
            if (targetUser == user)
            {
                await ReplyErrorLocalized("cant_attack_yourself").ConfigureAwait(false);
                return;
            }

                   
            // Checking stats first, then move
            //Set up the userstats
            var userStats = _stats.GetOrAdd(user.Id, new PokeStats());

            //Check if able to move
            //User not able if HP < 0, has made more than 4 attacks
            if (userStats.Hp < 0)
            {
                await ReplyErrorLocalized("you_fainted").ConfigureAwait(false);
                return;
            }
            if (userStats.MovesMade >= 5)
            {
                await ReplyErrorLocalized("too_many_moves").ConfigureAwait(false);
                return;
            }
            if (userStats.LastAttacked.Contains(targetUser.Id))
            {
                await ReplyErrorLocalized("cant_attack_again").ConfigureAwait(false);
                return;
            }
            //get target stats
            var targetStats = _stats.GetOrAdd(targetUser.Id, new PokeStats());

            //If target's HP is below 0, no use attacking
            if (targetStats.Hp <= 0)
            {
                await ReplyErrorLocalized("too_many_moves", targetUser).ConfigureAwait(false);
                return;
            }

            //Check whether move can be used
            PokemonType userType = GetPokeType(user.Id);

            var enabledMoves = userType.Moves;
            if (!enabledMoves.Contains(move.ToLowerInvariant()))
            {
                await ReplyErrorLocalized("invalid_move", Format.Bold(move), Prefix).ConfigureAwait(false);
                return;
            }

            //get target type
            PokemonType targetType = GetPokeType(targetUser.Id);
            //generate damage
            int damage = GetDamage(userType, targetType);
            //apply damage to target
            targetStats.Hp -= damage;
            
            var response = GetText("attack", Format.Bold(move), userType.Icon, Format.Bold(targetUser.ToString()), targetType.Icon, Format.Bold(damage.ToString()));

            //Damage type
            if (damage < 40)
            {
                response += "\n" + GetText("not_effective");
            }
            else if (damage > 60)
            {
                response += "\n" + GetText("super_effective");
            }
            else
            {
                response += "\n" + GetText("somewhat_effective");
            }

            //check fainted

            if (targetStats.Hp <= 0)
            {
                response += "\n" + GetText("fainted", Format.Bold(targetUser.ToString()));
            }
            else
            {
                response += "\n" + GetText("hp_remaining", Format.Bold(targetUser.ToString()), targetStats.Hp);
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
            _stats[user.Id] = userStats;
            _stats[targetUser.Id] = targetStats;

            await Context.Channel.SendConfirmAsync(Context.User.Mention + " " + response).ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Movelist()
        {
            IGuildUser user = (IGuildUser)Context.User;

            var userType = GetPokeType(user.Id);
            var movesList = userType.Moves;
            var embed = new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("moves", userType))
                .WithDescription(string.Join("\n", movesList.Select(m => userType.Icon + " " + m)));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Heal(IGuildUser targetUser = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            if (targetUser == null)
            {
                await ReplyErrorLocalized("user_not_found").ConfigureAwait(false);
                return;
            }

            if (_stats.ContainsKey(targetUser.Id))
            {
                var targetStats = _stats[targetUser.Id];
                if (targetStats.Hp == targetStats.MaxHp)
                {
                    await ReplyErrorLocalized("already_full", Format.Bold(targetUser.ToString())).ConfigureAwait(false);
                    return;
                }
                //Payment~
                var amount = 1;

                var target = (targetUser.Id == user.Id) ? "yourself" : targetUser.Mention;
                if (amount > 0)
                {
                    if (!await CurrencyHandler.RemoveCurrencyAsync(user, $"Poke-Heal {target}", amount, true).ConfigureAwait(false))
                    {
                        await ReplyErrorLocalized("no_currency", NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }

                //healing
                targetStats.Hp = targetStats.MaxHp;
                if (targetStats.Hp < 0)
                {
                    //Could heal only for half HP?
                    _stats[targetUser.Id].Hp = (targetStats.MaxHp / 2);
                    if (target == "yourself")
                    {
                        await ReplyConfirmLocalized("revive_yourself", NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }

                    await ReplyConfirmLocalized("revive_other", Format.Bold(targetUser.ToString()), NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
                }
                await ReplyConfirmLocalized("healed", Format.Bold(targetUser.ToString()), NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
            }
            else
            {
                await ErrorLocalized("already_full", Format.Bold(targetUser.ToString()));
            }
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Type(IGuildUser targetUser = null)
        {
            targetUser = targetUser ?? (IGuildUser)Context.User;
            var pType = GetPokeType(targetUser.Id);
            await ReplyConfirmLocalized("type_of_user", Format.Bold(targetUser.ToString()), pType).ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Settype([Remainder] string typeTargeted = null)
        {
            IGuildUser user = (IGuildUser)Context.User;

            var targetType = StringToPokemonType(typeTargeted);
            if (targetType == null)
            {
                await Context.Channel.EmbedAsync(_pokemonTypes.Aggregate(new EmbedBuilder().WithDescription("List of the available types:"), 
                        (eb, pt) => eb.AddField(efb => efb.WithName(pt.Name)
                                                          .WithValue(pt.Icon)
                                                          .WithIsInline(true)))
                            .WithColor(NadekoBot.OkColor)).ConfigureAwait(false);
                return;
            }
            if (targetType == GetPokeType(user.Id))
            {
                await ReplyErrorLocalized("already_that_type", targetType).ConfigureAwait(false);
                return;
            }

            //Payment~
            var amount = 1;
            if (amount > 0)
            {
                if (!await CurrencyHandler.RemoveCurrencyAsync(user, $"{user} change type to {typeTargeted}", amount, true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalized("no_currency", NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
            }

            //Actually changing the type here

            using (var uow = DbHandler.UnitOfWork())
            {
                var pokeUsers = uow.PokeGame.GetAll().ToArray();
                var setTypes = pokeUsers.ToDictionary(x => x.UserId, y => y.type);
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
                    var pokeUserCmd = pokeUsers.FirstOrDefault(p => p.UserId == user.Id);
                    pokeUserCmd.type = targetType.Name;
                    uow.PokeGame.Update(pokeUserCmd);
                }
                await uow.CompleteAsync();
            }

            //Now for the response
            await ReplyConfirmLocalized("settype_success", 
                targetType, 
                NadekoBot.BotConfig.CurrencySign).ConfigureAwait(false);
        }
    }
}




