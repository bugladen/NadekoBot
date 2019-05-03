using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class GlobalPermissionCommands : NadekoSubmodule
        {
            private GlobalPermissionService _service;
            private readonly DbService _db;

            public GlobalPermissionCommands(GlobalPermissionService service, DbService db)
            {
                _service = service;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Lgp()
            {
                if (!_service.BlockedModules.Any() && !_service.BlockedCommands.Any())
                {
                    await ReplyErrorLocalizedAsync("lgp_none").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithOkColor();

                if (_service.BlockedModules.Any())
                    embed.AddField(efb => efb.WithName(GetText("blocked_modules")).WithValue(string.Join("\n", _service.BlockedModules)).WithIsInline(false));

                if (_service.BlockedCommands.Any())
                    embed.AddField(efb => efb.WithName(GetText("blocked_commands")).WithValue(string.Join("\n", _service.BlockedCommands)).WithIsInline(false));

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Gmod(ModuleOrCrInfo module)
            {
                var moduleName = module.Name.ToLowerInvariant();
                if (_service.BlockedModules.Add(moduleName))
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.BlockedModules));
                        bc.BlockedModules.Add(new BlockedCmdOrMdl
                        {
                            Name = moduleName,
                        });
                        uow.SaveChanges();
                    }
                    await ReplyConfirmLocalizedAsync("gmod_add", Format.Bold(module.Name)).ConfigureAwait(false);
                    return;
                }
                else if (_service.BlockedModules.TryRemove(moduleName))
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.BlockedModules));
                        var mdls = bc.BlockedModules.Where(x => x.Name == moduleName);
                        if (mdls.Any())
                            uow._context.RemoveRange(mdls.ToArray());
                        uow.SaveChanges();
                    }
                    await ReplyConfirmLocalizedAsync("gmod_remove", Format.Bold(module.Name)).ConfigureAwait(false);
                    return;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Gcmd(CommandOrCrInfo cmd)
            {
                var commandName = cmd.Name.ToLowerInvariant();
                if (_service.BlockedCommands.Add(commandName))
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.BlockedCommands));
                        bc.BlockedCommands.Add(new BlockedCmdOrMdl
                        {
                            Name = commandName,
                        });
                        uow.SaveChanges();
                    }
                    await ReplyConfirmLocalizedAsync("gcmd_add", Format.Bold(cmd.Name)).ConfigureAwait(false);
                    return;
                }
                else if (_service.BlockedCommands.TryRemove(commandName))
                {
                    using (var uow = _db.GetDbContext())
                    {
                        var bc = uow.BotConfig.GetOrCreate(set => set.Include(x => x.BlockedCommands));
                        var objs = bc.BlockedCommands.Where(x => x.Name == commandName);
                        if (objs.Any())
                            uow._context.RemoveRange(objs.ToArray());
                        uow.SaveChanges();
                    }
                    await ReplyConfirmLocalizedAsync("gcmd_remove", Format.Bold(cmd.Name)).ConfigureAwait(false);
                    return;
                }
            }
        }
    }
}
