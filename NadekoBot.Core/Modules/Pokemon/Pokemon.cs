using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Pokemon.Common;
using NadekoBot.Modules.Pokemon.Services;

namespace NadekoBot.Modules.Pokemon
{
    public class Pokemon : NadekoTopLevelModule<PokemonService>
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;

        public Pokemon(DbService db, ICurrencyService cs)
        {
            _db = db;
            _cs = cs;
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

        private PokemonType GetPokeType(ulong id)
        {
            Dictionary<ulong, string> setTypes;
            using (var uow = _db.UnitOfWork)
            {
                setTypes = uow.PokeGame.GetAll().ToDictionary(x => x.UserId, y => y.type);
            }

            if (setTypes.ContainsKey(id))
            {
                return StringToPokemonType(setTypes[id]);
            }
            var count = _service.PokemonTypes.Count;

            var remainder = Math.Abs((int)(id % (ulong)count));

            return _service.PokemonTypes[remainder];
        }

        private PokemonType StringToPokemonType(string v)
        {
            var str = v?.ToUpperInvariant();
            var list = _service.PokemonTypes;
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

            if (string.IsNullOrWhiteSpace(move))
            {
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
            var userStats = _service.Stats.GetOrAdd(user.Id, new PokeStats());

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
            var targetStats = _service.Stats.GetOrAdd(targetUser.Id, new PokeStats());

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
            _service.Stats[user.Id] = userStats;
            _service.Stats[targetUser.Id] = targetStats;

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

            if (_service.Stats.ContainsKey(targetUser.Id))
            {
                var targetStats = _service.Stats[targetUser.Id];
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
                    if (!await _cs.RemoveAsync(user, $"Poke-Heal {target}", amount, true).ConfigureAwait(false))
                    {
                        await ReplyErrorLocalized("no_currency", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }

                //healing
                targetStats.Hp = targetStats.MaxHp;
                if (targetStats.Hp < 0)
                {
                    //Could heal only for half HP?
                    _service.Stats[targetUser.Id].Hp = (targetStats.MaxHp / 2);
                    if (target == "yourself")
                    {
                        await ReplyConfirmLocalized("revive_yourself", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }

                    await ReplyConfirmLocalized("revive_other", Format.Bold(targetUser.ToString()), _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                }
                await ReplyConfirmLocalized("healed", Format.Bold(targetUser.ToString()), _bc.BotConfig.CurrencySign).ConfigureAwait(false);
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
                await Context.Channel.EmbedAsync(_service.PokemonTypes.Aggregate(new EmbedBuilder().WithDescription("List of the available types:"),
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
                if (!await _cs.RemoveAsync(user, $"{user} change type to {typeTargeted}", amount, true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalized("no_currency", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }
            }

            //Actually changing the type here

            using (var uow = _db.UnitOfWork)
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
                _bc.BotConfig.CurrencySign).ConfigureAwait(false);
        }
    }
}
