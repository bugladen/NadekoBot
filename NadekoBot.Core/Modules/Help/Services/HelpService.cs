using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Linq;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Common;
using NLog;
using CommandLine;
using CommandLine.Text;

namespace NadekoBot.Modules.Help.Services
{
    public class HelpService : ILateExecutor, INService
    {
        private readonly IBotConfigProvider _bc;
        private readonly CommandHandler _ch;
        private readonly NadekoStrings _strings;
        private readonly Logger _log;

        public HelpService(IBotConfigProvider bc, CommandHandler ch, NadekoStrings strings)
        {
            _bc = bc;
            _ch = ch;
            _strings = strings;
            _log = LogManager.GetCurrentClassLogger();
        }

        public Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            try
            {
                if (guild == null)
                {
                    if (CREmbed.TryParse(_bc.BotConfig.DMHelpString, out var embed))
                        return msg.Channel.EmbedAsync(embed.ToEmbed(), embed.PlainText?.SanitizeMentions() ?? "");

                    return msg.Channel.SendMessageAsync(_bc.BotConfig.DMHelpString);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
            return Task.CompletedTask;
        }

        public EmbedBuilder GetCommandHelp(CommandInfo com, IGuild guild)
        {
            var prefix = _ch.GetPrefix(guild);
            
            var str = string.Format("**`{0}`**", prefix + com.Aliases.First());
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += string.Format(" **/ `{0}`**", prefix + alias);
            var em = new EmbedBuilder()
                .AddField(fb => fb.WithName(str)
                    .WithValue($"{com.RealSummary(prefix)}")
                    .WithIsInline(true));

            var reqs = GetCommandRequirements(com);
            if(reqs.Any())
            {
                em.AddField(GetText("requires", guild),
                    string.Join("\n", reqs));
            }

            em
                .AddField(fb => fb.WithName(GetText("usage", guild))
                    .WithValue(com.RealRemarks(prefix))
                    .WithIsInline(false))
                .WithFooter(efb => efb.WithText(GetText("module", guild, com.Module.GetTopLevelModule().Name)))
                .WithColor(NadekoBot.OkColor);

            var opt = ((NadekoOptions)com.Attributes.FirstOrDefault(x => x is NadekoOptions))?.OptionType;
            if (opt != null)
            {
                var hs = GetCommandOptionHelp(opt);
                if(!string.IsNullOrWhiteSpace(hs))
                    em.AddField(GetText("options", guild), hs, false);
            }

            return em;
        }

        public string GetCommandOptionHelp(Type opt)
        {
            var strs = opt.GetProperties()
                .Select(x => x.GetCustomAttributes(true).FirstOrDefault(a => a is OptionAttribute))
                .Where(x => x != null)
                .Cast<OptionAttribute>()
                .Select(x =>
                {
                    var toReturn = $"--{x.LongName}";

                    if (!string.IsNullOrWhiteSpace(x.ShortName))
                        toReturn += $" (-{x.ShortName})";

                    toReturn += $"   {x.HelpText}  ";
                    return toReturn;
                });

            return string.Join("\n", strs);
        }

        public string[] GetCommandRequirements(CommandInfo cmd) =>
            cmd.Preconditions
                  .Where(ca => ca is OwnerOnlyAttribute || ca is RequireUserPermissionAttribute)
                  .Select(ca =>
                  {
                      if (ca is OwnerOnlyAttribute)
                      {
                          return "Bot Owner Only";
                      }

                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                      {
                          return (cau.GuildPermission.ToString() + " Server Permission")
                                       .Replace("Guild", "Server");
                      }

                      return (cau.ChannelPermission + " Channel Permission")
                                       .Replace("Guild", "Server");
                  })
                .ToArray();

        private string GetText(string text, IGuild guild, params object[] replacements) =>
            _strings.GetText(text, guild?.Id, "Help".ToLowerInvariant(), replacements);
    }
}
