using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using NadekoBot.Extensions;
using System.Collections;
using System.Collections.Concurrent;

//github.com/micmorris contributed quite a bit to making trivia better!
namespace NadekoBot {
    public class Trivia : DiscordCommand {
        public static float HINT_TIME_SECONDS = 6;

        public static ConcurrentDictionary<ulong, TriviaGame> runningTrivias;

        public Trivia() : base() {
            runningTrivias = new ConcurrentDictionary<ulong, TriviaGame>();
        }

        public static TriviaGame StartNewGame(CommandEventArgs e) {
            if (runningTrivias.ContainsKey(e.User.Server.Id))
                return null;

            var tg = new TriviaGame(e, NadekoBot.client);
            runningTrivias.TryAdd(e.Server.Id, tg);

            return tg;
        }

        public TriviaQuestion GetCurrentQuestion(ulong serverId) => runningTrivias[serverId].currentQuestion;

        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            TriviaGame tg;
            if ((tg = StartNewGame(e)) != null) {
                await e.Send("**Trivia game started!**\nFirst player to get to 10 points wins! You have 30 seconds per question.\nUse command `tq` if game was started by accident.**");
            } else
                await e.Send("Trivia game is already running on this server. The question is:\n**" + GetCurrentQuestion(e.Server.Id).Question + "**");
        };

        private Func<CommandEventArgs, Task> LbFunc() => async e => {
            if (runningTrivias.ContainsKey(e.Server.Id)) {
                var lb = runningTrivias[e.User.Server.Id].GetLeaderboard();
                await e.Send(lb);
            } else
                await e.Send("Trivia game is not running on this server.");
        };

        private Func<CommandEventArgs, Task> RepeatFunc() => async e => {
            if (runningTrivias.ContainsKey(e.Server.Id)) {
                var lb = runningTrivias[e.User.Server.Id].GetLeaderboard();
                await e.Send(lb);
            } else
                await e.Send("Trivia game is not running on this server.");
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("t")
                .Description("Starts a game of trivia.")
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

        private Func<CommandEventArgs, Task> QuitFunc() => async e => {
            if (runningTrivias.ContainsKey(e.Server.Id) && runningTrivias[e.Server.Id].ChannelId == e.Channel.Id) {
                await e.Send("Trivia will stop after this question.");
                runningTrivias[e.Server.Id].StopGame();
            } else await e.Send("No trivias are running on this channel.");
        };

        internal static void FinishGame(TriviaGame triviaGame) {
            TriviaGame throwaway;
            runningTrivias.TryRemove(runningTrivias.Where(kvp => kvp.Value == triviaGame).First().Key,out throwaway);
        }
    }

    public class TriviaGame {

        private DiscordClient client;
        private ulong _serverId;
        private ulong _channellId;

        public ulong ChannelId => _channellId;

        private Dictionary<ulong, int> users;

        public List<string> oldQuestions;

        public TriviaQuestion currentQuestion = null;

        private bool active = false;

        //represents the min size to judge levDistance with
        private List<Tuple<int, int>> strictness;
        private int maxStringLength;

        private Timer timeout;
        private Stopwatch stopwatch;
        private bool isQuit = false;

        private System.Threading.CancellationTokenSource hintCancelSource;

        public TriviaGame(CommandEventArgs starter, DiscordClient client) {
            this.users = new Dictionary<ulong, int>();
            this.client = client;
            this._serverId = starter.Server.Id;
            this._channellId = starter.Channel.Id;

            oldQuestions = new List<string>();
            client.MessageReceived += PotentialGuess;

            strictness = new List<Tuple<int, int>>();
            strictness.Add(new Tuple<int, int>(5, 0));
            strictness.Add(new Tuple<int, int>(6, 1));
            strictness.Add(new Tuple<int, int>(8, 2));
            strictness.Add(new Tuple<int, int>(15, 3));
            strictness.Add(new Tuple<int, int>(22, 4));
            maxStringLength = 22;

            timeout = new Timer();
            timeout.Interval = 30000;
            stopwatch = new Stopwatch();
            timeout.Elapsed += (s, e) => { TimeUp(); };

            hintCancelSource = new System.Threading.CancellationTokenSource();
            TriviaQuestionsPool.Instance.Reload();
            LoadNextRound();
        }

        private async void PotentialGuess(object sender, MessageEventArgs e) {
            if (e.Server == null || e.Channel == null) return;
            if (e.Server.Id != _serverId || !active)
                return;

            if (e.User.Id == client.CurrentUser.Id)
                return;

            if (e.Message.Text.ToLower().Equals("idfk")) {
                GetHint(e.Channel);
                return;
            }

            if (IsAnswerCorrect(e.Message.Text.ToLower(), currentQuestion.Answer.ToLower())) {
                active = false; //start pause between rounds
                timeout.Enabled = false;
                stopwatch.Stop();

                if (!users.ContainsKey(e.User.Id))
                    users.Add(e.User.Id, 1);
                else {
                    users[e.User.Id]++;
                }
                await e.Send(e.User.Mention + " Guessed it!\n The answer was: **" + currentQuestion.Answer + "**");

                if (users[e.User.Id] >= 10) {
                    await e.Send(" We have a winner! It's " + e.User.Mention + "\n" + GetLeaderboard() + "\n To start a new game type '@NadekoBot t'");
                    FinishGame();
                    return;
                }

                //if it still didnt return, we can safely start another round :D
                LoadNextRound();
            }
        }

