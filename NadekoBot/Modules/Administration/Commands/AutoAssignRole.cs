using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Linq;

namespace NadekoBot.Modules.Administration.Commands
{
    class AutoAssignRole : DiscordCommand
    {
        public AutoAssignRole(DiscordModule module) : base(module)
        {
            NadekoBot.Client.UserJoined += (s, e) =>
            {
                try
                {
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);

                    var role = e.Server.Roles.Where(r => r.Id == config.AutoAssignedRole).FirstOrDefault();

                    if (role == null)
                        return;

                    e.User.AddRoles(role);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"aar exception. {ex}");
                }
            };
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "autoassignrole")
                .Alias(Module.Prefix + "aar")
                .Description($"Automaticaly assigns a specified role to every user who joins the server. Type `{Prefix}aar` to disable, `{Prefix}aar Role Name` to enable")
                .Parameter("role", ParameterType.Unparsed)
                .AddCheck(new SimpleCheckers.ManageRoles())
                .Do(async e =>
                {
                    if (!e.Server.CurrentUser.ServerPermissions.ManageRoles)
                    {
                        await e.Channel.SendMessage("I do not have the permission to manage roles.").ConfigureAwait(false);
                        return;
                    }
                    var r = e.GetArg("role")?.Trim();

                    var config = SpecificConfigurations.Default.Of(e.Server.Id);

                    if (string.IsNullOrWhiteSpace(r)) //if role is not specified, disable
                    {
                        config.AutoAssignedRole = 0;

                        await e.Channel.SendMessage("`Auto assign role on user join is now disabled.`").ConfigureAwait(false);
                        return;
                    }
                    var role = e.Server.FindRoles(r).FirstOrDefault();

                    if (role == null)
                    {
                        await e.Channel.SendMessage("💢 `Role not found.`").ConfigureAwait(false);
                        return;
                    }

                    config.AutoAssignedRole = role.Id;
                    await e.Channel.SendMessage("`Auto assigned role is set.`").ConfigureAwait(false);

                });
        }
    }
}
