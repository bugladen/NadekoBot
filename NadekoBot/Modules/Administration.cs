using Discord.Modules;
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
