using Discord.Modules;
using Manatee.Trello;
using Manatee.Trello.ManateeJson;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Action = Manatee.Trello.Action;

namespace NadekoBot.Modules.Trello
{
    internal class TrelloModule : DiscordModule
    {
        private readonly Timer t = new Timer { Interval = 2000 };
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Trello;

        public override void Install(ModuleManager manager)
        {

            var client = manager.Client;

            var serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new Manatee.Trello.WebApi.WebApiClientProvider();
            TrelloAuthorization.Default.AppKey = NadekoBot.Creds.TrelloAppKey;
            //TrelloAuthorization.Default.UserToken = "[your user token]";

            Discord.Channel bound = null;
            Board board = null;

            List<string> last5ActionIDs = null;
            t.Elapsed += async (s, e) =>
            {
                try
                {
                    if (board == null || bound == null)
                        return; //do nothing if there is no bound board

                    board.Refresh();
                    var cur5Actions = board.Actions.Take(board.Actions.Count() < 5 ? board.Actions.Count() : 5);
                    var cur5ActionsArray = cur5Actions as Action[] ?? cur5Actions.ToArray();

                    if (last5ActionIDs == null)
                    {
                        last5ActionIDs = cur5ActionsArray.Select(a => a.Id).ToList();
                        return;
                    }

                    foreach (var a in cur5ActionsArray.Where(ca => !last5ActionIDs.Contains(ca.Id)))
                    {
                        await bound.Send("**--TRELLO NOTIFICATION--**\n" + a.ToString()).ConfigureAwait(false);
                    }
                    last5ActionIDs.Clear();
                    last5ActionIDs.AddRange(cur5ActionsArray.Select(a => a.Id));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Timer failed " + ex.ToString());
                }
            };

            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                cgb.CreateCommand(Prefix + "bind")
                    .Description("Bind a trello bot to a single channel. " +
                                 "You will receive notifications from your board when something is added or edited." +
                                 $" | `{Prefix}bind [board_id]`")
                    .Parameter("board_id", Discord.Commands.ParameterType.Required)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound != null) return;
                        try
                        {
                            bound = e.Channel;
                            board = new Board(e.GetArg("board_id").Trim());
                            board.Refresh();
                            await e.Channel.SendMessage("Successfully bound to this channel and board " + board.Name);
                            t.Start();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to join the board. " + ex.ToString());
                        }
                    });

                cgb.CreateCommand(Prefix + "unbind")
                    .Description("Unbinds a bot from the channel and board.")
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || bound != e.Channel) return;
                        t.Stop();
                        bound = null;
                        board = null;
                        await e.Channel.SendMessage("Successfully unbound trello from this channel.").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "lists")
                    .Alias(Prefix + "list")
                    .Description("Lists all lists yo ;)")
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || board == null || bound != e.Channel) return;
                        await e.Channel.SendMessage("Lists for a board '" + board.Name + "'\n" + string.Join("\n", board.Lists.Select(l => "**• " + l.ToString() + "**")))
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "cards")
                    .Description($"Lists all cards from the supplied list. You can supply either a name or an index. | `{Prefix}cards index`")
                    .Parameter("list_name", Discord.Commands.ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || board == null || bound != e.Channel || e.GetArg("list_name") == null) return;

                        int num;
                        var success = int.TryParse(e.GetArg("list_name"), out num);
                        List list = null;
                        if (success && num <= board.Lists.Count() && num > 0)
                            list = board.Lists[num - 1];
                        else
                            list = board.Lists.FirstOrDefault(l => l.Name == e.GetArg("list_name"));


                        if (list != null)
                            await e.Channel.SendMessage("There are " + list.Cards.Count() + " cards in a **" + list.Name + "** list\n" + string.Join("\n", list.Cards.Select(c => "**• " + c.ToString() + "**")))
                                           .ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("No such list.")
                                           .ConfigureAwait(false);
                    });
            });
        }
    }
}
