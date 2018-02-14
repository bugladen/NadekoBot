using CommandLine;
using NadekoBot.Core.Common;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Linq;

namespace NadekoBot.Core.Modules.Gambling.Common.Events
{
    public class EventOptions : INadekoCommandOptions
    {
        [Option('a', "amount", Required = false, Default = 100, HelpText = "Amount of currency each user receives.")]
        public long Amount { get; set; } = 100;
        [Option('p', "pot-size", Required = false, Default = 0, HelpText = "The maximum amount of currency that can be rewarded. 0 means no limit.")]
        public ulong PotSize { get; set; } = 0;
        [Option('t', "type", Required = false, Default = "reaction", HelpText = "Type of the event. reaction, gamestatus or joinserver.")]
        public string TypeString { get; set; } = "reaction";

        public Event.Type Type { get; set; }


        public void NormalizeOptions()
        {
            if (Amount < 0)
                Amount = 100;
            if (PotSize < 0)
                PotSize = 0;

            TypeString = TypeString.ToLowerInvariant();
            var names = Enum.GetNames(typeof(Event.Type)).Select(x => x.ToLowerInvariant());
            if (!names.Contains(TypeString))
                TypeString = "reaction";

            Type = Enum.Parse<Event.Type>(TypeString);
        }
    }
}
