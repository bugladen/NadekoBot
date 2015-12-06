using Discord.Modules;

namespace NadekoBot.Modules
{
    class Gambling : DiscordModule
    {

        public Gambling() {
            commands.Add(new DrawCommand());
            commands.Add(new FlipCoinCommand());
            commands.Add(new DiceRollCommand());
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                commands.ForEach(com => com.Init(cgb));
            });
        }
    }
}
