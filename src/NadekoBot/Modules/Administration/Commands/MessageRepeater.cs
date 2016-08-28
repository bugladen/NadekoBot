using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//todo DB
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {

        [Group]
        public class RepeatCommands
        {
            public ConcurrentDictionary<ulong, Repeater> repeaters;

            public RepeatCommands()
            {
                using (var uow = DbHandler.UnitOfWork())
                {
                    repeaters = new ConcurrentDictionary<ulong, Repeater>(uow.Repeaters.GetAll().ToDictionary(r => r.ChannelId));
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageMessages)]
            public async Task RepeatInvoke(IMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;

                Repeater rep;
                if (!repeaters.TryGetValue(channel.Id, out rep))
                {
                    await channel.SendMessageAsync("`No repeating message found on this server.`").ConfigureAwait(false);
                    return;
                }

                await channel.SendMessageAsync("🔄 " + rep.Message);
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Repeat(IMessage imsg, int minutes, [Remainder] string message = null)
            {
                var channel = (ITextChannel)imsg.Channel;

                if (minutes < 1 || minutes > 1500)
                    return;

                Repeater rep;

                if (string.IsNullOrWhiteSpace(message)) //turn off
                {
                    if (repeaters.TryRemove(channel.Id, out rep))
                    {
                        using (var uow = DbHandler.UnitOfWork())
                        {
                            uow.Repeaters.Remove(rep);
                            await uow.CompleteAsync();
                        }
                        await channel.SendMessageAsync("`Stopped repeating a message.`").ConfigureAwait(false);
                    }
                    else
                        await channel.SendMessageAsync("`No message is repeating.`").ConfigureAwait(false);
                    return;
                }

                rep = repeaters.AddOrUpdate(channel.Id, (cid) =>
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var localRep = new Repeater
                        {
                            ChannelId = channel.Id,
                            GuildId = channel.Guild.Id,
                            Interval = TimeSpan.FromMinutes(minutes),
                            Message = message,
                        };
                        uow.Repeaters.Add(localRep);
                        uow.Complete();
                        return localRep;
                    }
                }, (cid, old) =>
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        old.Message = message;
                        old.Interval = TimeSpan.FromMinutes(minutes);
                        uow.Repeaters.Update(old);
                        uow.Complete();
                        return old;
                    }
                });
            }

        }
    }
}