        private bool IsAnswerCorrect(string guess, string answer) {
            if (guess.Equals(answer)) {
                return true;
            }
            guess = CleanString(guess);
            answer = CleanString(answer);
            if (guess.Equals(answer)) {
                return true;
            }

            int levDistance = guess.LevenshteinDistance(answer);
            return Judge(guess.Length, answer.Length, levDistance);
        }

        private bool Judge(int guessLength, int answerLength, int levDistance) {
            foreach (Tuple<int, int> level in strictness) {
                if (guessLength <= level.Item1 || answerLength <= level.Item1) {
                    if (levDistance <= level.Item2)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        private string CleanString(string str) {
            str = " " + str + " ";
            str = Regex.Replace(str, "\\s+", " ");
            str = Regex.Replace(str, "[^\\w\\d\\s]", "");
            //Here's where custom modification can be done
            str = Regex.Replace(str, "\\s(a|an|the|of|in|for|to|as|at|be)\\s", " ");
            //End custom mod and cleanup whitespace
            str = Regex.Replace(str, "^\\s+", "");
            str = Regex.Replace(str, "\\s+$", "");
            //Trim the really long answers
            str = str.Length <= maxStringLength ? str : str.Substring(0, maxStringLength);
            return str;
        }

        public async void GetHint(Channel ch) {
                await ch.Send(currentQuestion.Answer.Scramble());
        }

        public void StopGame() {
            isQuit = true;
        }

        private void LoadNextRound() {
            Channel ch = client.GetChannel(_channellId);


            if (currentQuestion != null)
                oldQuestions.Add(currentQuestion.Question);

            currentQuestion = TriviaQuestionsPool.Instance.GetRandomQuestion(oldQuestions);

            if (currentQuestion == null || isQuit) {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ch.Send("Trivia bot stopping. :\\\n" + GetLeaderboard());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FinishGame();
                return;
            }
            Timer t = new Timer();
            t.Interval = 2500;
            t.Enabled = true;
            t.Elapsed += async (s, ev) => {
                active = true;
                await ch.Send(currentQuestion.ToString());
                t.Enabled = false;
                timeout.Enabled = true;//starting countdown of the next question
                stopwatch.Reset();
                stopwatch.Start();
                try {
                    await Task.Delay((int)Trivia.HINT_TIME_SECONDS * 1000);
                    GetHint(ch);
                } catch (Exception) { }
            };
            return;
        }

        private async void TimeUp() {
            await client.GetChannel(_channellId)?.Send("**Time's up.**\nCorrect answer was: **" + currentQuestion.Answer + "**\n\n*[tq quits trivia][tl shows leaderboard][" + NadekoBot.botMention + " clr clears my messages]*");
            LoadNextRound();
        }

        public void FinishGame() {
            isQuit = true;
            active = false;
            client.MessageReceived -= PotentialGuess;
            timeout.Enabled = false;
            timeout.Dispose();
            stopwatch.Stop();
            stopwatch.Reset();
            Trivia.FinishGame(this);
        }

        public string GetLeaderboard() {
            if (users.Count == 0)
                return "";


            string str = "**Leaderboard:**\n-----------\n";

            if (users.Count > 1)
                users.OrderBy(kvp => kvp.Value);

            foreach (var KeyValuePair in users) {
                str += "**" + client.GetServer(_serverId).GetUser(KeyValuePair.Key).Name + "** has " + KeyValuePair.Value + (KeyValuePair.Value == 1 ? "point." : "points.") + Environment.NewLine;
            }

            return str;
        }
    }

    public class TriviaQuestion {
        public string Category;
        public string Question;
        public string Answer;
        public TriviaQuestion(string q, string a) {
            this.Question = q;
            this.Answer = a;
        }

        public override string ToString() =>
            this.Category == null ?
            "--------**Q**--------\nQuestion: **" + this.Question + "?**" :
            "--------Q--------\nCategory: " + this.Category + "\nQuestion: **" + this.Question + "?**";
    }

    public class TriviaQuestionsPool {
        private static TriviaQuestionsPool instance = null;

        public static TriviaQuestionsPool Instance {
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
            Reload();
        }


        public TriviaQuestion GetRandomQuestion(List<string> exclude) {
            if (pool.Count == 0)
                return null;

            TriviaQuestion tq = pool[_r.Next(0, pool.Count)];
            if (exclude.Count > 0 && exclude.Count < pool.Count) {
                while (exclude.Contains(tq.Question)) {
                    tq = pool[_r.Next(0, pool.Count)];
                }
            }
            return tq;
        }

        internal void Reload() {
            _r = new Random();
            pool = new List<TriviaQuestion>();
            JArray arr = JArray.Parse(File.ReadAllText("data/questions.txt"));

            foreach (var item in arr) {
                TriviaQuestion tq;
                tq = new TriviaQuestion((string)item["Question"], (string)item["Answer"]);

                if (item?["Category"] != null) {
                    tq.Category = item["Category"].ToString();
                }

                pool.Add(tq);
            }
        }
    }
}
