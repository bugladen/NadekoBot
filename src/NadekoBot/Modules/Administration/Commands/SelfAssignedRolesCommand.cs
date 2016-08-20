//using Discord.Commands;
//using Discord.Net;
//using NadekoBot.Classes;
//using NadekoBot.Modules.Permissions.Classes;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
////todo DB
//namespace NadekoBot.Modules.Administration
//{
//    internal class SelfAssignedRolesCommand : DiscordCommand
//    {
//        public SelfAssignedRolesCommand(DiscordModule module) : base(module) { }
//        internal override void Init(CommandGroupBuilder cgb)
//        {
//            cgb.CreateCommand(Module.Prefix + "asar")
//                .Description("Adds a role, or list of roles separated by whitespace" +
//                             $"(use quotations for multiword roles) to the list of self-assignable roles. **Needs Manage Roles Permissions.**| `{Prefix}asar Gamer`")
//                .Parameter("roles", ParameterType.Multiple)
//                .AddCheck(SimpleCheckers.CanManageRoles)
//                .Do(async e =>
//                {
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    var msg = new StringBuilder();
//                    foreach (var arg in e.Args)
//                    {
//                        var role = e.Server.FindRoles(arg.Trim()).FirstOrDefault();
//                        if (role == null)
//                            msg.AppendLine($":anger:Role **{arg}** not found.");
//                        else
//                        {
//                            if (config.ListOfSelfAssignableRoles.Contains(role.Id))
//                            {
//                                msg.AppendLine($":anger:Role **{role.Name}** is already in the list.");
//                                continue;
//                            }
//                            config.ListOfSelfAssignableRoles.Add(role.Id);
//                            msg.AppendLine($":ok:Role **{role.Name}** added to the list.");
//                        }
//                    }
//                    await channel.SendMessageAsync(msg.ToString()).ConfigureAwait(false);
//                });

//            cgb.CreateCommand(Module.Prefix + "rsar")
//                .Description($"Removes a specified role from the list of self-assignable roles. | `{Prefix}rsar`")
//                .Parameter("role", ParameterType.Unparsed)
//                .AddCheck(SimpleCheckers.CanManageRoles)
//                .Do(async e =>
//                {
//                    var roleName = e.GetArg("role")?.Trim();
//                    if (string.IsNullOrWhiteSpace(roleName))
//                        return;
//                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
//                    if (role == null)
//                    {
//                        await channel.SendMessageAsync(":anger:That role does not exist.").ConfigureAwait(false);
//                        return;
//                    }
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
//                    {
//                        await channel.SendMessageAsync(":anger:That role is not self-assignable.").ConfigureAwait(false);
//                        return;
//                    }
//                    config.ListOfSelfAssignableRoles.Remove(role.Id);
//                    await channel.SendMessageAsync($":ok:**{role.Name}** has been removed from the list of self-assignable roles").ConfigureAwait(false);
//                });

//            cgb.CreateCommand(Module.Prefix + "lsar")
//                .Description($"Lists all self-assignable roles. | `{Prefix}lsar`")
//                .Parameter("roles", ParameterType.Multiple)
//                .Do(async e =>
//                {
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    var msg = new StringBuilder($"There are `{config.ListOfSelfAssignableRoles.Count}` self assignable roles:\n");
//                    var toRemove = new HashSet<ulong>();
//                    foreach (var roleId in config.ListOfSelfAssignableRoles.OrderBy(r => r.ToString()))
//                    {
//                        var role = e.Server.GetRole(roleId);
//                        if (role == null)
//                        {
//                            msg.Append($"`{roleId} not found. Cleaned up.`, ");
//                            toRemove.Add(roleId);
//                        }
//                        else
//                        {
//                            msg.Append($"**{role.Name}**, ");
//                        }
//                    }
//                    foreach (var id in toRemove)
//                    {
//                        config.ListOfSelfAssignableRoles.Remove(id);
//                    }
//                    await channel.SendMessageAsync(msg.ToString()).ConfigureAwait(false);
//                });



