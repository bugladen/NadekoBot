using Discord.Modules;
using Discord.Commands;
using Discord;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Discord.Commands.Permissions.Visibility;

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
                    .Description("Sets a role for a given user.\n**Usage**: .sr @User Guest")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Required)
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
                            await usr.AddRoles(new Role[] { role });
                            await e.Send( $"Successfully added role **{role.Name}** to user **{usr.Mention}**");
                        }
                        catch (Exception ex)
                        {
                            await e.Send( "Failed to add roles. Most likely reason: Insufficient permissions.\n");
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand(".rr").Alias(".removerole")
                    .Description("Removes a role from a given user.\n**Usage**: .rr @User Admin")
                    .Parameter("user_name", ParameterType.Required)
                    .Parameter("role_name", ParameterType.Required)
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
                            await usr.RemoveRoles(new Role[] { role });
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
                    .Description("Creates a role with a given name, and color.\n**Usage**: .r AwesomeRole Orange")
                    .Parameter("role_name", ParameterType.Required)
                    .Parameter("role_color", ParameterType.Optional)
                    .Do(async e =>
                    {
                        if (!e.User.ServerPermissions.ManageRoles) return;

                        var color = Color.Blue;
                        if (e.GetArg("role_color") != null)
                        {
                            try
                            {
                                if (e.GetArg("role_color") != null && e.GetArg("role_color").Trim().Length > 0)
                                    color = (typeof(Color)).GetField(e.GetArg("role_color")).GetValue(null) as Color;
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
                        catch (Exception) { }
                    });

                cgb.CreateCommand(".b").Alias(".ban")
                    .Description("Bans a mentioned user")
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
                            } catch (Exception) { }
                        });

                cgb.CreateCommand(".ub").Alias(".unban")
                    .Description("Unbans a mentioned user")
                        .Do(async e => {
                            try {
                                if (e.User.ServerPermissions.BanMembers && e.Message.MentionedUsers.Any()) {
                                    var usr = e.Message.MentionedUsers.First();
                                    await usr.Server.Unban(usr);
                                    await e.Send("Unbanned user " + usr.Name + " Id: " + usr.Id);
                                }
                            } catch (Exception) { }
                        });

                cgb.CreateCommand(".k").Alias(".kick")
                    .Parameter("user")
                    .Description("Kicks a mentioned user.")
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
                    .Description("Removes a voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Voice).FirstOrDefault()?.Delete();
                                await e.Send( $"Removed channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".vch").Alias(".cvch")
                    .Description("Creates a new voice channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Voice);
                                await e.Send( $"Created voice channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".rch").Alias(".rtch")
                    .Description("Removes a text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.FindChannels(e.GetArg("channel_name"), ChannelType.Text).FirstOrDefault()?.Delete();
                                await e.Send( $"Removed text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception)
                        {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".ch").Alias(".tch")
                    .Description("Creates a new text channel with a given name.")
                    .Parameter("channel_name", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await e.Server.CreateChannel(e.GetArg("channel_name"), ChannelType.Text);
                                await e.Send( $"Added text channel **{e.GetArg("channel_name")}**.");
                            }
                        }
                        catch (Exception) {
                            await e.Send( "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".st").Alias(".settopic")
                    .Description("Sets a topic on the current channel.")
                    .Parameter("topic", ParameterType.Unparsed)
                    .Do(async e => {
                        try {
                            await e.Channel.Edit(topic: e.GetArg("topic"));
                        } catch (Exception) { }
                    });

                cgb.CreateCommand(".uid").Alias(".userid")
                    .Description("Shows user id")
                    .Parameter("user", ParameterType.Required)
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

                        await e.Send($"```Servers: {serverCount}\nUnique Users: {uniqueUserCount}\nUptime: {uptime}\nMy id is: {client.CurrentUser.Id}```");
                    });

                cgb.CreateCommand(".leaveall")
                    .Description("Nadeko leaves all servers")
                    .Do(e => {
                        NadekoBot.client.Servers.ForEach(async s => { if (s.Name == "NadekoLog" || s.Name == "Discord Bots") return; await s.Leave(); });
                    });
                cgb.CreateCommand(".prune")
                    .Parameter("num", ParameterType.Required)
                    .Description("Prunes a number of messages from the current channel.\n**Usage**: .prune 50")
                    .Do(async e => {
                        int num;

                        if (!Int32.TryParse(e.GetArg("num"), out num) || num < 1) {
                            await e.Send("Incorrect amount.");
                            return;
                        }
                        try {
                            (await e.Channel.DownloadMessages(num)).ForEach(async m => await m.Delete());
                        } catch (Exception) { await e.Send("Failed pruning. Make sure the bot has correct permissions."); }

                    });

                cgb.CreateCommand(".die")
                    .Description("Works only for the owner. Shuts the bot down.")
                    .Do(async e => {
                        if (e.User.Id == NadekoBot.OwnerID) {
                            Timer t = new Timer();
                            t.Interval = 2000;
                            t.Elapsed += (s, ev) => { Environment.Exit(0); };
                            t.Start();
                            await e.Send("Shutting down.");
                        }
                    });

                cgb.CreateCommand(".clr")
                    .Description("Clears some of nadeko's messages from the current channel.")
                    .Do(async e => {
                        try {
                            if (e.Channel.Messages.Count() < 50) {
                                await e.Channel.DownloadMessages(100);
                            }

                            e.Channel.Messages.Where(msg => msg.User.Id == client.CurrentUser.Id).ForEach(async m => await m.Delete());

                        } catch (Exception) {
                            await e.Send("I cant do it :(");
                        }
                    });
                cgb.CreateCommand(".newname")
                    .Description("Give the bot a new name.")
                    .Parameter("new_name", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID || e.GetArg("new_name") == null) return;

                        await client.CurrentUser.Edit(NadekoBot.password, e.GetArg("new_name"));
                    });

                cgb.CreateCommand(".greet")
                    .Description("Enables or Disables anouncements on the current channel when someone joins the server.")
                    .Do(async e => {
                        if (e.User.Id != NadekoBot.OwnerID) return;
                        announcing = !announcing;
                        
                        if (announcing) {
                            announceChannel = e.Channel;
                            joinServer = e.Server;
                            NadekoBot.client.UserJoined += Client_UserJoined;
                            await e.Send("Announcements enabled on this channel.");
                        } else {
                            announceChannel = null;
                            joinServer = null;
                            NadekoBot.client.UserJoined -= Client_UserJoined;
                            await e.Send("Announcements disabled.");
                        }
                    });

                cgb.CreateCommand(".greetmsg")
                    .Description("Sets a new announce message. Type %user% if you want to mention the new member.\n**Usage**: .greetmsg Welcome to the server, %user%.")
                    .Parameter("msg",ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.GetArg("msg") == null) return;
                        announceMsg = e.GetArg("msg");
                        await e.Send("New message set.");
                    });
            });

        }

        bool announcing = false;
        Channel announceChannel = null;
        Server joinServer = null;
        string announceMsg = "Welcome to the server %user%";

        private void Client_UserJoined(object sender, UserEventArgs e) {
            if (e.Server != joinServer) return;
            try {
                announceChannel?.Send(announceMsg.Replace("%user%", e.User.Mention));
            } catch (Exception) {
                Console.WriteLine("Failed sending greet message to the specified channel");
            }

        }
    }
}
