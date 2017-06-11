using Discord.Commands;
using NadekoBot.Extensions;
using System.Linq;
using Discord;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NadekoBot.Modules.Help
{
    [NadekoModule("Help", "-")]
    public class Help : NadekoTopLevelModule
    {
        private static string helpString { get; } = NadekoBot.BotConfig.HelpString;
        public static string HelpString => String.Format(helpString, NadekoBot.Credentials.ClientId, NadekoBot.ModulePrefixes[typeof(Help).Name]);

        public static string DMHelpString { get; } = NadekoBot.BotConfig.DMHelpString;

        public const string PatreonUrl = "https://patreon.com/nadekobot";
        public const string PaypalUrl = "https://paypal.me/Kwoth";

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Modules()
        {
            var embed = new EmbedBuilder().WithOkColor()
                .WithFooter(efb => efb.WithText("ℹ️" + GetText("modules_footer", Prefix)))
                .WithTitle(GetText("list_of_modules"))
                .WithDescription(string.Join("\n",
                                     NadekoBot.CommandService.Modules.GroupBy(m => m.GetTopLevelModule())
                                         .Where(m => !Permissions.Permissions.GlobalPermissionCommands.BlockedModules.Contains(m.Key.Name.ToLowerInvariant()))
                                         .Select(m => "• " + m.Key.Name)
                                         .OrderBy(s => s)));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Commands([Remainder] string module = null)
        {
            var channel = Context.Channel;

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;
            var cmds = NadekoBot.CommandService.Commands.Where(c => c.Module.GetTopLevelModule().Name.ToUpperInvariant().StartsWith(module))
                                                  .Where(c => !Permissions.Permissions.GlobalPermissionCommands.BlockedCommands.Contains(c.Aliases.First().ToLowerInvariant()))
                                                  .OrderBy(c => c.Aliases.First())
                                                  .Distinct(new CommandTextEqualityComparer())
                                                  .AsEnumerable();

            var cmdsArray = cmds as CommandInfo[] ?? cmds.ToArray();
            if (!cmdsArray.Any())
            {
                await ReplyErrorLocalized("module_not_found").ConfigureAwait(false);
                return;
            }
            var j = 0;
            var groups = cmdsArray.GroupBy(x => j++ / 48).ToArray();

            for (int i = 0; i < groups.Count(); i++)
            {
                await channel.SendTableAsync(i == 0 ? $"📃 **{GetText("list_of_commands")}**\n" : "", groups.ElementAt(i), el => $"{el.Aliases.First(),-15} {"[" + el.Aliases.Skip(1).FirstOrDefault() + "]",-8}").ConfigureAwait(false);
            }
            

            await ConfirmLocalized("commands_instr", Prefix).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task H([Remainder] string comToFind = null)
        {
            var channel = Context.Channel;

            comToFind = comToFind?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                IMessageChannel ch = channel is ITextChannel ? await ((IGuildUser)Context.User).CreateDMChannelAsync() : channel;
                await ch.SendMessageAsync(HelpString).ConfigureAwait(false);
                return;
            }
            var com = NadekoBot.CommandService.Commands.FirstOrDefault(c => c.Aliases.Select(a=>a.ToLowerInvariant()).Contains(comToFind));

            if (com == null)
            {
                await ReplyErrorLocalized("command_not_found").ConfigureAwait(false);
                return;
            }
            var str = string.Format("**`{0}`**", com.Aliases.First());
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += string.Format(" **/ `{0}`**", alias);
            var embed = new EmbedBuilder()
                .AddField(fb => fb.WithName(str).WithValue($"{string.Format(com.Summary, com.Module.Aliases.First())} {GetCommandRequirements(com)}").WithIsInline(true))
                .AddField(fb => fb.WithName(GetText("usage")).WithValue(string.Format(com.Remarks, com.Module.Aliases.First())).WithIsInline(false))
                .WithColor(NadekoBot.OkColor);
            await channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        private string GetCommandRequirements(CommandInfo cmd) => 
            string.Join(" ", cmd.Preconditions
                  .Where(ca => ca is OwnerOnlyAttribute || ca is RequireUserPermissionAttribute)
                  .Select(ca =>
                  {
                      if (ca is OwnerOnlyAttribute)
                          return Format.Bold(GetText("bot_owner_only"));
                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                          return Format.Bold(GetText("server_permission", cau.GuildPermission))
                                       .Replace("Guild", "Server");
                      return Format.Bold(GetText("channel_permission", cau.ChannelPermission))
                                       .Replace("Guild", "Server");
                  }));

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Hgit()
        {
            var helpstr = new StringBuilder();
            helpstr.AppendLine(GetText("cmdlist_donate", PatreonUrl, PaypalUrl) + "\n");
            helpstr.AppendLine("##"+ GetText("table_of_contents"));
            helpstr.AppendLine(string.Join("\n", NadekoBot.CommandService.Modules.Where(m => m.GetTopLevelModule().Name.ToLowerInvariant() != "help")
                .Select(m => m.GetTopLevelModule().Name)
                .Distinct()
                .OrderBy(m => m)
                .Prepend("Help")
                .Select(m => string.Format("- [{0}](#{1})", m, m.ToLowerInvariant()))));
            helpstr.AppendLine();
            string lastModule = null;
            foreach (var com in NadekoBot.CommandService.Commands.OrderBy(com => com.Module.GetTopLevelModule().Name).GroupBy(c => c.Aliases.First()).Select(g => g.First()))
            {
                var module = com.Module.GetTopLevelModule();
                if (module.Name != lastModule)
                {
                    if (lastModule != null)
                    {
                        helpstr.AppendLine();
                        helpstr.AppendLine($"###### [{GetText("back_to_toc")}](#{GetText("table_of_contents").ToLowerInvariant().Replace(' ', '-')})");
                    }
                    helpstr.AppendLine();
                    helpstr.AppendLine("### " + module.Name + "  ");
                    helpstr.AppendLine($"{GetText("cmd_and_alias")} | {GetText("desc")} | {GetText("usage")}");
                    helpstr.AppendLine("----------------|--------------|-------");
                    lastModule = module.Name;
                }
                helpstr.AppendLine($"{string.Join(" ", com.Aliases.Select(a => "`" + a + "`"))} |" +
                                   $" {string.Format(com.Summary, com.Module.GetPrefix())} {GetCommandRequirements(com)} |" +
                                   $" {string.Format(com.Remarks, com.Module.GetPrefix())}");
            }
            helpstr = helpstr.Replace(NadekoBot.Client.CurrentUser.Username , "@BotName");
            File.WriteAllText("../../docs/Commands List.md", helpstr.ToString());
            await ReplyConfirmLocalized("commandlist_regen").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Guide()
        {
            await ConfirmLocalized("guide", 
                "http://nadekobot.readthedocs.io/en/1.3x/Commands%20List/",
                "http://nadekobot.readthedocs.io/en/1.3x/").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Donate()
        {
            await ReplyConfirmLocalized("donate", PatreonUrl, PaypalUrl).ConfigureAwait(false);
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<CommandInfo>
    {
        public bool Equals(CommandInfo x, CommandInfo y) => x.Aliases.First() == y.Aliases.First();

        public int GetHashCode(CommandInfo obj) => obj.Aliases.First().GetHashCode();

    }
}
