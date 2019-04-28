using Discord.Commands;

namespace Discord
{
    public class UserPermAttribute : RequireUserPermissionAttribute
    {
        public UserPermAttribute(GuildPermission permission) : base(permission)
        {
        }

        public UserPermAttribute(ChannelPermission permission) : base(permission)
        {
        }
    }
}