//            cgb.CreateCommand(Module.Prefix + "togglexclsar").Alias(Module.Prefix + "tesar")
//                .Description($"toggle whether the self-assigned roles should be exclusive | `{Prefix}tesar`")
//                .AddCheck(SimpleCheckers.CanManageRoles)
//                .Do(async e =>
//                {
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    config.ExclusiveSelfAssignedRoles = !config.ExclusiveSelfAssignedRoles;
//                    string exl = config.ExclusiveSelfAssignedRoles ? "exclusive" : "not exclusive";
//                    await channel.SendMessageAsync("Self assigned roles are now " + exl);
//                });

//            cgb.CreateCommand(Module.Prefix + "iam")
//                .Description("Adds a role to you that you choose. " +
//                             "Role must be on a list of self-assignable roles." +
//                             $" | `{Prefix}iam Gamer`")
//                .Parameter("role", ParameterType.Unparsed)
//                .Do(async e =>
//                {
//                    var roleName = e.GetArg("role")?.Trim();
//                    if (string.IsNullOrWhiteSpace(roleName))
//                        return;
//                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
//                    if (role == null)
//                    {
//                        await channel.SendMessageAsync(":anger:That role does not exist.").ConfigureAwait(false);
//                        return;
//                    }
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
//                    {
//                        await channel.SendMessageAsync(":anger:That role is not self-assignable.").ConfigureAwait(false);
//                        return;
//                    }
//                    if (imsg.Author.HasRole(role))
//                    {
//                        await channel.SendMessageAsync($":anger:You already have {role.Name} role.").ConfigureAwait(false);
//                        return;
//                    }
//                    var sameRoles = imsg.Author.Roles.Where(r => config.ListOfSelfAssignableRoles.Contains(r.Id));
//                    if (config.ExclusiveSelfAssignedRoles && sameRoles.Any())
//                    {
//                        await channel.SendMessageAsync($":anger:You already have {sameRoles.FirstOrDefault().Name} role.").ConfigureAwait(false);
//                        return;
//                    }
//                    try
//                    {
//                        await imsg.Author.AddRoles(role).ConfigureAwait(false);
//                    }
//                    catch (HttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
//                    {
//                    }
//                    catch (Exception ex)
//                    {
//                        await channel.SendMessageAsync($":anger:`I am unable to add that role to you. I can't add roles to owners or other roles higher than my role in the role hierarchy.`").ConfigureAwait(false);
//                        return;
//                    }
//                    var msg = await channel.SendMessageAsync($":ok:You now have {role.Name} role.").ConfigureAwait(false);
//                    await Task.Delay(3000).ConfigureAwait(false);
//                    await msg.Delete().ConfigureAwait(false);
//                    try
//                    {
//                        await e.Message.Delete().ConfigureAwait(false);
//                    }
//                    catch { }
//                });

//            cgb.CreateCommand(Module.Prefix + "iamnot")
//                .Alias(Module.Prefix + "iamn")
//                .Description("Removes a role to you that you choose. " +
//                             "Role must be on a list of self-assignable roles." +
//                             $" | `{Prefix}iamn Gamer`")
//                .Parameter("role", ParameterType.Unparsed)
//                .Do(async e =>
//                {
//                    var roleName = e.GetArg("role")?.Trim();
//                    if (string.IsNullOrWhiteSpace(roleName))
//                        return;
//                    var role = e.Server.FindRoles(roleName).FirstOrDefault();
//                    if (role == null)
//                    {
//                        await channel.SendMessageAsync(":anger:That role does not exist.").ConfigureAwait(false);
//                        return;
//                    }
//                    var config = SpecificConfigurations.Default.Of(e.Server.Id);
//                    if (!config.ListOfSelfAssignableRoles.Contains(role.Id))
//                    {
//                        await channel.SendMessageAsync(":anger:That role is not self-assignable.").ConfigureAwait(false);
//                        return;
//                    }
//                    if (!imsg.Author.HasRole(role))
//                    {
//                        await channel.SendMessageAsync($":anger:You don't have {role.Name} role.").ConfigureAwait(false);
//                        return;
//                    }
//                    await imsg.Author.RemoveRoles(role).ConfigureAwait(false);
//                    var msg = await channel.SendMessageAsync($":ok:Successfuly removed {role.Name} role from you.").ConfigureAwait(false);
//                    await Task.Delay(3000).ConfigureAwait(false);
//                    await msg.Delete().ConfigureAwait(false);
//                    try
//                    {
//                        await e.Message.Delete().ConfigureAwait(false);
//                    }
//                    catch { }
//                });
//        }
//    }
//}
