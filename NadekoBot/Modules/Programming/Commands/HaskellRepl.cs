using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// I have no idea what am i doing
/// </summary>
namespace NadekoBot.Modules.Programming.Commands
{
    class HaskellRepl : DiscordCommand
    {
        ConcurrentQueue<KeyValuePair<string, Channel>> commandQueue = new ConcurrentQueue<KeyValuePair<string, Channel>>();

        Thread haskellThread;

        public HaskellRepl(DiscordModule module) : base(module)
        {
            //start haskell interpreter

            haskellThread = new Thread(new ThreadStart(() =>
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "stack", //shouldn't use repl, but a Language.Haskell.Interpreter somehow
                    Arguments = "repl",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                });

                Task.Run(async () =>
                {
                    while (true)
                    {
                        while (commandQueue.Count == 0)
                            await Task.Delay(100);

                        //read from queue
                        KeyValuePair<string, Channel> com;
                        if (!commandQueue.TryDequeue(out com))
                        {
                            await Task.Delay(100);
                            continue;
                        }
                        //var bytes = Encoding.ASCII.GetBytes(com.Key);

                        //send the command to the process
                        p.StandardInput.WriteLine(com.Key);

                        //wait 50 ms for execution
                        await Task.Delay(50);

                        //read everything from the output
                        var outBuffer = new byte[1500];

                        p.StandardOutput.BaseStream.Read(outBuffer, 0, 1500);

                        var outStr = Encoding.ASCII.GetString(outBuffer);
                        //send to channel
                        await com.Value.SendMessage($"```hs\nPrelude> {com.Key}\n" + outStr + "\n```");
                    }
                });

            }));
            haskellThread.Start();

        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "hs")
                .Description("Executes a haskell express with LAMBDABOT")
                .Parameter("command", ParameterType.Unparsed)
                .Do(e =>
                {
                    var com = e.GetArg("command")?.Trim();
                    if (string.IsNullOrWhiteSpace(com))
                        return;

                    //send a command and a channel to the queue
                    commandQueue.Enqueue(new KeyValuePair<string, Channel>(com, e.Channel));
                });
        }
    }
}
