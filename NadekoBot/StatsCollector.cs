using Discord;
using Discord.Commands;
using Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace NadekoBot
{
    public class StatsCollector
    {

        private CommandService _service;

        string lastMention = "";
        string lastMessage = "No messages.";
        int commandsRan = 0;
        string dataLastSent = "Data last sent at: NEVER";

        List<string> messages = new List<string>();

        public StatsCollector(CommandService service)
        {
            this._service = service;

            _service.RanCommand += StatsCollector_RanCommand;
            //NadekoBot.client.MessageReceived += Client_MessageReceived;

            StartCollecting();
        }

        private void FillConsole() {
            Console.Clear();

            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            string str = "Online for " + time.Days + "d, " + time.Hours + "h, " + time.Minutes + "m, "+time.Seconds+"s.";

            Console.SetCursorPosition(0, 0);
            Console.Write(str);

            Console.SetCursorPosition(0, 1);
            Console.Write(dataLastSent);

            Console.SetCursorPosition(0, 2);
            Console.Write("Commands ran since start: " +commandsRan);

            Console.SetCursorPosition(0, 3);
            Console.Write(lastMention);

            Console.SetCursorPosition(0, 4);
            Console.WriteLine(lastMessage);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {

            lastMessage = "[" + e.User.Name + "] on [" + e.Server.Name + "] server, channel: [" + e.Channel.Name + "]                                \n" + "Body: " + e.Message.Text + "                 ";
            
            if (e.Message.MentionedUsers.Where(u => u.Id == NadekoBot.OwnerID).Count() > 0)
            {
                lastMention = "You were last mentioned in '" + e.Server.Name + "' server, channel '" + e.Channel.Name + "', by " + e.User.Name;
            }
            
        }

        private async void TryJoin(MessageEventArgs e, string code) {
            try
            {
                await NadekoBot.client.AcceptInvite(await NadekoBot.client.GetInvite(code));
                await e.Send(e.User.Mention + " I joined it, thanks :)");
                DEBUG_LOG("Sucessfuly joined server with code " + code);
                DEBUG_LOG("Here is a link for you: discord.gg/" + code);
            }
            catch (Exception ex) {
                DEBUG_LOG("Failed to join " + code);
                DEBUG_LOG("Reason: " + ex.ToString());
            }
        }

        public static void DEBUG_LOG(string text) {
            NadekoBot.client.GetChannel(119365591852122112).Send(text);
        }

        private void StartCollecting() {
            Timer t = new Timer();
            t.Interval = 3600000;
            t.Enabled = true;
            t.Elapsed += (s, e) =>
            {
                var obj = new ParseObject("Stats");
                dataLastSent = "Data last sent at: "+DateTime.Now.Hour+":"+DateTime.Now.Minute;
                obj["OnlineUsers"] = NadekoBot.client.AllUsers.Count();
                obj["ConnectedServers"] = NadekoBot.client.AllServers.Count();

                obj.SaveAsync();
            };
            Console.WriteLine("Server stats sent.");
        }

        public static void SaveRequest(CommandEventArgs e, string text) {

            var obj = new ParseObject("Requests");

            obj["ServerId"] = e.Server.Id;
            obj["ServerName"] = e.Server.Name;
            obj["UserId"] = e.User.Id;
            obj["UserName"] = e.User.Name;
            obj["Request"] = text;

            obj.SaveAsync();
        }

        public static string GetRequests() {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;

            string str = "Here are all current requests for NadekoBot:\n\n";
            int i = 1;
            foreach (var reqObj in task)
            {
                
                str += (i++) + ". by **" + reqObj["UserName"] +"** from **" + reqObj["ServerName"] + "**  at "+ reqObj.CreatedAt.Value.ToLocalTime() + "\n";
                str+= "**"+reqObj["Request"]+"**\n----------\n";
            }
            return str+"\n__Type [@NadekoBot clr] to clear all of my messages.__";
        }

        public static bool DeleteRequest(int requestNumber) {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;
            int i = 1;
            foreach (var reqObj in task)
            {
                if (i == requestNumber)
                {
                    reqObj.DeleteAsync();
                    return true;
                }
                i++;
            }
            return false;
        }
        /// <summary>
        /// Resolves a request with a number and returns that users id.
        /// </summary>
        /// <returns>RequestObject of the request. Null if none</returns>
        public static ResolveRequestObject ResolveRequest(int requestNumber) {
            var task = ParseObject.GetQuery("Requests")
                .FindAsync().Result;
            int i = 1;
            foreach (var reqObj in task)
            {
                if (i == requestNumber) {
                    var txt = reqObj.Get<string>("Request");
                    var id = reqObj.Get<long>("UserId");
                    var sid = reqObj.Get<long>("ServerId");
                    reqObj.DeleteAsync();
                    return new ResolveRequestObject { Id = id, Text = txt, ServerId=sid };
                }
                i++;
            }
            return null;
        }

        public class ResolveRequestObject {
            public long Id;
            public long ServerId;
            public string Text;
        }

        private void StatsCollector_RanCommand(object sender, CommandEventArgs e)
        {
            Console.WriteLine("command ran");
            commandsRan++;
            var obj = new ParseObject("CommandsRan");

            obj["ServerId"] = e.Server.Id;
            obj["ServerName"] = e.Server.Name;

            obj["ChannelId"] = e.Channel.Id;
            obj["ChannelName"] = e.Channel.Name;

            obj["UserId"] = e.User.Id;
            obj["UserName"] = e.User.Name;

            obj["CommandName"] = e.Command.Text;
            obj.SaveAsync();
        }
    }
}
