using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.DataStructures;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.TypeReaders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class GlobalPermissionCommands : NadekoSubmodule
        {
            public static readonly ConcurrentHashSet<string> BlockedModules;
            public static readonly ConcurrentHashSet<string> BlockedCommands;

            static GlobalPermissionCommands()
            {
                BlockedModules = new ConcurrentHashSet<string>(NadekoBot.BotConfig.BlockedModules.Select(x => x.Name));
                BlockedCommands = new ConcurrentHashSet<string>(NadekoBot.BotConfig.BlockedCommands.Select(x => x.Name));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Lgp()
            {
                if (!BlockedModules.Any() && !BlockedCommands.Any())
                {
                    await ReplyErrorLocalized("lgp_none").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithOkColor();

                if (BlockedModules.Any())
                    embed.AddField(efb => efb.WithName(GetText("blocked_modules")).WithValue(string.Join("\n", BlockedModules)).WithIsInline(false));

                if (BlockedCommands.Any())
                    embed.AddField(efb => efb.WithName(GetText("blocked_commands")).WithValue(string.Join("\n", BlockedCommands)).WithIsInline(false));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Gmod(ModuleOrCrInfo module)
            {
                var moduleName = module.Name.ToLowerInvariant();
                if (BlockedModules.Add(moduleName))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var bc = uow.BotConfig.GetOrCreate();
                        bc.BlockedModules.Add(new Services.Database.Models.BlockedCmdOrMdl
                        {
                            Name = moduleName,
                        });
                        uow.Complete();
                    }
                    await ReplyConfirmLocalized("gmod_add", Format.Bold(module.Name)).ConfigureAwait(false);
                    return;
                }
                else if (BlockedModules.TryRemove(moduleName))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var bc = uow.BotConfig.GetOrCreate();
                        bc.BlockedModules.RemoveWhere(x => x.Name == moduleName);
                        uow.Complete();
                    }
                    await ReplyConfirmLocalized("gmod_remove", Format.Bold(module.Name)).ConfigureAwait(false);
                    return;
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task Gcmd(CommandOrCrInfo cmd)
            {
                var commandName = cmd.Name.ToLowerInvariant();
                if (BlockedCommands.Add(commandName))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var bc = uow.BotConfig.GetOrCreate();
                        bc.BlockedCommands.Add(new Services.Database.Models.BlockedCmdOrMdl
                        {
                            Name = commandName,
                        });
                        uow.Complete();
                    }
                    await ReplyConfirmLocalized("gcmd_add", Format.Bold(cmd.Name)).ConfigureAwait(false);
                    return;
                }
                else if (BlockedCommands.TryRemove(commandName))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var bc = uow.BotConfig.GetOrCreate();
                        bc.BlockedCommands.RemoveWhere(x => x.Name == commandName);
                        uow.Complete();
                    }
                    await ReplyConfirmLocalized("gcmd_remove", Format.Bold(cmd.Name)).ConfigureAwait(false);
                    return;
                }
            }
        }
    }
}
