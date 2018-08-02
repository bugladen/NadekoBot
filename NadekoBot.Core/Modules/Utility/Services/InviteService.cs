using CommandLine;
using NadekoBot.Core.Common;
using NadekoBot.Core.Services;

namespace NadekoBot.Core.Modules.Utility.Services
{
    public class InviteService : INService
    {
        public class Options : INadekoCommandOptions
        {
            [Option('m', "max-uses", Required = false, Default = 0, HelpText = "Maximum number of times the invite can be used. Default 0 (never).")]
            public int MaxUses { get; set; } = 0;

            [Option('u', "unique", Required = false, Default = false, HelpText = "Not setting this flag will result in bot getting the existing invite with the same settings if it exists, instead of creating a new one.")]
            public bool Unique { get; set; } = false;

            [Option('t', "temporary", Required = false, Default = false, HelpText = "Setting this flag will make the link expire after 24h.")]
            public bool Temporary { get; set; } = false;

            public void NormalizeOptions()
            {
                if (MaxUses < 0)
                    MaxUses = 0;
            }
        }
    }
}
