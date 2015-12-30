using Discord.Modules;
using System;
using System.Diagnostics;
using System.Linq;
using Discord.Legacy;

namespace NadekoBot.Modules
{
    class Administration : DiscordModule
    {
        public Administration() : base() {
            commands.Add(new HelpCommand());
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(".sr").Alias(".setrole")
                    .Description("Sets a role for a given user.\nUsage: .sr @User Guest")
                    .Parameter("user_name", Discord.Commands.ParameterType.Required)
                    .Parameter("role_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles) return;
                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send( "You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Send( "You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await usr.AddRoles(new Discord.Role[] { role });
                            await e.Send( $"Successfully added role **{role.Name}** to user **{usr.Mention}**");
                        }
                        catch (Exception ex)
                        {
                            await e.Send( "Failed to add roles. Most likely reason: Insufficient permissions.\n");
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand(".rr").Alias(".removerole")
                    .Description("Removes a role from a given user.\nUsage: .rr @User Admin")
                    .Parameter("user_name", Discord.Commands.ParameterType.Required)
                    .Parameter("role_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles) return;

                        var usr = e.Server.FindUsers(e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Send( "You failed to supply a valid username");
                            return;
                        }

                        var role = e.Server.FindRoles(e.GetArg("role_name")).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Send( "You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await usr.RemoveRoles(new Discord.Role[] { role });
                            await e.Send( $"Successfully removed role **{role.Name}** from user **{usr.Mention}**");
                        }
                        catch (InvalidOperationException) {
                        }
                        catch (Exception)
                        {
                            await e.Send( "Failed to remove roles. Most likely reason: Insufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".r").Alias(".role").Alias(".cr")
                    .Description("Creates a role with a given name, and color.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("role_name",Discord.Commands.ParameterType.Required)
                    .Parameter("role_color",Discord.Commands.ParameterType.Optional)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles) return;

                        var color = Discord.Color.Blue;
                        if (e.GetArg("role_color") != null)
                        {
                            try
                            {
                                if (e.GetArg("role_color") != null && e.GetArg("role_color").Trim().Length > 0)
                                    color = (typeof(Discord.Color)).GetField(e.GetArg("role_color")).GetValue(null) as Discord.Color;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                await e.Send( "Please supply a proper color.\n Example: DarkBlue, Orange, Teal");
                                return;
                            }
                        }
                        try
                        {
                                var r = await e.Server.CreateRole(e.GetArg("role_name"));
                                await r.Edit(null,null, color);
                                await e.Send( $"Successfully created role **{r.ToString()}**.");
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                        return;
                    });

                cgb.CreateCommand(".b").Alias(".ban")
                    .Description("Kicks a mentioned user\n*Both the user and the bot must have the sufficient permissions.*")
                        .Do(async e =>
                        {
                            try
                            {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any())
                                {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Ban(usr);
                                    await e.Send( "Banned user " + usr.Name + " Id: " + usr.Id);
                                }
                            }
                            catch (Exception)
                            {
                                await e.Send( "No sufficient permissions.");
                            }
                        });

                cgb.CreateCommand(".k").Alias(".kick")
                    .Parameter("user")
                    .Description("Kicks a mentioned user.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.KickMembers && e.Message.MentionedUsers.Any())
                            {
                                var usr = e.Message.MentionedUsers.First();
                                await e.Message.MentionedUsers.First().Kick();
                                await e.Send("Kicked user " + usr.Name+" Id: "+usr.Id);
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".rvch")
                    .Description("Removes a voice channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.FindChannels(e.GetArg("channel_name"),Discord.ChannelType.Voice).FirstOrDefault()?.Delete();
                                await e.Send( $"Removed channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".vch").Alias(".cvch")
                    .Description("Creates a new voice channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), Discord.ChannelType.Voice);
                                await e.Send( $"Created voice channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".rch").Alias(".rtch")
                    .Description("Removes a text channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.FindChannels(e.GetArg("channel_name"), Discord.ChannelType.Text).FirstOrDefault()?.Delete();
                                await e.Send( $"Removed text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".ch").Alias(".tch")
                    .Description("Creates a new text channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), Discord.ChannelType.Text);
                                await e.Send( $"Added text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception) {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".uid").Alias(".userid")
                    .Description("Shows user id")
                    .Parameter("user",Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        var usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Send("You must mention a user.");
                            return;
                        }

                        await e.Send( "Id of the user " + usr.Name + " is " + usr.Id);
                    });

                cgb.CreateCommand(".cid").Alias(".channelid")
                    .Description("Shows current channel id")
                    .Do(async e =>
                    {
                        await e.Send( "This channel's id is " + e.Channel.Id);
                    });

                cgb.CreateCommand(".sid").Alias(".serverid")
                    .Description("Shows current server id")
                    .Do(async e =>
                    {
                        await e.Send( "This server's id is " + e.Server.Id);
                    });

                cgb.CreateCommand(".stats")
                    .Description("Shows some basic stats for nadeko")
                    .Do(async e =>
                    {
                        int serverCount = client.Servers.Count();
                        int uniqueUserCount = client.Servers.Sum(s=>s.Users.Count());
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        string uptime = " " + time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";

                        await e.Send( String.Format("```Servers: {0}\nUnique Users: {1}\nUptime: {2}\nMy id is: {3}```", serverCount, uniqueUserCount, uptime, client.CurrentUser.Id));
                    });
            });

        }
    }
}
