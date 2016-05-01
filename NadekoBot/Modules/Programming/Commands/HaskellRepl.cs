using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// I have no idea what am i doing
/// </summary>
namespace NadekoBot.Modules.Programming.Commands
{
    class HaskellRepl : DiscordCommand
    {
        Queue<KeyValuePair<string, Channel>> commandQueue = new Queue<KeyValuePair<string, Channel>>();

        Thread haskellThread;

        public HaskellRepl(DiscordModule module) : base(module)
        {
            //start haskell interpreter

            haskellThread = new Thread(new ThreadStart(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "stack", //shouldn't use repl, but a Language.Haskell.Interpreter somehow
                    Arguments = "repl"
                });

            }));


            Task.Run(() =>
            {
                //read from queue

                //send the command to the process

                //wait 50 ms for execution

                //read everything from the output

                //send to chanenl
            });

        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "hs")
                .Description("Executes a haskell express with LAMBDABOT")
                .Do(async e =>
                {
                    //send a command and a channel to the queue
                });
        }
    }
}
