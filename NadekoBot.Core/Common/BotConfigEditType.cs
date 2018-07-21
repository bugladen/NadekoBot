namespace NadekoBot.Common
{
    public enum BotConfigEditType
    {
        /// <summary>
        /// The reward multiplier for correctly guessing a `.bf` (betflip) bet. Default is 1.95
        /// (in other words, if you bet 100 and guess, you will get 195 as a reward)
        /// Keep in mind you can't change the chance to guess the correct flip. It's always 50%.
        /// </summary>
        BetflipMultiplier,
        /// <summary>
        /// The reward multiplier for rolling 100 on `.br`
        /// Default is 10.0.
        /// </summary>
        Betroll100Multiplier,
        /// <summary>
        /// The reward multiplier for rolling 67 or higher on `.br`.
        /// Default is 2.0.
        /// </summary>
        Betroll67Multiplier,
        /// <summary>
        /// The reward multiplier for rolling 91 or higher on `.br`
        /// Default is 4.0.
        /// </summary>
        Betroll91Multiplier,
        /// <summary>
        /// Either "true" or "false" value on whether the currency spawned with `.gc` command
        /// will have a random password associated with it in the top left corner. This helps
        /// prevent people who are lurking in the chat and just spam `.pick` to gain flowers.
        /// Default is false.
        /// </summary>
        CurrencyGenerationPassword,
        /// <summary>
        /// A number between 0.0 and 1.0 which represents the chance that a message sent to
        /// the channel where `.gc` is enabled will spawn currency. 0 is 0% and 1.0 is 100%
        /// Default is 0.02. (that's 2% chance)
        /// </summary>
        CurrencyGenerationChance,
        /// <summary>
        /// A number of seconds that the bot is guaranteed not to spawn any flowers again 
        /// after doing so in the channel where `.gc` is enabled. This is useful if you have
        /// a pretty high chance of the flowers spawning in the channel (for whatever stupid reason)
        /// and don't want the chat to be flooded with currency spawn messages.
        /// Default is 10.
        /// </summary>
        CurrencyGenerationCooldown,
        /// <summary>
        /// Name of your currency. Mostly people aren't creative with this and just call them
        /// "Credit" or "Dollar". You can do better :^)
        /// Default is NadekoFlower
        /// </summary>
        CurrencyName,
        /// <summary>
        /// Plural name of your currency (if you have currency name called "dollar" then put this to "dollars".
        /// I'm not sure if this is even used anywhere in the bot anymore.
        /// Default is NadekoFlowers
        /// </summary>
        CurrencyPluralName,
        /// <summary>
        /// Emoji of your currency. You can use server emojis only if your bot is only on your own server, or 
        /// if you have nitro/partner.
        /// Default is 🌸.
        /// </summary>
        CurrencySign,
        /// <summary>
        /// The string which will be sent whenever someone DMs the bot. Supports embeds.
        /// How it looks: https://puu.sh/B0BLV.png
        /// Default is "Use `.h` for help". 
        /// </summary>
        DmHelpString,
        /// <summary>
        /// The strings which will be sent whenever someone types `.h`. Supports embeds.
        /// You can also use {0} placeholder which will be replaced with your bot's
        /// client id, and {1} placeholder which will be replaced with your bot's prefix.
        /// How it looks: https://puu.sh/B0BMa.png
        /// Default is too long to type out.
        /// </summary>
        HelpString,
        /// <summary>
        /// The amount of currency which will drop when `.gc` spawn is triggered. Default is 1.
        /// This will be the minimum amount of currency to be spawned if CurrencyDropAmountMax is also specified.
        /// </summary>
        CurrencyDropAmount,
        /// <summary>
        /// Setting this value will make currency generation spawn a random amount of currency
        /// between CurrencyDropAmount and CurrencyDropAmountMax, inclusively.
        /// Default is 0.
        /// </summary>
        CurrencyDropAmountMax,
        /// <summary>
        /// The amount of currency awarded to the winner of the trivia game.
        /// Default is 0.
        /// </summary>
        TriviaCurrencyReward,
        /// <summary>
        /// The amount of XP the user receives when they send a message (which is not too short).
        /// Default is 3.
        /// </summary>
        XpPerMessage,
        /// <summary>
        /// This value represents how often the user can receive XP from sending messages.
        /// Default is 5.
        /// </summary>
        XpMinutesTimeout,
        /// <summary>
        /// Minimum price the users can pay to claim a waifu with `.claim`.
        /// Default is 50.
        /// </summary>
        MinWaifuPrice,
        /// <summary>
        /// The multiplier applied to the gift pricebefore it's added to the waifu's value.
        /// For example, if a waifu is worth 100 currency, and you give her a gift which is worth
        /// 10 currency, her new value will be `(10 * WaifuGiftMultiplier) + 100`
        /// Default is 1.
        /// </summary>
        WaifuGiftMultiplier,
        /// <summary>
        /// Users can't start trivia games which have smaller win requirement than specified by this setting.
        /// Default is 0.
        /// </summary>
        MinimumTriviaWinReq,
        /// <summary>
        /// Minimum amount of currency a user can gamble with in a single gamble. Set 0 to disable.
        /// Default is 0.
        /// </summary>
        MinBet,
        /// <summary>
        /// Maximum amount of currency a user can gamble with in a single gamble. Set 0 to disable.
        /// Default is 0.
        /// </summary>
        MaxBet,
        /// <summary>
        /// Hex of the color which will show on the left side of the bot's response
        /// when a command succesfully executes. Example: https://puu.sh/B0BXd.png
        /// Default is 00e584.
        /// </summary>
        ErrorColor,
        /// <summary>
        /// Hex of the color which will show on the left side of the bot's response
        /// when a command either errors, or you can't perform some action. Example: https://puu.sh/B0BXs.png
        /// Default is ee281f.
        /// </summary>
        OkColor,
        /// <summary>
        /// 2 values, either 'Simple' or 'Normal'. Normal is the default type,
        /// and the simple one shows only basic info about the executed commands
        /// in the console. Here is a comparison: https://puu.sh/B0Chn.png
        /// </summary>
        ConsoleOutputType,
        /// <summary>
        /// The percentage of currency all users will lose every 24 hours. 
        /// The value goes between 0 and 1.0 (0 being 0% to 1.0 being 100%).
        /// This is a useful tool to control the inflation :)
        /// Default is 0.
        /// </summary>
        DailyCurrencyDecay,
        /// <summary>
        /// Whether the bot will see if there are updates available. The patch notes will be
        /// sent to Bot Owner's DM. The bot checks for updates once every 8 hours.
        /// There are 3 available values:
        /// None: The bot will not check for updates
        /// Commit: This is useful for linux self-hosters - the bot will check for any new commit on the NadekoBot repository.
        /// Release: This is useful for windows self-hosters - the bot will check for any new releases published on the NadekoBot repository.
        ///     This setting is also useful for linux self-hosters who only want to update when it's pretty safe to do so :)
        /// Default is Release.
        /// </summary>
        CheckForUpdates,
        /// <summary>
        /// You need this only if you have a patreon page, and you've specified the
        /// PatreonCampaignId and PatreonAccessToken in credentials.json. This value is the amount of 
        /// currency the users will get with `.clparew` for each cent they've pledged. Also make sure your 
        /// patreon is set to charge upfront, otherwise people will be able to pledge, claim reward and unpledge
        /// without getting charged.
        /// Default is 1.
        /// </summary>
        PatreonCurrencyPerCent,
    }
}
