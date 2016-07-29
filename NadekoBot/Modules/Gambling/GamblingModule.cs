using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Commands;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Linq;
using System.Text;

namespace NadekoBot.Modules.Gambling
{
    internal class GamblingModule : DiscordModule
    {
        public GamblingModule()
        {
            commands.Add(new DrawCommand(this));
            commands.Add(new FlipCoinCommand(this));
            commands.Add(new DiceRollCommand(this));
            commands.Add(new AnimalRacing(this));
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Gambling;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(com => com.Init(cgb));

                cgb.CreateCommand(Prefix + "raffle")
                    .Description($"Prints a name and ID of a random user from the online list from the (optional) role. | `{Prefix}raffle` or `{Prefix}raffle RoleName`")
                    .Parameter("role", ParameterType.Optional)
                    .Do(async e =>
                    {
                        var arg = string.IsNullOrWhiteSpace(e.GetArg("role")) ? "@everyone" : e.GetArg("role");
                        var role = e.Server.FindRoles(arg).FirstOrDefault();
                        if (role == null)
                        {
                            await e.Channel.SendMessage("💢 Role not found.").ConfigureAwait(false);
                            return;
                        }
                        var members = role.Members.Where(u => u.Status == UserStatus.Online); // only online
                        var membersArray = members as User[] ?? members.ToArray();
                        var usr = membersArray[new Random().Next(0, membersArray.Length)];
                        await e.Channel.SendMessage($"**Raffled user:** {usr.Name} (id: {usr.Id})").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "$$")
                    .Description(string.Format("Check how much {0}s a person has. (Defaults to yourself) |`{1}$$` or `{1}$$ @Someone`",
                        NadekoBot.Config.CurrencyName, Prefix))
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usr = e.Message.MentionedUsers.FirstOrDefault() ?? e.User;
                        var pts = GetUserFlowers(usr.Id);
                        var str = $"{usr.Name} has {pts} {NadekoBot.Config.CurrencySign}";
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "give")
                    .Description(string.Format("Give someone a certain amount of {0}s", NadekoBot.Config.CurrencyName)+ $"|`{Prefix}give 1 \"@SomeGuy\"`")
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("receiver", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount <= 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != NadekoBot.Client.CurrentUser.Id &&
                                                            u.Id != e.User.Id);
                        if (mentionedUser == null)
                            return;

                        var userFlowers = GetUserFlowers(e.User.Id);

                        if (userFlowers < amount)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
                            return;
                        }

