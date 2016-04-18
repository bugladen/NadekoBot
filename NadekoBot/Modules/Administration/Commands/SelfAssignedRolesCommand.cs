using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NadekoBot.Modules.Administration.Commands
{
    internal class SelfAssignedRolesCommand : DiscordCommand
    {
        public SelfAssignedRolesCommand(DiscordModule module) : base(module) { }
        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(".asar")
                .Description("Adds a role, or list of roles separated by whitespace" +
                             "(use quotations for multiword roles) to the list of self-assignable roles.\n**Usage**: .asar Gamer")
                .Parameter("roles", ParameterType.Multiple)
                .AddCheck(SimpleCheckers.CanManageRoles)
                .Do(async e =>
                {
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    var msg = new StringBuilder();
                    foreach (var arg in e.Args)
                    {
                        var role = e.Server.FindRoles(arg.Trim()).FirstOrDefault();
                        if (role == null)
                            msg.AppendLine($":anger:Role **{arg}** not found.");
                        else
                        {
                            if (config.ListOfSelfAssignableRoles.Contains(role.Id))
                            {
                                msg.AppendLine($":anger:Role **{role.Name}** is already in the list.");
                                continue;
                            }
                            config.ListOfSelfAssignableRoles.Add(role.Id);
                            msg.AppendLine($":ok:Role **{role.Name}** added to the list.");
                        }
                    }
                    await e.Channel.SendMessage(msg.ToString()).ConfigureAwait(false);
                });

            cgb.CreateCommand(".rsar")
                .Description("Removes a specified role from the list of self-assignable roles.")
                .Parameter("role", ParameterType.Unparsed)
                .AddCheck(SimpleCheckers.CanManageRoles)
                .Do(async e =>
                {
                    var roleName = e.GetArg("role")?.Trim();
                    if (string.IsNullOrWhiteSpace(roleName))
                        return;
                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
                    if (role == null)
                    {
                        await e.Channel.SendMessage(":anger:That role does not exist.").ConfigureAwait(false);
                        return;
                    }
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
                    {
                        await e.Channel.SendMessage(":anger:That role is not self-assignable.").ConfigureAwait(false);
                        return;
                    }
                    config.ListOfSelfAssignableRoles.Remove(role.Id);
                    await e.Channel.SendMessage($":ok:**{role.Name}** has been removed from the list of self-assignable roles").ConfigureAwait(false);
                });

            cgb.CreateCommand(".lsar")
                .Description("Lits all self-assignable roles.")
                .Parameter("roles", ParameterType.Multiple)
                .Do(async e =>
                {
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    var msg = new StringBuilder($"There are `{config.ListOfSelfAssignableRoles.Count}` self assignable roles:\n");
                    var toRemove = new HashSet<ulong>();
                    foreach (var roleId in config.ListOfSelfAssignableRoles)
                    {
                        var role = e.Server.GetRole(roleId);
                        if (role == null)
                        {
                            msg.Append($"`{roleId} not found. Cleaned up.`, ");
                            toRemove.Add(roleId);
                        }
                        else
                        {
                            msg.Append($"**{role.Name}**, ");
                        }
                    }
                    foreach (var id in toRemove)
                    {
                        config.ListOfSelfAssignableRoles.Remove(id);
                    }
                    await e.Channel.SendMessage(msg.ToString()).ConfigureAwait(false);
                });

            cgb.CreateCommand(".iam")
                .Description("Adds a role to you that you choose. " +
                             "Role must be on a list of self-assignable roles." +
                             "\n**Usage**: .iam Gamer")
                .Parameter("role", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var roleName = e.GetArg("role")?.Trim();
                    if (string.IsNullOrWhiteSpace(roleName))
                        return;
                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
                    if (role == null)
                    {
                        await e.Channel.SendMessage(":anger:That role does not exist.").ConfigureAwait(false);
                        return;
                    }
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
                    {
                        await e.Channel.SendMessage(":anger:That role is not self-assignable.").ConfigureAwait(false);
                        return;
                    }
                    if (e.User.HasRole(role))
                    {
                        await e.Channel.SendMessage($":anger:You already have {role.Name} role.").ConfigureAwait(false);
                        return;
                    }
                    await e.User.AddRoles(role).ConfigureAwait(false);
                    await e.Channel.SendMessage($":ok:You now have {role.Name} role.").ConfigureAwait(false);
                });

            cgb.CreateCommand(".iamn")
                .Alias(".iamnot")
                .Description("Removes a role to you that you choose. " +
                             "Role must be on a list of self-assignable roles." +
                             "\n**Usage**: .iamn Gamer")
                .Parameter("role", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var roleName = e.GetArg("role")?.Trim();
                    if (string.IsNullOrWhiteSpace(roleName))
                        return;
                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
                    if (role == null)
                    {
                        await e.Channel.SendMessage(":anger:That role does not exist.").ConfigureAwait(false);
                        return;
                    }
                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
                    {
                        await e.Channel.SendMessage(":anger:That role is not self-assignable.").ConfigureAwait(false);
                        return;
                    }
                    if (!e.User.HasRole(role))
                    {
                        await e.Channel.SendMessage($":anger:You don't have {role.Name} role.").ConfigureAwait(false);
                        return;
                    }
                    await e.User.RemoveRoles(role).ConfigureAwait(false);
                    await e.Channel.SendMessage($":ok:Successfuly removed {role.Name} role from you.").ConfigureAwait(false);
                });
        }
    }
}
