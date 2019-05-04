using Discord.Commands;

namespace Discord
{
    public class UserPermAttribute : RequireUserPermissionAttribute
    {
        public UserPermAttribute(GuildPerm permission) : base((GuildPermission)permission)
        {
        }

        public UserPermAttribute(ChannelPerm permission) : base((ChannelPermission)permission)
        {
        }
    }
}
