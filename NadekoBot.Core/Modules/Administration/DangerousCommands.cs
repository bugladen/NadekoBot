using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using NadekoBot.Services;
using System;
using System.Threading.Tasks;

#if DEBUG
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public class DangerousCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public DangerousCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ExecSql([Remainder]string sql)
            {
                try
                {
                    int res;
                    using (var uow = _db.UnitOfWork)
                    {
                        res = uow._context.Database.ExecuteSqlCommand(sql);
                    }

                    await Context.Channel.SendConfirmAsync(res.ToString());
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString());
                }
            }
        }
    }
}
#endif