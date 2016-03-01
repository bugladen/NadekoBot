using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Concurrent;
using Discord;

namespace NadekoBot.Commands {
    internal class VoiceNotificationCommand : DiscordCommand {


        public VoiceNotificationCommand()  {
            //NadekoBot.client.
        }

        //voicechannel/text channel
        private ConcurrentDictionary<Channel, Channel> subscribers = new ConcurrentDictionary<Channel, Channel>();

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            var arg = e.GetArg("voice_name");
            if (string.IsNullOrWhiteSpace("voice_name"))
                return;
            var voiceChannel = e.Server.FindChannels(arg, ChannelType.Voice).FirstOrDefault();
            if (voiceChannel == null)
                return;
            if (subscribers.ContainsKey(voiceChannel)) {
                await e.Channel.SendMessage("`Voice channel notifications disabled.`");
                return;
            }
            if (subscribers.TryAdd(voiceChannel, e.Channel)) {
                await e.Channel.SendMessage("`Voice channel notifications enabled.`");
            }
        };

        public override void Init(CommandGroupBuilder cgb) {
            /*
            cgb.CreateCommand(".voicenotif")
                  .Description("Enables notifications on who joined/left the voice channel.\n**Usage**:.voicenotif Karaoke club")
                  .Parameter("voice_name", ParameterType.Unparsed)
                  .Do(DoFunc());
                  */
        }
    }
}
