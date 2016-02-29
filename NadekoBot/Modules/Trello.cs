using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Modules;
using Manatee.Trello.ManateeJson;
using Manatee.Trello;
using System.Timers;
using NadekoBot.Extensions;

namespace NadekoBot.Modules {
    class Trello : DiscordModule {
        public override void Install(ModuleManager manager) {

            var client = manager.Client;

            var serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new Manatee.Trello.WebApi.WebApiClientProvider();
            TrelloAuthorization.Default.AppKey = NadekoBot.TrelloAppKey;
            //TrelloAuthorization.Default.UserToken = "[your user token]";

            Discord.Channel bound = null;
            Board board = null;

            Timer t = new Timer();
            t.Interval = 2000;
            List<string> last5ActionIDs = null;
            t.Elapsed += async (s, e) => {
                try {
                    if (board == null || bound == null)
                        return; //do nothing if there is no bound board

                    board.Refresh();
                    IEnumerable<Manatee.Trello.Action> cur5Actions;
                    if (board.Actions.Count() < 5)
                        cur5Actions = board.Actions.Take(board.Actions.Count());
                    else
                        cur5Actions = board.Actions.Take(5);

                    if (last5ActionIDs == null) {
                        last5ActionIDs = new List<string>();
                        foreach (var a in cur5Actions)
                            last5ActionIDs.Add(a.Id);
                        return;
                    }
                    foreach (var a in cur5Actions.Where(ca => !last5ActionIDs.Contains(ca.Id))) {
                        await bound.Send("**--TRELLO NOTIFICATION--**\n" + a.ToString());
                    }
                    last5ActionIDs.Clear();
                    foreach (var a in cur5Actions)
                        last5ActionIDs.Add(a.Id);
                } catch (Exception ex) {
                    Console.WriteLine("Timer failed " + ex.ToString());
                }
            };

            manager.CreateCommands("", cgb => {
                
                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                cgb.CreateCommand("join")
                    .Alias("j")
                    .Description("Joins a server")
                    .Parameter("code", Discord.Commands.ParameterType.Required)
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        try {
                            await (await client.GetInvite(e.GetArg("code"))).Accept();
                        } catch (Exception ex) {
                            Console.WriteLine(ex.ToString());
                        }
                    });

                cgb.CreateCommand("bind")
                    .Description("Bind a trello bot to a single channel. You will receive notifications from your board when something is added or edited.")
                    .Parameter("board_id", Discord.Commands.ParameterType.Required)
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound != null) return;
                        try {
                            bound = e.Channel;
                            board = new Board(e.GetArg("board_id").Trim());
                            board.Refresh();
                            await e.Channel.SendMessage("Successfully bound to this channel and board " + board.Name);
                            t.Start();
                        } catch (Exception ex) {
                            Console.WriteLine("Failed to join the board. " + ex.ToString());
                        }
                    });

                cgb.CreateCommand("unbind")
                    .Description("Unbinds a bot from the channel and board.")
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || bound != e.Channel) return;
                        t.Stop();
                        bound = null;
                        board = null;
                        await e.Channel.SendMessage("Successfully unbound trello from this channel.");

                    });

                cgb.CreateCommand("lists")
                    .Alias("list")
                    .Description("Lists all lists yo ;)")
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || board == null || bound != e.Channel) return;
                        await e.Channel.SendMessage("Lists for a board '" + board.Name + "'\n" + string.Join("\n", board.Lists.Select(l => "**• " + l.ToString() + "**")));
                    });

                cgb.CreateCommand("cards")
                    .Description("Lists all cards from the supplied list. You can supply either a name or an index.")
                    .Parameter("list_name", Discord.Commands.ParameterType.Unparsed)
                    .Do(async e => {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        if (bound == null || board == null || bound != e.Channel || e.GetArg("list_name") == null) return;

                        int num;
                        var success = int.TryParse(e.GetArg("list_name"), out num);
                        List list = null;
                        if (success && num <= board.Lists.Count() && num > 0)
                            list = board.Lists[num - 1];
                        else
                            list = board.Lists.Where(l => l.Name == e.GetArg("list_name")).FirstOrDefault();


                        if (list != null)
                            await e.Channel.SendMessage("There are " + list.Cards.Count() + " cards in a **" + list.Name + "** list\n" + string.Join("\n", list.Cards.Select(c => "**• " + c.ToString() + "**")));
                        else
                            await e.Channel.SendMessage("No such list.");
                    });
            });
        }
    }
}