                        await FlowersHandler.RemoveFlowers(e.User, "Gift", (int)amount, true).ConfigureAwait(false);
                        await FlowersHandler.AddFlowersAsync(mentionedUser, "Gift", (int)amount).ConfigureAwait(false);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully sent {amount} {NadekoBot.Config.CurrencyName}s to {mentionedUser.Mention}!").ConfigureAwait(false);

                    });

                cgb.CreateCommand(Prefix + "award")
                    .Description($"Gives someone a certain amount of flowers. **Bot Owner Only!** | `{Prefix}award 100 @person`")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("receiver", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount < 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != NadekoBot.Client.CurrentUser.Id);
                        if (mentionedUser == null)
                            return;

                        await FlowersHandler.AddFlowersAsync(mentionedUser, $"Awarded by bot owner. ({e.User.Name}/{e.User.Id})", (int)amount).ConfigureAwait(false);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully awarded {amount} {NadekoBot.Config.CurrencyName}s to {mentionedUser.Mention}!").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "take")
                    .Description($"Takes a certain amount of flowers from someone. **Bot Owner Only!** | `{Prefix}take 1 \"@someguy\"`")
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Parameter("amount", ParameterType.Required)
                    .Parameter("rektperson", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var amountStr = e.GetArg("amount")?.Trim();
                        long amount;
                        if (!long.TryParse(amountStr, out amount) || amount < 0)
                            return;

                        var mentionedUser = e.Message.MentionedUsers.FirstOrDefault(u =>
                                                            u.Id != NadekoBot.Client.CurrentUser.Id);
                        if (mentionedUser == null)
                            return;

                        await FlowersHandler.RemoveFlowers(mentionedUser, $"Taken by bot owner.({e.User.Name}/{e.User.Id})", (int)amount).ConfigureAwait(false);

                        await e.Channel.SendMessage($"{e.User.Mention} successfully took {amount} {NadekoBot.Config.CurrencyName}s from {mentionedUser.Mention}!").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "betroll")
                    .Alias(Prefix + "br")
                    .Description($"Bets a certain amount of {NadekoBot.Config.CurrencyName}s and rolls a dice. Rolling over 66 yields x2 flowers, over 90 - x3 and 100 x10. | `{Prefix}br 5`")
                    .Parameter("amount",ParameterType.Required)
                    .Do(async e =>
                    {
                        var amountstr = e.GetArg("amount").Trim();
                        int amount;

                        if (!int.TryParse(amountstr, out amount) || amount < 1)
                            return;

                        var userFlowers = GetUserFlowers(e.User.Id);

                        if (userFlowers < amount)
                        {
                            await e.Channel.SendMessage($"{e.User.Mention} You don't have enough {NadekoBot.Config.CurrencyName}s. You only have {userFlowers}{NadekoBot.Config.CurrencySign}.").ConfigureAwait(false);
                            return;
                        }

                        await FlowersHandler.RemoveFlowers(e.User, "Betroll Gamble", (int)amount, true).ConfigureAwait(false);

                        var rng = new Random().Next(0, 101);
                        var str = $"{e.User.Mention} `You rolled {rng}.` ";
                        if (rng < 67)
                        {
                            str += "Better luck next time.";
                        }
                        else if (rng < 90)
                        {
                            str += $"Congratulations! You won {amount * 2}{NadekoBot.Config.CurrencySign} for rolling above 66";
                            await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 2, true).ConfigureAwait(false);
                        }
                        else if (rng < 100)
                        {
                            str += $"Congratulations! You won {amount * 3}{NadekoBot.Config.CurrencySign} for rolling above 90.";
                            await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 3, true).ConfigureAwait(false);
                        }
                        else {
                            str += $"👑 Congratulations! You won {amount * 10}{NadekoBot.Config.CurrencySign} for rolling **100**. 👑";
                            await FlowersHandler.AddFlowersAsync(e.User, "Betroll Gamble", amount * 10, true).ConfigureAwait(false);
                        }

                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                        
                    });

                cgb.CreateCommand(Prefix + "leaderboard")
                    .Alias(Prefix + "lb")
                    .Description($"Displays bot currency leaderboard | {Prefix}lb")
                    .Do(async e =>
                    {
                        var richestTemp = DbHandler.Instance.GetTopRichest();
                        var richest = richestTemp as CurrencyState[] ?? richestTemp.ToArray();
                        if (richest.Length == 0)
                            return;
                        await e.Channel.SendMessage(
                            richest.Aggregate(new StringBuilder(
    $@"```xl
┏━━━━━━━━━━━━━━━━━━━━━┳━━━━━━━┓
┃        Id           ┃  $$$  ┃
"),
                            (cur, cs) => cur.AppendLine(
    $@"┣━━━━━━━━━━━━━━━━━━━━━╋━━━━━━━┫
┃{(e.Server.Users.Where(u => u.Id == (ulong)cs.UserId).FirstOrDefault()?.Name.TrimTo(18, true) ?? cs.UserId.ToString()),-20} ┃ {cs.Value,5} ┃")
                                    ).ToString() + "┗━━━━━━━━━━━━━━━━━━━━━┻━━━━━━━┛```").ConfigureAwait(false);
                    });
            });
        }

        public static long GetUserFlowers(ulong userId) =>
            Classes.DbHandler.Instance.GetStateByUserId((long)userId)?.Value ?? 0;
    }
}
