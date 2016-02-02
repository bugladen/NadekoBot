using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Concurrent;
using NadekoBot.Extensions;
using Discord;
using Parse;
using System.ComponentModel;

/* Voltana's legacy
public class AsyncLazy<T> : Lazy<Task<T>> 
{ 
    public AsyncLazy(Func<T> valueFactory) : 
        base(() => Task.Factory.StartNew(valueFactory)) { }

    public AsyncLazy(Func<Task<T>> taskFactory) : 
        base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap()) { } 

    public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); } 
}
*/

namespace NadekoBot.Commands {
    class ServerGreetCommand : DiscordCommand {

        public static ConcurrentDictionary<ulong, AnnounceControls> AnnouncementsDictionary;

        public ServerGreetCommand() : base() {
            AnnouncementsDictionary = new ConcurrentDictionary<ulong, AnnounceControls>();

            NadekoBot.client.UserJoined += UserJoined;
            NadekoBot.client.UserLeft += UserLeft;

            var data = new ParseQuery<ParseObject>("Announcements")
                               .FindAsync()
                               .Result;
            if(data.Any())
            foreach (var po in data)
                AnnouncementsDictionary.TryAdd(po.Get<ulong>("serverId"), new AnnounceControls(po.Get<ulong>("serverId")).Initialize(po));
        }

        private async void UserLeft(object sender, UserEventArgs e) {
            if (!AnnouncementsDictionary.ContainsKey(e.Server.Id) ||
                !AnnouncementsDictionary[e.Server.Id].Bye) return;

            var controls = AnnouncementsDictionary[e.Server.Id];
            var channel = NadekoBot.client.GetChannel(controls.ByeChannel);
            if (channel == null) return;
            var msg = controls.GreetText.Replace("%user%", e.User.Mention).Trim();
            if (string.IsNullOrEmpty(msg))
                return;
            await channel.Send(msg);
        }

        private async void UserJoined(object sender, Discord.UserEventArgs e) {
            if (!AnnouncementsDictionary.ContainsKey(e.Server.Id) || 
                !AnnouncementsDictionary[e.Server.Id].Greet) return;

            var controls = AnnouncementsDictionary[e.Server.Id];
            var channel = NadekoBot.client.GetChannel(controls.GreetChannel);
            if (channel == null) return;

            var msg = controls.GreetText.Replace("%user%", e.User.Mention).Trim();
            if (string.IsNullOrEmpty(msg))
                return;
            await channel.Send(msg);
        }

        public class AnnounceControls {
            private ParseObject ParseObj = null;

            private bool greet;

            public bool Greet {
                get { return greet; }
                set { greet = value; Save(); }
            }

            private ulong greetChannel;

            public ulong GreetChannel {
                get { return greetChannel; }
                set { greetChannel = value; }
            }

            private string greetText = "Welcome to the server %user%";
            public string GreetText {
                get { return greetText; }
                set { greetText = value; Save(); }
            }

            private bool bye;

            public bool Bye {
                get { return bye; }
                set { bye = value; Save(); }
            }

            private ulong byeChannel;

            public ulong ByeChannel {
                get { return byeChannel; }
                set { byeChannel = value; }
            }

            private string byeText = "%user% has left the server";
            public string ByeText {
                get { return byeText; }
                set { byeText = value; Save(); }
            }


            public ulong ServerId { get; }

            public AnnounceControls(ulong serverId) {
                this.ServerId = serverId;
            }

            internal bool ToggleBye(ulong id) {
                if (Bye) {
                    return Bye = false;
                } else {
                    ByeChannel = id;
                    return Bye = true;
                }
            }

            internal bool ToggleGreet(ulong id) {
                if (Greet) {
                    return Greet = false;
                } else {
                    GreetChannel = id;
                    return Greet = true;
                }
            }

            private void Save() {
                ParseObject p = null;
                if (this.ParseObj != null)
                    p = ParseObj;
                else
                    p = ParseObj = new ParseObject("Announcements");
                p["greet"] = greet;
                p["greetText"] = greetText;
                p["greetChannel"] = greetChannel;

                p["bye"] = bye;
                p["byeText"] = byeText;
                p["byeChannel"] = byeChannel;

                p["serverId"] = ServerId;

                p.SaveAsync();
            }

            internal AnnounceControls Initialize(ParseObject po) {
                greet = po.Get<bool>("greet");
                greetText = po.Get<string>("greetText");
                greetChannel = po.Get<ulong>("greetChannel");

                bye = po.Get<bool>("bye");
                byeText = po.Get<string>("byeText");
                byeChannel = po.Get<ulong>("byeChannel");

                this.ParseObj = po;
                return this;
            }
        }

        public override Func<CommandEventArgs, Task> DoFunc() {
            throw new NotImplementedException();
        }

        public override void Init(CommandGroupBuilder cgb) {

            cgb.CreateCommand(".greet")
                .Description("Enables or Disables anouncements on the current channel when someone joins the server.")
                .Do(async e => {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    if (!AnnouncementsDictionary.ContainsKey(e.Server.Id))
                        AnnouncementsDictionary.TryAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    var controls = AnnouncementsDictionary[e.Server.Id];

                    if (controls.ToggleGreet(e.Channel.Id))
                        await e.Send("Greet announcements enabled on this channel.");
                    else
                        await e.Send("Greet announcements disabled.");
                });

            cgb.CreateCommand(".greetmsg")
                .Description("Sets a new announce message. Type %user% if you want to mention the new member.\n**Usage**: .greetmsg Welcome to the server, %user%.")
                .Parameter("msg", ParameterType.Unparsed)
                .Do(async e => {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    if (e.GetArg("msg") == null) return;
                    if (!AnnouncementsDictionary.ContainsKey(e.Server.Id))
                        AnnouncementsDictionary.TryAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    AnnouncementsDictionary[e.Server.Id].GreetText = e.GetArg("msg");
                    await e.Send("New greet message set.");
                    if (!AnnouncementsDictionary[e.Server.Id].Greet)
                        await e.Send("Enable greet messsages by typing `.greet`");
                });

            cgb.CreateCommand(".bye")
                .Description("Enables or Disables anouncements on the current channel when someone leaves the server.")
                .Do(async e => {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    if (!AnnouncementsDictionary.ContainsKey(e.Server.Id))
                        AnnouncementsDictionary.TryAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    var controls = AnnouncementsDictionary[e.Server.Id];

                    if (controls.ToggleBye(e.Channel.Id))
                        await e.Send("Bye announcements enabled on this channel.");
                    else
                        await e.Send("Bye announcements disabled.");
                });

            cgb.CreateCommand(".byemsg")
                .Description("Sets a new announce leave message. Type %user% if you want to mention the new member.\n**Usage**: .byemsg %user% has left the server.")
                .Parameter("msg", ParameterType.Unparsed)
                .Do(async e => {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    if (e.GetArg("msg") == null) return;
                    if (!AnnouncementsDictionary.ContainsKey(e.Server.Id))
                        AnnouncementsDictionary.TryAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    AnnouncementsDictionary[e.Server.Id].ByeText = e.GetArg("msg");
                    await e.Send("New bye message set.");
                    if (!AnnouncementsDictionary[e.Server.Id].Bye)
                        await e.Send("Enable bye messsages by typing `.bye`");
                });
        }
    }
}
