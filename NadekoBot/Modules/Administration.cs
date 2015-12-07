using Discord.Modules;
using System;
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

                cgb.CreateCommand(".r").Alias(".role")
                    .Description("Creates a role with a given name, and color.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("role_name",Discord.Commands.ParameterType.Required)
                    .Parameter("role_color",Discord.Commands.ParameterType.Optional)
                    .Do(async e =>
                    {
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
                                System.Console.WriteLine(ex.ToString());
                                await client.SendMessage(e.Channel, "Please supply a proper color.\n Example: DarkBlue, Orange, Teal");
                                return;
                            }
                        }
                        try
                        {
                            if (e.User.ServerPermissions.ManageRoles)
                            {
                                var r = await client.CreateRole(e.Server, e.GetArg("role_name"));
                                await client.EditRole(r, null,null, color);
                            }
                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "No sufficient permissions.");
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
                                    await client.SendMessage(e.Channel, "Banned user " + usr.Name + " Id: " + usr.Id);
                                }
                            }
                            catch (Exception)
                            {
                                await client.SendMessage(e.Channel, "No sufficient permissions.");
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
                                await client.SendMessage(e.Channel,"Kicked user " + usr.Name+" Id: "+usr.Id);
                            }
                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".vch")
                    .Description("Creates a new voice channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await client.CreateChannel(e.Server, e.GetArg("channel_name"), Discord.ChannelType.Voice);
                            }
                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".ch")
                    .Alias(".tch")
                    .Description("Creates a new text channel with a given name.\n*Both the user and the bot must have the sufficient permissions.*")
                    .Parameter("channel_name", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.User.ServerPermissions.ManageChannels)
                            {
                                await client.CreateChannel(e.Server, e.GetArg("channel_name"), Discord.ChannelType.Text);
                            }
                        }
                        catch (Exception) {
                            await client.SendMessage(e.Channel, "No sufficient permissions.");
                        }
                    });

                cgb.CreateCommand(".uid")
                    .Description("Shows user id")
                    .Parameter("user",Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.Message.MentionedUsers.Any())
                            await client.SendMessage(e.Channel, "Id of the user " + e.Message.MentionedUsers.First().Mention + " is " + e.Message.MentionedUsers.First().Id);
                        else
                            await client.SendMessage(e.Channel, "You must mention a user.");
                    });

                cgb.CreateCommand(".cid")
                    .Description("Shows current channel id")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, "This channel's id is " + e.Channel.Id);
                    });

                cgb.CreateCommand(".sid")
                    .Description("Shows current server id")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, "This server's id is " + e.Server.Id);
                    });
            });

        }
    }
}
