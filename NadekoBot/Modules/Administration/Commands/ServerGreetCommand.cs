using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

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

namespace NadekoBot.Modules.Administration.Commands
{
    internal class ServerGreetCommand : DiscordCommand
    {

        public static ConcurrentDictionary<ulong, AnnounceControls> AnnouncementsDictionary;

        public static long Greeted = 0;

        public ServerGreetCommand(DiscordModule module) : base(module)
        {
            AnnouncementsDictionary = new ConcurrentDictionary<ulong, AnnounceControls>();

            NadekoBot.Client.UserJoined += UserJoined;
            NadekoBot.Client.UserLeft += UserLeft;

            var data = Classes.DbHandler.Instance.GetAllRows<DataModels.Announcement>();

            if (!data.Any()) return;
            foreach (var obj in data)
                AnnouncementsDictionary.TryAdd((ulong)obj.ServerId, new AnnounceControls(obj));
        }

        private async void UserLeft(object sender, UserEventArgs e)
        {
            try
            {
                if (!AnnouncementsDictionary.ContainsKey(e.Server.Id) ||
                    !AnnouncementsDictionary[e.Server.Id].Bye) return;

                var controls = AnnouncementsDictionary[e.Server.Id];
                var channel = NadekoBot.Client.GetChannel(controls.ByeChannel);
                var msg = controls.ByeText.Replace("%user%", "**" + e.User.Name + "**").Trim();
                if (string.IsNullOrEmpty(msg))
                    return;

                if (controls.ByePM)
                {
                    Greeted++;
                    try
                    {
                        await e.User.SendMessage($"`Farewell Message From {e.Server?.Name}`\n" + msg).ConfigureAwait(false);

                    }
                    catch { }
                }
                else
                {
                    if (channel == null) return;
                    Greeted++;
                    var toDelete = await channel.SendMessage(msg).ConfigureAwait(false);
                    if (e.Server.CurrentUser.GetPermissions(channel).ManageMessages && controls.DeleteGreetMessages)
                    {
                        await Task.Delay(30000).ConfigureAwait(false); // 5 minutes
                        await toDelete.Delete().ConfigureAwait(false);
                    }
                }
            }
            catch { }
        }

        private async void UserJoined(object sender, Discord.UserEventArgs e)
        {
            try
            {
                if (!AnnouncementsDictionary.ContainsKey(e.Server.Id) ||
                    !AnnouncementsDictionary[e.Server.Id].Greet) return;

                var controls = AnnouncementsDictionary[e.Server.Id];
                var channel = NadekoBot.Client.GetChannel(controls.GreetChannel);

                var msg = controls.GreetText.Replace("%user%", e.User.Mention).Trim();
                if (string.IsNullOrEmpty(msg))
                    return;
                if (controls.GreetPM)
                {
                    Greeted++;
                    await e.User.SendMessage($"`Welcome Message From {e.Server.Name}`\n" + msg).ConfigureAwait(false);
                }
                else
                {
                    if (channel == null) return;
                    Greeted++;
                    var toDelete = await channel.SendMessage(msg).ConfigureAwait(false);
                    if (e.Server.CurrentUser.GetPermissions(channel).ManageMessages && controls.DeleteGreetMessages)
                    {
                        await Task.Delay(30000).ConfigureAwait(false); // 5 minutes
                        await toDelete.Delete().ConfigureAwait(false);
                    }
                }
            }
            catch { }
        }

        public class AnnounceControls
        {
            private DataModels.Announcement _model { get; }

            public bool Greet {
                get { return _model.Greet; }
                set { _model.Greet = value; Save(); }
            }

            public ulong GreetChannel {
                get { return (ulong)_model.GreetChannelId; }
                set { _model.GreetChannelId = (long)value; Save(); }
            }

            public bool GreetPM {
                get { return _model.GreetPM; }
                set { _model.GreetPM = value; Save(); }
            }

            public bool ByePM {
                get { return _model.ByePM; }
                set { _model.ByePM = value; Save(); }
            }

            public string GreetText {
                get { return _model.GreetText; }
                set { _model.GreetText = value; Save(); }
            }

            public bool Bye {
                get { return _model.Bye; }
                set { _model.Bye = value; Save(); }
            }
            public ulong ByeChannel {
                get { return (ulong)_model.ByeChannelId; }
                set { _model.ByeChannelId = (long)value; Save(); }
            }

            public string ByeText {
                get { return _model.ByeText; }
                set { _model.ByeText = value; Save(); }
            }

            public ulong ServerId {
                get { return (ulong)_model.ServerId; }
                set { _model.ServerId = (long)value; }
            }

            public bool DeleteGreetMessages {
                get {
                    return _model.DeleteGreetMessages;
                }
                set {
                    _model.DeleteGreetMessages = value; Save();
                }
            }

