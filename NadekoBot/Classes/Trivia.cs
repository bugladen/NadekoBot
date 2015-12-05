using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NadekoBot
{
    public class Trivia : DiscordCommand
    {
        public static Dictionary<long, TriviaGame> runningTrivias;

        public Trivia() : base() {
            runningTrivias = new Dictionary<long, TriviaGame>();
        }

        public static TriviaGame StartNewGame(CommandEventArgs e) {
            if (runningTrivias.ContainsKey(e.User.Server.Id))
                return null;

            var tg = new TriviaGame(e, NadekoBot.client);
            runningTrivias.Add(e.Server.Id, tg);
            
            return tg;
        }

        public TriviaQuestion GetCurrentQuestion(long serverId) {
            return runningTrivias[serverId].currentQuestion;
        }

        public override Func<CommandEventArgs, Task> DoFunc()
        {
            return async e =>
            {
                TriviaGame tg;
                if ((tg = StartNewGame(e))!=null)
                {
                    await client.SendMessage(e.Channel, "**Trivia game started!** It is bound to this channel. But only 1 game can run per server. \n First player to get to 10 points wins! You have 30 seconds per question.\nUse command [tq] if game was started by accident.");
                }
                else
                    await client.SendMessage(e.Channel, "Trivia game is already running on this server. The question is:\n**"+GetCurrentQuestion(e.Server.Id).Question+"**\n[tq quits trivia]\n[@NadekoBot clr clears my messages]"); // TODO type x to be reminded of the question
            };
        }

        private Func<CommandEventArgs, Task> LbFunc()
        {
            return async e =>
            {
                if (runningTrivias.ContainsKey(e.Server.Id))
                {
                    var lb = runningTrivias[e.User.Server.Id].GetLeaderboard();
                    await client.SendMessage(e.Channel, lb);
                }
                else
                    await client.SendMessage(e.Channel, "Trivia game is not running on this server."); // TODO type x to be reminded of the question
            };
        }

        private Func<CommandEventArgs, Task> RepeatFunc()
        {
            return async e =>
            {
                if (runningTrivias.ContainsKey(e.Server.Id))
                {
                    var lb = runningTrivias[e.User.Server.Id].GetLeaderboard();
                    await client.SendMessage(e.Channel, lb);
                }
                else
                    await client.SendMessage(e.Channel, "Trivia game is not running on this server."); // TODO type x to be reminded of the question
            };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("t")
                .Description("Starts a game of trivia. Questions suck and repeat a lot atm.")
                .Alias("-t")
                .Do(DoFunc());

            cgb.CreateCommand("tl")
                .Description("Shows a current trivia leaderboard.")
                .Alias("-tl")
                .Alias("tlb")
                .Alias("-tlb")
                .Do(LbFunc());

            cgb.CreateCommand("tq")
                .Description("Quits current trivia after current question.")
                .Alias("-tq")
                .Do(QuitFunc());
        }

        private Func<CommandEventArgs, Task> QuitFunc()
        {
            return async e =>
            {
                if (runningTrivias.ContainsKey(e.Server.Id) && runningTrivias[e.Server.Id].ChannelId ==e.Channel.Id)
                {
                    await client.SendMessage(e.Channel, "Trivia will stop after this question. Run [**@NadekoBot clr**] to remove this bot's messages from the channel.");
                    runningTrivias[e.Server.Id].StopGame();
                }
                else await client.SendMessage(e.Channel, "No trivias are running on this channel.");
            };
        }

        internal static void FinishGame(TriviaGame triviaGame)
        {
            runningTrivias.Remove(runningTrivias.Where(kvp => kvp.Value == triviaGame).First().Key);
        }
    }

    public class TriviaGame {

        private DiscordClient client;
        private long _serverId;
        private long _channellId;

        public long ChannelId
        {
            get
            {
                return _channellId;
            }
        }

        private Dictionary<long, int> users;

        public List<string> oldQuestions;

        public TriviaQuestion currentQuestion = null;

        private bool active = false;

        private Timer timeout;
        private bool isQuit = false;

        public TriviaGame(CommandEventArgs starter, DiscordClient client) {
            this.users = new Dictionary<long, int>();
            this.client = client;
            this._serverId = starter.Server.Id;
            this._channellId= starter.Channel.Id;

            oldQuestions = new List<string>();
            client.MessageReceived += PotentialGuess;

            timeout = new Timer();
            timeout.Interval = 30000;
            timeout.Elapsed += (s, e) => { TimeUp(); };

            LoadNextRound();
        }

        private async void PotentialGuess(object sender, MessageEventArgs e)
        {
            if (e.Server.Id != _serverId || !active)
                return;

            if (e.Message.Text.ToLower() == currentQuestion.Answer.ToLower())
            {
                active = false; //start pause between rounds
                timeout.Enabled = false;

                if (!users.ContainsKey(e.User.Id))
                    users.Add(e.User.Id, 1);
                else
                {
                    users[e.User.Id]++;
                }
                await client.SendMessage(e.Channel, Mention.User(e.User) + " Guessed it!\n The answer was: **" + currentQuestion.Answer + "**");

                if (users[e.User.Id] >= 10) {
                    await client.SendMessage(e.Channel, " We have a winner! It's " + Mention.User(e.User)+"\n"+GetLeaderboard()+"\n To start a new game type '@NadekoBot t'");
                    FinishGame();
                    return;
                }

                //if it still didnt return, we can safely start another round :D
                LoadNextRound();
            }
        }

        public void StopGame() {
            isQuit = true;
        }

        private void LoadNextRound()
        {
            Channel ch = client.GetChannel(_channellId);
            

            if(currentQuestion!=null)
                oldQuestions.Add(currentQuestion.Question);

            currentQuestion = TriviaQuestionsPool.Instance.GetRandomQuestion(oldQuestions);

            if (currentQuestion == null || isQuit)
            {
                client.SendMessage(ch, "Trivia bot stopping. :\\\n" + GetLeaderboard());
                FinishGame();
                return;
            }
            Timer t = new Timer();
            t.Interval = 2500;
            t.Enabled = true;
            t.Elapsed += async (s, ev) => {
                active = true;
                await client.SendMessage(ch, "QUESTION\n**" + currentQuestion.Question + " **");
                t.Enabled = false;
                timeout.Enabled = true;//starting countdown of the next question
            };
            return;
        }

        private async void TimeUp() {
            await client.SendMessage(client.GetChannel(_channellId), "**Time's up.**\nCorrect answer was: **" + currentQuestion.Answer+"**\n**[tq quits trivia][tl shows leaderboard][@NadekoBot clr clears my messages]**");
            LoadNextRound();
        }

        public void FinishGame() {
            isQuit = true;
            active = false;
            client.MessageReceived -= PotentialGuess;
            timeout.Enabled = false;
            timeout.Dispose();
            Trivia.FinishGame(this);
        }

        public string GetLeaderboard() {
            if (users.Count == 0)
                return "";
            

            string str = "**Leaderboard:**\n-----------\n";

            if(users.Count>1)
                users.OrderBy(kvp => kvp.Value);
            
            foreach (var KeyValuePair in users)
            {
                str += "**" + client.GetUser(client.GetServer(_serverId), KeyValuePair.Key).Name + "** has " +KeyValuePair.Value + (KeyValuePair.Value == 1 ? "point." : "points.") + Environment.NewLine;
            }
            
            return str;
        }
    }

    public class TriviaQuestion {
        public string Question;
        public string Answer;
        public TriviaQuestion(string q, string a) {
            this.Question = q;
            this.Answer = a;
        }

        public override string ToString()
        {
            return this.Question;
        }
    }

    public class TriviaQuestionsPool {
        private static TriviaQuestionsPool instance = null;

        public static TriviaQuestionsPool Instance
        {
            get {
                if (instance == null)
                    instance = new TriviaQuestionsPool();
                return instance;
            }
            private set { instance = value; }
        }

        public List<TriviaQuestion> pool;

        private Random _r;

        public TriviaQuestionsPool() {
            _r = new Random();
            pool = new List<TriviaQuestion>();
            var httpClient = new System.Net.Http.HttpClient();


            var r = httpClient.GetAsync("http://jservice.io/api/clues?category=19").Result;
            string str = r.Content.ReadAsStringAsync().Result;
            dynamic obj = JArray.Parse(str);

            foreach (var item in obj)
            {
                pool.Add(new TriviaQuestion((string)item.question,(string)item.answer));
            }
        }

        
        public TriviaQuestion GetRandomQuestion(List<string> exclude) {
            if (pool.Count == 0)
                return null;

            TriviaQuestion tq = pool[_r.Next(0, pool.Count)];
            if (exclude.Count > 0 && exclude.Count < pool.Count)
            {
                while (exclude.Contains(tq.Question))
                {
                    tq = pool[_r.Next(0, pool.Count)];
                }
            }
            return tq;
        }
    }
}
