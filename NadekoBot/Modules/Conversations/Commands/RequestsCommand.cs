using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Classes.Conversations.Commands
{
    internal class RequestsCommand : DiscordCommand
    {
        public void SaveRequest(CommandEventArgs e, string text)
        {
            DbHandler.Instance.InsertData(new DataModels.Request
            {
                RequestText = text,
                UserName = e.User.Name,
                UserId = (long)e.User.Id,
                ServerId = (long)e.Server.Id,
                ServerName = e.Server.Name,
                DateAdded = DateTime.Now
            });
        }
        // todo what if it's too long?
        public string GetRequests()
        {
            var task = DbHandler.Instance.GetAllRows<DataModels.Request>();

            var str = "Here are all current requests for NadekoBot:\n\n";
            foreach (var reqObj in task)
            {
                str += $"{reqObj.Id}. by **{reqObj.UserName}** from **{reqObj.ServerName}** at {reqObj.DateAdded.ToLocalTime()}\n" +
                       $"**{reqObj.RequestText}**\n----------\n";
            }
            return str + "\n__Type [@NadekoBot clr] to clear all of my messages.__";
        }

        public bool DeleteRequest(int requestNumber) =>
            DbHandler.Instance.Delete<DataModels.Request>(requestNumber) != null;

        /// <summary>
        /// Delete a request with a number and returns that request object.
        /// </summary>
        /// <returns>RequestObject of the request. Null if none</returns>
        public DataModels.Request ResolveRequest(int requestNumber) =>
            DbHandler.Instance.Delete<DataModels.Request>(requestNumber);

        internal override void Init(CommandGroupBuilder cgb)
        {

            cgb.CreateCommand("req")
                .Alias("request")
                .Description("Requests a feature for nadeko.\n**Usage**: @NadekoBot req new_feature")
                .Parameter("all", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var str = e.Args[0];

                    try
                    {
                        SaveRequest(e, str);
                    }
                    catch
                    {
                        await e.Channel.SendMessage("Something went wrong.").ConfigureAwait(false);
                        return;
                    }
                    await e.Channel.SendMessage("Thank you for your request.").ConfigureAwait(false);
                });

            cgb.CreateCommand("lr")
                .Description("PMs the user all current nadeko requests.")
                .Do(async e =>
                {
                    var str = await Task.Run(() => GetRequests()).ConfigureAwait(false);
                    if (str.Trim().Length > 110)
                        await e.User.Send(str).ConfigureAwait(false);
                    else
                        await e.User.Send("No requests atm.").ConfigureAwait(false);
                });

            cgb.CreateCommand("dr")
                .Description("Deletes a request. **Owner Only!**")
                .Parameter("reqNumber", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    try
                    {
                        if (DeleteRequest(int.Parse(e.Args[0])))
                        {
                            await e.Channel.SendMessage(e.User.Mention + " Request deleted.").ConfigureAwait(false);
                        }
                        else
                        {
                            await e.Channel.SendMessage("No request on that number.").ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage("Error deleting request, probably NaN error.").ConfigureAwait(false);
                    }
                });

            cgb.CreateCommand("rr")
                .Description("Resolves a request. **Owner Only!**")
                .Parameter("reqNumber", ParameterType.Required)
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(async e =>
                {
                    try
                    {
                        var sc = ResolveRequest(int.Parse(e.Args[0]));
                        if (sc != null)
                        {
                            await e.Channel.SendMessage(e.User.Mention + " Request resolved, notice sent.").ConfigureAwait(false);
                            await NadekoBot.Client.GetServer((ulong)sc.ServerId).GetUser((ulong)sc.UserId).Send("**This request of yours has been resolved:**\n" + sc.RequestText).ConfigureAwait(false);
                        }
                        else
                        {
                            await e.Channel.SendMessage("No request on that number.").ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage("Error resolving request, probably NaN error.").ConfigureAwait(false);
                    }
                });
        }

        public RequestsCommand(DiscordModule module) : base(module) { }
    }
}