            public AnnounceControls(DataModels.Announcement model)
            {
                this._model = model;
            }

            public AnnounceControls(ulong serverId)
            {
                this._model = new DataModels.Announcement();
                ServerId = serverId;
            }

            internal bool ToggleBye(ulong id)
            {
                if (Bye)
                {
                    return Bye = false;
                }
                else
                {
                    ByeChannel = id;
                    return Bye = true;
                }
            }

            internal bool ToggleGreet(ulong id)
            {
                if (Greet)
                {
                    return Greet = false;
                }
                else
                {
                    GreetChannel = id;
                    return Greet = true;
                }
            }

            internal bool ToggleDelete() => DeleteGreetMessages = !DeleteGreetMessages;
            internal bool ToggleGreetPM() => GreetPM = !GreetPM;
            internal bool ToggleByePM() => ByePM = !ByePM;

            private void Save()
            {
                Classes.DbHandler.Instance.Save(_model);
            }
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "grdel")
                .Description($"Toggles automatic deletion of greet and bye messages. | `{Prefix}grdel`")
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    if (ann.ToggleDelete())
                        await e.Channel.SendMessage("`Automatic deletion of greet and bye messages has been enabled.`").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("`Automatic deletion of greet and bye messages has been disabled.`").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "greet")
                .Description($"Toggles anouncements on the current channel when someone joins the server. | `{Prefix}greet`")
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    if (ann.ToggleGreet(e.Channel.Id))
                        await e.Channel.SendMessage("Greet announcements enabled on this channel.").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("Greet announcements disabled.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "greetmsg")
                .Description($"Sets a new join announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current greet message. | `{Prefix}greetmsg Welcome to the server, %user%.`")
                .Parameter("msg", ParameterType.Unparsed)
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));
                    if (string.IsNullOrWhiteSpace(e.GetArg("msg")))
                    {
                        await e.Channel.SendMessage("`Current greet message:` " + ann.GreetText);
                        return;
                    }


                    ann.GreetText = e.GetArg("msg");
                    await e.Channel.SendMessage("New greet message set.").ConfigureAwait(false);
                    if (!ann.Greet)
                        await e.Channel.SendMessage("Enable greet messsages by typing `.greet`").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "bye")
                .Description($"Toggles anouncements on the current channel when someone leaves the server. | `{Prefix}bye`")
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    if (ann.ToggleBye(e.Channel.Id))
                        await e.Channel.SendMessage("Bye announcements enabled on this channel.").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("Bye announcements disabled.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "byemsg")
                .Description($"Sets a new leave announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current bye message. | `{Prefix}byemsg %user% has left the server.`")
                .Parameter("msg", ParameterType.Unparsed)
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));
                    if (string.IsNullOrWhiteSpace(e.GetArg("msg")))
                    {
                        await e.Channel.SendMessage("`Current bye message:` " + ann.ByeText);
                        return;
                    }

                    ann.ByeText = e.GetArg("msg");
                    await e.Channel.SendMessage("New bye message set.").ConfigureAwait(false);
                    if (!ann.Bye)
                        await e.Channel.SendMessage("Enable bye messsages by typing `.bye`.").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "byepm")
                .Description($"Toggles whether the good bye messages will be sent in a PM or in the text channel. | `{Prefix}byepm`")
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;
                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));


                    if (ann.ToggleByePM())
                        await e.Channel.SendMessage("Bye messages will be sent in a PM from now on.\n ⚠ Keep in mind this might fail if the user and the bot have no common servers after the user leaves.").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("Bye messages will be sent in a bound channel from now on.").ConfigureAwait(false);
                    if (!ann.Bye)
                        await e.Channel.SendMessage("Enable bye messsages by typing `.bye`, and set the bye message using `.byemsg`").ConfigureAwait(false);
                });

            cgb.CreateCommand(Module.Prefix + "greetpm")
                .Description($"Toggles whether the greet messages will be sent in a PM or in the text channel. | `{Prefix}greetpm`")
                .Do(async e =>
                {
                    if (!e.User.ServerPermissions.ManageServer) return;

                    var ann = AnnouncementsDictionary.GetOrAdd(e.Server.Id, new AnnounceControls(e.Server.Id));

                    if (ann.ToggleGreetPM())
                        await e.Channel.SendMessage("Greet messages will be sent in a PM from now on.").ConfigureAwait(false);
                    else
                        await e.Channel.SendMessage("Greet messages will be sent in a bound channel from now on.").ConfigureAwait(false);
                    if (!ann.Greet)
                        await e.Channel.SendMessage("Enable greet messsages by typing `.greet`, and set the greet message using `.greetmsg`").ConfigureAwait(false);
                });
        }
    }
}
