using Discord;
using Discord.Commands;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class GameChannelCommands : NadekoSubmodule<GameVoiceChannelService>
        {
            private readonly DbService _db;

            public GameChannelCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.MoveMembers)]
            public async Task GameVoiceChannel()
            {
                var vch = ((IGuildUser)Context.User).VoiceChannel;

                if (vch == null)
                {
                    await ReplyErrorLocalized("not_in_voice").ConfigureAwait(false);
                    return;
                }
                ulong? id;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    if (gc.GameVoiceChannel == vch.Id)
                    {
                        _service.GameVoiceChannels.TryRemove(vch.Id);
                        id = gc.GameVoiceChannel = null;
                    }
                    else
                    {
                        if(gc.GameVoiceChannel != null)
                            _service.GameVoiceChannels.TryRemove(gc.GameVoiceChannel.Value);
                        _service.GameVoiceChannels.Add(vch.Id);
                        id = gc.GameVoiceChannel = vch.Id;
                    }

                    uow.Complete();
                }

                if (id == null)
                {
                    await ReplyConfirmLocalized("gvc_disabled").ConfigureAwait(false);
                }
                else
                {
                    _service.GameVoiceChannels.Add(vch.Id);
                    await ReplyConfirmLocalized("gvc_enabled", Format.Bold(vch.Name)).ConfigureAwait(false);
                }
            }
        }
    }
}
