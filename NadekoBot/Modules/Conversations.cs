using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace NadekoBot.Modules
{
    class Conversations : DiscordModule
    {
        private string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations() : base()
        {
            commands.Add(new CopyCommand());
        }

        private CommandBuilder CreateCommand(CommandGroupBuilder cbg, string txt)
        {
            CommandBuilder cb = cbg.CreateCommand(txt);
            return AliasCommand(cb, txt);
        }

        private CommandBuilder AliasCommand(CommandBuilder cb, string txt)
        {
            return cb.Alias(new string[] { "," + txt, "-" + txt });
        }

        public override void Install(ModuleManager manager)
        {
            Random rng = new Random();

            manager.CreateCommands("", cgb =>
            {
                var client = manager.Client;

                cgb.CreateCommand("\\o\\")
                    .Description("Nadeko replies with /o/")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, Mention.User(e.User) + "/o/");
                    });

                cgb.CreateCommand("/o/")
                    .Description("Nadeko replies with \\o\\")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, Mention.User(e.User) + "\\o\\");
                    });
            });

            manager.CreateCommands(NadekoBot.botMention, cgb =>
            {
                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                CreateCommand(cgb, "do you love me")
                    .Description("Replies with positive answer only to the bot owner.")
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                            await client.SendMessage(e.Channel, Mention.User(e.User) + ", Of course I do, my Master.");
                        else
                            await client.SendMessage(e.Channel, Mention.User(e.User) + ", Don't be silly.");
                    });
                
                CreateCommand(cgb, "die")
                    .Description("Works only for the owner. Shuts the bot down.")
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            Timer t = new Timer();
                            t.Interval = 2000;
                            t.Elapsed += (s, ev) => { Environment.Exit(0); };
                            t.Start();
                            await client.SendMessage(e.Channel, Mention.User(e.User) + ", Yes, my love.");
                        }
                        else
                            await client.SendMessage(e.Channel, Mention.User(e.User) + ", No.");
                    });

                CreateCommand(cgb, "how are you")
                    .Description("Replies positive only if bot owner is online.")
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            await client.SendMessage(e.Channel, Mention.User(e.User) + " I am great as long as you are here.");
                        }
                        else
                        {
                            var kw = client.GetUser(e.Server, NadekoBot.OwnerID);
                            if (kw != null && kw.Status == UserStatus.Online)
                            {
                                await client.SendMessage(e.Channel, Mention.User(e.User) + " I am great as long as " + Mention.User(kw) + " is with me.");
                            }
                            else
                            {
                                await client.SendMessage(e.Channel, Mention.User(e.User) + " I am sad. My Master is not with me.");
                            }
                        }
                    });

                CreateCommand(cgb, "insult")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Only works for owner. Insults @X person.\nUsage: @NadekoBot insult @X.")
                    .Do(async e =>
                    {
                        List<string> insults = new List<string> { " you are a poop.", " you jerk.", " i will eat you when i get my powers back." };
                        Random r = new Random();
                        var u = e.Message.MentionedUsers.Last();
                        if (u.Id == NadekoBot.OwnerID)
                        {
                            await client.SendMessage(e.Channel, "I would never insult my master <3");
                        }
                        else if (e.User.Id == NadekoBot.OwnerID)
                        {
                            await client.SendMessage(e.Channel, Mention.User(u) + insults[r.Next(0, insults.Count)]);
                        }
                        else
                        {
                            await client.SendMessage(e.Channel, Mention.User(e.User) + " Eww, why would i do that for you ?!");
                        }
                    });

                CreateCommand(cgb, "praise")
                    .Description("Only works for owner. Praises @X person.\nUsage: @NadekoBot insult @X.")
                    .Parameter("mention", ParameterType.Required)
                    .Do(async e =>
                    {
                        List<string> praises = new List<string> { " You are cool.", " You are nice... But don't get any wrong ideas.", " You did a good job." };
                        Random r = new Random();
                        var u = e.Message.MentionedUsers.First();
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            if (u.Id != NadekoBot.OwnerID)
                                await client.SendMessage(e.Channel, Mention.User(u) + praises[r.Next(0, praises.Count)]);
                            else
                            {
                                await client.SendMessage(e.Channel, Mention.User(u) + " No need, you know I love you <3");
                            }
                        }
                        else
                        {
                            if (u.Id == NadekoBot.OwnerID)
                            {
                                await client.SendMessage(e.Channel, Mention.User(e.User) + " I don't need your permission to praise my beloved Master <3");
                            }
                            else
                            {
                                await client.SendMessage(e.Channel, Mention.User(e.User) + " Yeah... No.");
                            }
                        }
                    });

                CreateCommand(cgb, "are you real")
                    .Description("Useless.")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, Mention.User(e.User) + " I will be soon.");
                    });

                cgb.CreateCommand("are you there")
                    .Description("Checks if nadeko is operational.")
                    .Alias(new string[] { "!", "?", "??", "???", "!!", "!!!" })
                    .Do(SayYes());

                CreateCommand(cgb, "draw")
                    .Description("Nadeko instructs you to type $draw. Gambling functions start with $")
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, "Sorry i dont gamble, type $draw for that function.");
                    });

                CreateCommand(cgb, "uptime")
                    .Description("Shows how long is Nadeko running for.")
                    .Do(async e =>
                    {
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        string str = "I am online for " + time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
                        await client.SendMessage(e.Channel, str);
                    });
                CreateCommand(cgb, "fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.\nUsage: @NadekoBot fire [x]")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e =>
                    {
                        int count = 0;
                        if (e.Args?.Length > 0)
                            int.TryParse(e.Args[0], out count);

                        if (count < 1)
                            count = 1;
                        else if (count > 12)
                            count = 12;
                        string str = "";
                        for (int i = 0; i < count; i++)
                        {
                            str += firestr;
                        }
                        await client.SendMessage(e.Channel, str);
                    });

                CreateCommand(cgb, "rip")
                    .Description("Shows a grave image.Optional parameter [@X] instructs her to put X's name on the grave.\nUsage: @NadekoBot rip [@X]")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {

                        if (e.Message.MentionedUsers.Count() == 1)
                        {
                            await client.SendFile(e.Channel, @"images\rip.png");
                        }
                        else
                        {
                            foreach (User u in e.Message.MentionedUsers)
                            {
                                if (u.Name == "NadekoBot") continue;
                                RipName(u.Name);
                                await client.SendFile(e.Channel, @"images\ripnew.png");
                            }
                        }
                    });

                cgb.CreateCommand("j")
                    .Description("Joins a server using a code. Obsolete, since nadeko will autojoin any valid code in chat.")
                    .Parameter("id", ParameterType.Required)
                    .Do(async e =>
                    {
                        try
                        {
                            await client.AcceptInvite(client.GetInvite(e.Args[0]).Result);
                            await client.SendMessage(e.Channel, "I got in!");
                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "Invalid code.");
                        }
                    });

                cgb.CreateCommand("i")
                   .Description("Pulls a first image using a search parameter.\nUsage: @NadekoBot img Multiword_search_parameter")
                   .Alias("img")
                   .Parameter("all", ParameterType.Unparsed)
                       .Do(async e =>
                       {
                           var httpClient = new System.Net.Http.HttpClient();
                           string str = e.Args[0];

                           var r = httpClient.GetAsync("http://ajax.googleapis.com/ajax/services/search/images?v=1.0&q=" + Uri.EscapeDataString(str) + "&start=0").Result;

                           dynamic obj = JObject.Parse(r.Content.ReadAsStringAsync().Result);
                           if (obj.responseData.results.Count == 0)
                           {
                               await client.SendMessage(e.Channel, "No results found for that keyword :\\");
                               return;
                           }
                           string s = Searches.ShortenUrl(obj.responseData.results[0].url.ToString());
                           await client.SendMessage(e.Channel, s);
                       });

                cgb.CreateCommand("ir")
                    .Description("Pulls a random image using a search parameter.\nUsage: @NadekoBot img Multiword_search_parameter")
                    .Alias("imgrandom")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {

                        var httpClient = new System.Net.Http.HttpClient();
                        string str = e.Args[0];
                        var r = httpClient.GetAsync("http://ajax.googleapis.com/ajax/services/search/images?v=1.0&q=" + Uri.EscapeDataString(str) + "&start=" + rng.Next(0, 30)).Result;
                        dynamic obj = JObject.Parse(r.Content.ReadAsStringAsync().Result);
                        if (obj.responseData.results.Count == 0)
                        {
                            await client.SendMessage(e.Channel, "No results found for that keyword :\\");
                            return;
                        }
                        int rnd = rng.Next(0, obj.responseData.results.Count);
                        string s = Searches.ShortenUrl(obj.responseData.results[rnd].url.ToString());
                        await client.SendMessage(e.Channel, s);
                    });


                AliasCommand(CreateCommand(cgb, "save"), "s")
                    .Description("Saves something for the owner in a file.")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            string m = "";
                            try
                            {
                                FileStream f = File.OpenWrite("saves.txt");
                                m = e.Args[0];
                                byte[] b = Encoding.ASCII.GetBytes(m + "\n");
                                f.Seek(f.Length, SeekOrigin.Begin);
                                f.Write(b, 0, b.Length);
                                f.Close();
                            }
                            catch (Exception)
                            {
                                await client.SendMessage(e.Channel, "Error saving. Sorry :(");
                            }
                            if (m.Length > 0)
                                await client.SendMessage(e.Channel, "I saved this for you: " + Environment.NewLine + "```" + m + "```");
                            else
                                await client.SendMessage(e.Channel, "No point in saving empty message...");
                        }
                        else await client.SendMessage(e.Channel, "Not for you, only my Master <3");
                    });

                CreateCommand(cgb, "ls")
                    .Description("Shows all saved items.")
                    .Do(async e =>
                    {
                        FileStream f = File.OpenRead("saves.txt");
                        if (f.Length == 0)
                        {
                            await client.SendMessage(e.Channel, "Saves are empty.");
                            return;
                        }
                        byte[] b = new byte[f.Length / sizeof(byte)];
                        f.Read(b, 0, b.Length);
                        f.Close();
                        string str = Encoding.ASCII.GetString(b);
                        await client.SendPrivateMessage(e.User, "```" + (str.Length < 1950 ? str : str.Substring(0, 1950)) + "```");
                    });

                CreateCommand(cgb, "cs")
                    .Description("Deletes all saves")
                    .Do(async e =>
                    {
                        File.Delete("saves.txt");
                        await client.SendMessage(e.Channel, "Cleared all saves.");
                    });

                CreateCommand(cgb, "bb")
                    .Description("Says bye to someone.\nUsage: @NadekoBot bb @X")
                    .Parameter("ppl", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string str = "Bye";
                        foreach (var u in e.Message.MentionedUsers)
                        {
                            str += " " + Mention.User(u);
                        }
                        await client.SendMessage(e.Channel, str);
                    });

                AliasCommand(CreateCommand(cgb, "req"), "request")
                    .Description("Requests a feature for nadeko.\nUsage: @NadekoBot req Mutliword_feature_request")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string str = e.Args[0];

                        try
                        {
                            StatsCollector.SaveRequest(e, str);
                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "Something went wrong.");
                            return;
                        }
                        await client.SendMessage(e.Channel, "Thank you for your request.");
                    });

                CreateCommand(cgb, "lr")
                    .Description("PMs the user all current nadeko requests.")
                    .Do(async e =>
                    {
                        string str = StatsCollector.GetRequests();
                        if (str.Trim().Length > 110)
                            await client.SendPrivateMessage(e.User, str);
                        else
                            await client.SendPrivateMessage(e.User, "No requests atm.");
                    });

                CreateCommand(cgb, "dr")
                    .Description("Deletes a request. Only owner is able to do this.")
                    .Parameter("reqNumber", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            try
                            {
                                if (StatsCollector.DeleteRequest(int.Parse(e.Args[0])))
                                {
                                    await client.SendMessage(e.Channel, Mention.User(e.User) + " Request deleted.");
                                }
                                else
                                {
                                    await client.SendMessage(e.Channel, "No request on that number.");
                                }
                            }
                            catch
                            {
                                await client.SendMessage(e.Channel, "Error deleting request, probably NaN error.");
                            }
                        }
                        else await client.SendMessage(e.Channel, "You don't have permission to do that.");
                    });

                CreateCommand(cgb, "rr")
                    .Description("Resolves a request. Only owner is able to do this.")
                    .Parameter("reqNumber", ParameterType.Required)
                    .Do(async e =>
                    {
                        if (e.User.Id == NadekoBot.OwnerID)
                        {
                            try
                            {
                                var sc = StatsCollector.ResolveRequest(int.Parse(e.Args[0]));
                                if (sc != null)
                                {
                                    await client.SendMessage(e.Channel, Mention.User(e.User) + " Request resolved, notice sent.");
                                    await client.SendPrivateMessage(client.GetUser(client.GetServer(sc.ServerId), sc.Id), "**This request of yours has been resolved:**\n" + sc.Text);
                                }
                                else
                                {
                                    await client.SendMessage(e.Channel, "No request on that number.");
                                }
                            }
                            catch
                            {
                                await client.SendMessage(e.Channel, "Error resolving request, probably NaN error.");
                            }
                        }
                        else await client.SendMessage(e.Channel, "You don't have permission to do that.");
                    });

                CreateCommand(cgb, "clr")
                    .Description("Clears some of nadeko's messages from the current channel.")
                    .Do(async e =>
                    {
                        try
                        {
                            if (e.Channel.Messages.Count() < 50)
                            {
                                await client.DownloadMessages(e.Channel, 100);
                            }

                            await client.DeleteMessages(e.Channel.Messages.Where(msg => msg.User.Id == client.CurrentUser.Id));

                        }
                        catch (Exception)
                        {
                            await client.SendMessage(e.Channel, "I cant do it :(");
                        }
                    });

                CreateCommand(cgb, "call")
                    .Description("Useless. Writes calling @X to chat.\nUsage: @NadekoBot call @X ")
                    .Parameter("who", ParameterType.Required)
                    .Do(async e =>
                    {
                        await client.SendMessage(e.Channel, "Calling " + e.Args[0] + "...");
                    });
                CreateCommand(cgb, "hide")
                    .Description("Hides nadeko in plain sight!11!!")
                    .Do(async e =>
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream())
                            using (Image img = Image.FromFile("images/hidden.png"))
                            {
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                                await client.EditProfile("", null, null, null,  ms, ImageType.Png);
                            }
                        }
                        catch (Exception ex)
                        {
                            StatsCollector.DEBUG_LOG(ex.ToString());
                        }
                    });

                CreateCommand(cgb, "unhide")
                    .Description("Hides nadeko in plain sight!11!!")
                    .Do(async e =>
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream())
                            using (Image img = Image.FromFile("images/nadeko.jpg"))
                            {
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                                await client.EditProfile("", null, null, null,ms, ImageType.Jpeg);
                            }
                        }
                        catch (Exception ex)
                        {
                            StatsCollector.DEBUG_LOG(ex.ToString());
                        }
                    });


                //TODO add eval
                /*
                cgb.CreateCommand(">")
                    .Parameter("code", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (e.Message.User.Id == NadekoBot.OwnerId)
                        {
                            var result = await CSharpScript.EvaluateAsync(e.Args[0]);
                            await client.SendMessage(e.Channel, result?.ToString() ?? "null");
                            return;
                        }
                    });*/
            });
        }

        public void RipName(string name)
        {
            Bitmap bm = new Bitmap(Image.FromFile(@"images\rip.png"));

            int offset = name.Length * 5;

            int fontSize = 20;

            if (name.Length > 10)
            {
                fontSize -= (name.Length - 10) / 2;
            }

            //TODO use measure string
            Graphics g = Graphics.FromImage(bm);
            g.DrawString(name, new Font("Comic Sans MS", fontSize, FontStyle.Bold), Brushes.Black, 100 - offset, 200);
            g.DrawString("? - " + DateTime.Now.Year, new Font("Consolas", 12, FontStyle.Bold), Brushes.Black, 80, 235);
            g.Flush();
            g.Dispose();

            bm.Save(@"images\ripnew.png");
        }

        private Func<CommandEventArgs, Task> SayYes()
        {
            return async e =>
            {
                await NadekoBot.client.SendMessage(e.Channel, "Yes. :)");
            };
        }
    }
}
