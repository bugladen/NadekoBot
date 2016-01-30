using System;
using System.Threading.Tasks;
using Discord.Commands;
using Parse;
using NadekoBot.Extensions;

namespace NadekoBot.Commands {
    class RequestsCommand : DiscordCommand {
        public void SaveRequest(CommandEventArgs e, string text) {

            var obj = new ParseObject("Requests");

            obj["ServerId"] = e.Server.Id;
            obj["ServerName"] = e.Server.Name;
            obj["UserId"] = e.User.Id;
            obj["UserName"] = e.User.Name;
            obj["Request"] = text;

            obj.SaveAsync();
        }
        // todo what if it's too long?
        public string GetRequests() {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;

            string str = "Here are all current requests for NadekoBot:\n\n";
            int i = 1;
            foreach (var reqObj in task) {
                str += (i++) + ". by **" + reqObj["UserName"] + "** from **" + reqObj["ServerName"] + "**  at " + reqObj.CreatedAt.Value.ToLocalTime() + "\n";
                str += "**" + reqObj["Request"] + "**\n----------\n";
            }
            return str + "\n__Type [@NadekoBot clr] to clear all of my messages.__";
        }

        public bool DeleteRequest(int requestNumber) {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;
            int i = 1;
            foreach (var reqObj in task) {
                if (i == requestNumber) {
                    reqObj.DeleteAsync();
                    return true;
                }
                i++;
            }
            return false;
        }

        public class ResolveRequestObject {
            public ulong Id;
            public ulong ServerId;
            public string Text;
        }
        /// <summary>
        /// Resolves a request with a number and returns that users id.
        /// </summary>
        /// <returns>RequestObject of the request. Null if none</returns>
        public ResolveRequestObject ResolveRequest(int requestNumber) {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;
            int i = 1;
            foreach (var reqObj in task) {
                if (i == requestNumber) {
                    var txt = reqObj.Get<string>("Request");
                    var id = reqObj.Get<ulong>("UserId");
                    var sid = reqObj.Get<ulong>("ServerId");
                    reqObj.DeleteAsync();
                    return new ResolveRequestObject { Id = id, Text = txt, ServerId = sid };
                }
                i++;
            }
            return null;
        }

        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        public override void Init(CommandGroupBuilder cgb) {

            cgb.CreateCommand("req")
                .Alias("request")
                .Description("Requests a feature for nadeko.\n**Usage**: @NadekoBot req new_feature")
                .Parameter("all", ParameterType.Unparsed)
                .Do(async e => {
                    string str = e.Args[0];

                    try {
                        SaveRequest(e, str);
                    } catch (Exception) {
                        await e.Send("Something went wrong.");
                        return;
                    }
                    await e.Send("Thank you for your request.");
                });

            cgb.CreateCommand("lr")
                .Description("PMs the user all current nadeko requests.")
                .Do(async e => {
                    string str = GetRequests();
                    if (str.Trim().Length > 110)
                        await e.User.Send(str);
                    else
                        await e.User.Send("No requests atm.");
                });

            cgb.CreateCommand("dr")
                .Description("Deletes a request. Only owner is able to do this.")
                .Parameter("reqNumber", ParameterType.Required)
                .Do(async e => {
                    if (e.User.Id == NadekoBot.OwnerID) {
                        try {
                            if (DeleteRequest(int.Parse(e.Args[0]))) {
                                await e.Send(e.User.Mention + " Request deleted.");
                            } else {
                                await e.Send("No request on that number.");
                            }
                        } catch {
                            await e.Send("Error deleting request, probably NaN error.");
                        }
                    } else await e.Send("You don't have permission to do that.");
                });

            cgb.CreateCommand("rr")
                .Description("Resolves a request. Only owner is able to do this.")
                .Parameter("reqNumber", ParameterType.Required)
                .Do(async e => {
                    if (e.User.Id == NadekoBot.OwnerID) {
                        try {
                            var sc = ResolveRequest(int.Parse(e.Args[0]));
                            if (sc != null) {
                                await e.Send(e.User.Mention + " Request resolved, notice sent.");
                                await client.GetServer(sc.ServerId).GetUser(sc.Id).Send("**This request of yours has been resolved:**\n" + sc.Text);
                            } else {
                                await e.Send("No request on that number.");
                            }
                        } catch {
                            await e.Send("Error resolving request, probably NaN error.");
                        }
                    } else await e.Send("You don't have permission to do that.");
                });
        }
    }
}
