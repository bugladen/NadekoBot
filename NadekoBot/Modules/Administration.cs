using Discord.Modules;
using System;
using System.Diagnostics;
using System.Linq;

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
                        var usr = client.FindUsers(e.Server, e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null) {
                            await e.Send( "You failed to supply a valid username");
                            return;
                        }

                        var role = client.FindRoles(e.Server, e.GetArg("role_name")).FirstOrDefault();
                        if (role == null) {
                            await e.Send( "You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await client.EditUser(usr, null, null,null, new Discord.Role[] { role }, Discord.EditMode.Add);
                            await e.Send( $"Successfully added role **{role.Name}** to user **{usr.Mention}**");
                        }
                        catch (InvalidOperationException) { //fkin voltana and his shenanigans, fix role.Mention pl0x
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
                        var usr = client.FindUsers(e.Server, e.GetArg("user_name")).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Send( "You failed to supply a valid username");
                            return;
                        }

                        var role = client.FindRoles(e.Server, e.GetArg("role_name")).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Send( "You failed to supply a valid role");
                            return;
                        }

                        try
                        {
                            await client.EditUser(usr, null, null,null, new Discord.Role[]{ role }, Discord.EditMode.Remove);
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
                                var r = await client.CreateRole(e.Server, e.GetArg("role_name"));
                                await client.EditRole(r, null,null, color);
                                await e.Send( $"Successfully created role **{r.Mention}**.");
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
                                    await client.BanUser(e.Message.MentionedUsers.First());
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
                                await client.KickUser(e.Message.MentionedUsers.First());
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
                                await client.DeleteChannel(client.FindChannels(e.Server,e.GetArg("channel_name"),Discord.ChannelType.Voice).FirstOrDefault());
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
                                await client.CreateChannel(e.Server, e.GetArg("channel_name"), Discord.ChannelType.Voice);
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
                                await client.DeleteChannel(client.FindChannels(e.Server, e.GetArg("channel_name"), Discord.ChannelType.Text).FirstOrDefault());
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
                                await client.CreateChannel(e.Server, e.GetArg("channel_name"), Discord.ChannelType.Text);
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
                        if (e.Message.MentionedUsers.Any())
                            await e.Send( "Id of the user " + e.Message.MentionedUsers.First().Mention + " is " + e.Message.MentionedUsers.First().Id);
                        else
                            await e.Send( "You must mention a user.");
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
                        int serverCount = client.AllServers.Count();
                        int uniqueUserCount = client.AllUsers.Count();
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        string uptime = " " + time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";

                        await e.Send( String.Format("```Servers: {0}\nUnique Users: {1}\nUptime: {2}\nMy id is: {3}```", serverCount, uniqueUserCount, uptime, client.CurrentUser.Id));
                    });
            });

        }
    }
}
