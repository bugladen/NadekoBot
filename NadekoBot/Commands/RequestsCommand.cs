using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;

namespace NadekoBot.Commands {
    class RequestsCommand : DiscordCommand {
        public void SaveRequest(CommandEventArgs e, string text) {
            Classes.DBHandler.Instance.InsertData(new Classes._DataModels.Request {
                RequestText = text,
                UserName = e.User.Name,
                UserId = (long)e.User.Id,
                ServerId = (long)e.Server.Id,
                ServerName = e.Server.Name,
                DateAdded = DateTime.Now
            });
        }
        // todo what if it's too long?
        public string GetRequests() {
            var task = Classes.DBHandler.Instance.GetAllRows<Classes._DataModels.Request>();

            string str = "Here are all current requests for NadekoBot:\n\n";
            int i = 1;
            foreach (var reqObj in task) {
                str += $"{reqObj.Id}. by **{reqObj.UserName}** from **{reqObj.ServerName}** at {reqObj.DateAdded.ToLocalTime()}\n" +
                       $"**{reqObj.RequestText}**\n----------\n";
            }
            return str + "\n__Type [@NadekoBot clr] to clear all of my messages.__";
        }

        public bool DeleteRequest(int requestNumber) => 
            Classes.DBHandler.Instance.Delete<Classes._DataModels.Request>(requestNumber) != null;

        /// <summary>
        /// Delete a request with a number and returns that request object.
        /// </summary>
        /// <returns>RequestObject of the request. Null if none</returns>
        public Classes._DataModels.Request ResolveRequest(int requestNumber) =>
            Classes.DBHandler.Instance.Delete<Classes._DataModels.Request>(requestNumber);

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
                                await client.GetServer((ulong)sc.ServerId).GetUser((ulong)sc.UserId).Send("**This request of yours has been resolved:**\n" + sc.RequestText);
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
