using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NLog;
using System.Diagnostics;
using Discord.Commands;

namespace NadekoBot.Services
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private Logger _log;

        public event EventHandler<CommandExecutedEventArgs> CommandExecuted = delegate { };

        public CommandHandler(DiscordSocketClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _log = LogManager.GetCurrentClassLogger();

            _client.MessageReceived += MessageReceivedHandler;
        }

        private Task MessageReceivedHandler(IMessage msg)
        {
            var usrMsg = msg as IUserMessage;
            if (usrMsg == null)
                return Task.CompletedTask;
            var throwaway = Task.Run(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var t = await _commandService.Execute(usrMsg, usrMsg.Content, MultiMatchHandling.Best);
                var command = t.Item1;
                var result = t.Item2;
                sw.Stop();
                var channel = (usrMsg.Channel as ITextChannel);
                if (result.IsSuccess)
                {
                    CommandExecuted(this, new CommandExecutedEventArgs(usrMsg, command));
                    _log.Info("Command Executed after {4}s\n\t" +
                              "User: {0}\n\t" +
                              "Server: {1}\n\t" +
                              "Channel: {2}\n\t" +
                              "Message: {3}",
                              usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                              (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                              (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                              usrMsg.Content, // {3}
                              sw.Elapsed.TotalSeconds // {4}
                              );
                }
                else if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    _log.Warn("Command Errored after {5}s\n\t" +
                              "User: {0}\n\t" +
                              "Server: {1}\n\t" +
                              "Channel: {2}\n\t" +
                              "Message: {3}\n\t" +
                              "Error: {4}",
                              usrMsg.Author + " [" + usrMsg.Author.Id + "]", // {0}
                              (channel == null ? "PRIVATE" : channel.Guild.Name + " [" + channel.Guild.Id + "]"), // {1}
                              (channel == null ? "PRIVATE" : channel.Name + " [" + channel.Id + "]"), // {2}
                              usrMsg.Content,// {3}
                              result.ErrorReason, // {4}
                              sw.Elapsed.TotalSeconds // {5}
                              );
                }
            });

            return Task.CompletedTask;
        }
    }

    public class CommandExecutedEventArgs
    {
        public Command Command { get; }
        public IUserMessage Message { get; }

        public CommandExecutedEventArgs(IUserMessage msg, Command cmd)
        {
            Message = msg;
            Command = cmd;
        }
    }
}
