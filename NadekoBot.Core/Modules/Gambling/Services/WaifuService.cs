using Discord;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Gambling.Services
{
    public class WaifuService : INService
    {
        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IBotConfigProvider _bc;

        public WaifuService(DbService db, ICurrencyService cs, IBotConfigProvider bc)
        {
            _db = db;
            _cs = cs;
            _bc = bc;
        }

        public async Task<bool> WaifuTransfer(IUser owner, ulong waifuId, IUser newOwner)
        {
            if (owner.Id == newOwner.Id || waifuId == newOwner.Id)
                return false;
            using (var uow = _db.UnitOfWork)
            {
                var waifu = uow.Waifus.ByWaifuUserId(waifuId);
                var ownerUser = uow.DiscordUsers.GetOrCreate(owner);

                // owner has to be the owner of the waifu
                if (waifu == null || waifu.ClaimerId != ownerUser.Id)
                    return false;

                if (!await _cs.RemoveAsync(owner.Id, "Waifu Transfer",  
                    waifu.Price / 10, gamble: true))
                {
                    return false;
                }

                //new claimerId is the id of the new owner
                var newOwnerUser = uow.DiscordUsers.GetOrCreate(newOwner);
                waifu.ClaimerId = newOwnerUser.Id;

                await uow.CompleteAsync().ConfigureAwait(false);
            }

            return true;
        }

        public int GetResetPrice(IUser user)
        {
            using (var uow = _db.UnitOfWork)
            {
                var waifu = uow.Waifus.ByWaifuUserId(user.Id);
                var divorces = uow._context.WaifuUpdates.Count(x => x.Old != null &&
                        x.Old.UserId == user.Id &&
                        x.UpdateType == WaifuUpdateType.Claimed &&
                        x.New == null);
                var affs = uow._context.WaifuUpdates
                        .Where(w => w.User.UserId == user.Id && w.UpdateType == WaifuUpdateType.AffinityChanged && w.New != null)
                        .GroupBy(x => x.New)
                        .Count();
                
                return (int)Math.Ceiling(waifu.Price * 1.25f) + ((divorces + affs + 2) * _bc.BotConfig.DivorcePriceMultiplier);
            }
        }

        public async Task<bool> TryReset(IUser user)
        {
            using (var uow = _db.UnitOfWork)
            {
                var price = GetResetPrice(user);
                if (!await _cs.RemoveAsync(user.Id, "Waifu Reset", price, gamble: true))
                    return false;

                var affs = uow._context.WaifuUpdates
                    .Where(w => w.User.UserId == user.Id
                        && w.UpdateType == WaifuUpdateType.AffinityChanged
                        && w.New != null);

                var divorces = uow._context.WaifuUpdates.Where(x => x.Old != null &&
                        x.Old.UserId == user.Id &&
                        x.UpdateType == WaifuUpdateType.Claimed &&
                        x.New == null);

                //reset changes of heart to 0
                uow._context.WaifuUpdates.RemoveRange(affs);
                //reset divorces to 0
                uow._context.WaifuUpdates.RemoveRange(divorces);
                var waifu = uow.Waifus.ByWaifuUserId(user.Id);
                //reset price, remove items
                //remove owner, remove affinity
                waifu.Price = 50;
                waifu.Items.Clear();
                waifu.ClaimerId = null;
                waifu.AffinityId = null;

                //wives stay though

                uow.Complete();
            }
            return true;
        }
    }
}
