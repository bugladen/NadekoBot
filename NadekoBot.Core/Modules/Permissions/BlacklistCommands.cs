using Discord;
using Discord.Commands;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Modules.Permissions.Services;
using NadekoBot.Common.TypeReaders;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class BlacklistCommands : NadekoSubmodule<BlacklistService>
        {
            private readonly DbService _db;
            private readonly IBotCredentials _creds;

            private ConcurrentHashSet<ulong> BlacklistedUsers => _service.BlacklistedUsers;
            private ConcurrentHashSet<ulong> BlacklistedGuilds => _service.BlacklistedGuilds;
            private ConcurrentHashSet<ulong> BlacklistedChannels => _service.BlacklistedChannels;

            public BlacklistCommands(DbService db, IBotCredentials creds)
            {
                _db = db;
                _creds = creds;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task UserBlacklist(AddRemove action, IUser usr)
                => Blacklist(action, usr.Id, BlacklistType.User);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ChannelBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Channel);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, ulong id)
                => Blacklist(action, id, BlacklistType.Server);

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ServerBlacklist(AddRemove action, IGuild guild)
                => Blacklist(action, guild.Id, BlacklistType.Server);

            private async Task Blacklist(AddRemove action, ulong id, BlacklistType type)
            {
                if (action == AddRemove.Add && _creds.OwnerIds.Contains(id))
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    if (action == AddRemove.Add)
                    {
                        var item = new BlacklistItem { ItemId = id, Type = type };
                        uow.BotConfig.GetOrCreate().Blacklist.Add(item);
                        if (type == BlacklistType.Server)
                        {
                            BlacklistedGuilds.Add(id);
                        }
                        else if (type == BlacklistType.Channel)
                        {
                            BlacklistedChannels.Add(id);
                        }
                        else if (type == BlacklistType.User)
                        {
                            BlacklistedUsers.Add(id);
                        }
                    }
                    else
                    {
                        var objs = uow.BotConfig
                            .GetOrCreate(set => set.Include(x => x.Blacklist))
                            .Blacklist
                            .Where(bi => bi.ItemId == id && bi.Type == type);

                        if (objs.Any())
                            uow._context.Set<BlacklistItem>().RemoveRange(objs);

                        if (type == BlacklistType.Server)
                        {
                            BlacklistedGuilds.TryRemove(id);
                        }
                        else if (type == BlacklistType.Channel)
                        {
                            BlacklistedChannels.TryRemove(id);
                        }
                        else if (type == BlacklistType.User)
                        {
                            BlacklistedUsers.TryRemove(id);
                        }
                    }
                    await uow.CompleteAsync();
                }

                if (action == AddRemove.Add)
                    await ReplyConfirmLocalized("blacklisted", Format.Code(type.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("unblacklisted", Format.Code(type.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
        }
    }
}
