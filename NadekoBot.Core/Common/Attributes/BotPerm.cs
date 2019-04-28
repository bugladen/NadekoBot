using Discord.Commands;

namespace Discord
{
    public class BotPermAttribute : RequireBotPermissionAttribute
    {
        public BotPermAttribute(GuildPermission permission) : base(permission)
        {
        }

        public BotPermAttribute(ChannelPermission permission) : base(permission)
        {
        }
    }
}
