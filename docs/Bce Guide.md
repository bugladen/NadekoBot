# BotConfigEdit Guide

`.bce` allows you to conveniently set many of the bot-wide settings that Nadeko has, such as what the currency looks like, what people get when they use `.help`, and so on.
Below is a list of all the settings that can be set, with a quick instruction on how to use them, and their default value.

## BetflipMultiplier
The reward multiplier for correctly guessing a `.bf` (betflip) bet. Keep in mind you can't change the chance to guess the correct flip. It's always 50%.
__Default is 1.95 (in other words, if you bet 100 and guess, you will get 195 as a reward)__

## Betroll100Multiplier
The reward multiplier for rolling 100 on `.br`.
__Default is 10.0__

## Betroll67Multiplier
The reward multiplier for rolling 67 or higher on `.br`.
__Default is 2.0__

## Betroll91Multiplier
The reward multiplier for rolling 91 or higher on `.br`.
__Default is 4.0__

## CurrencyGenerationPassword
Either `true` or `false` value on whether the currency spawned with `.gc` command will have a random password associated with it in the top left corner. This helps prevent people who lurk in the chat and just spam `.pick` to gain flowers.
__Default is `false`__

## CurrencyGenerationChance
A number between 0.0 and 1.0 which represents the chance that a message sent to a channel where `.gc` is enabled will spawn currency. 0 is 0% and 1.0 is 100%
__Default is 0.02. (That's 2% chance)__

## CurrencyGenerationCooldown
A number of seconds that the bot is guaranteed not to spawn any flowers again after doing so in a channel where `.gc` is enabled. This is useful if you have a pretty high chance of the flowers spawning in the channel (for whatever stupid reason) and don't want the chat to be flooded with currency spawn messages.
__Default is 10__

## CurrencyName
Name of your currency. Mostly people aren't creative with this and just call them "Credit" or "Dollar". You can do better :^)
__Default is NadekoFlower__

## CurrencyPluralName
Plural name of your currency (if you have currency name called "dollar" then put this to "dollars". I'm not sure if this is even used anywhere in the bot anymore.
__Default is NadekoFlowers__

## CurrencySign
Emoji of your currency. You can use server emojis, though occasionally it will fail to send it correctly on other servers.
__Default is 🌸__

## DmHelpString
The string which will be sent whenever someone DMs the bot. Supports [embeds][1]. How it looks: https://puu.sh/B0BLV.png
__Default is "Use `.h` for help"__

## HelpString
The strings which will be sent whenever someone types `.h`. Supports [embeds][1]. You can also use {0} placeholder which will be replaced with your bot's client id, and {1} placeholder which will be replaced with your bot's prefix. How it looks: https://puu.sh/B0BMa.png
__Default is too long to type out (check the screenshot)__

## CurrencyDropAmount
The amount of currency which will drop when `.gc` spawn is triggered. This will be the minimum amount of currency to be spawned if CurrencyDropAmountMax is also specified.
__Default is 1__

## CurrencyDropAmountMax
Setting this value will make currency generation spawn a random amount of currency between CurrencyDropAmount and CurrencyDropAmountMax, inclusive.
__Default is 0__

## TriviaCurrencyReward
The amount of currency awarded to the winner of the trivia game.
__Default is 0__

## XpPerMessage
The amount of XP the user receives when they send a message (which is not too short).
__Default is 3__

## XpMinutesTimeout
This value represents how often the user can receive XP from sending messages.
__Default is 5__

## MinWaifuPrice
Minimum price the users can pay to claim a waifu with `.claim`.
__Default is 50__

## WaifuGiftMultiplier
The multiplier applied to the gift price before it's added to the waifu's value. For example, if a waifu is worth 100 currency, and you give her a gift which is worth 10 currency, her new value will be `(10 * WaifuGiftMultiplier) + 100`.
__Default is 1__

## MinimumTriviaWinReq
Users can't start trivia games which have smaller win requirement than specified by this setting.
__Default is 0__

## MinBet
Minimum amount of currency a user can gamble with in a single gamble. Set 0 to disable.
__Default is 0__

## MaxBet
Maximum amount of currency a user can gamble with in a single gamble. Set 0 to disable.
__Default is 0__

## OkColor
Hex of the color which will show on the left side of the bot's response when a command succesfully executes. Example: https://puu.sh/B0BXd.png
__Default is 00e584__

## ErrorColor
Hex of the color which will show on the left side of the bot's response when a command either errors, or you can't perform some action. Example: https://puu.sh/B0BXs.png
__Default is ee281f__

## ConsoleOutputType
2 values, either 'Simple' or 'Normal'. Normal is the usual amount, and the simple one shows only basic info about the executed commands in the console. Here is a comparison: https://puu.sh/B0Chn.png
__Default is 'Normal'__

## DailyCurrencyDecay
The percentage of currency all users will lose every 24 hours. The value goes between 0 and 1.0 (0 being 0% to 1.0 being 100%). This is a useful tool to control the inflation :)
__Default is 0__

## CheckForUpdates
Whether the bot will see if there are updates available. The patch notes will be sent to Bot Owner's DM. The bot checks for updates once every 8 hours. There are 3 available values:
* None: The bot will not check for updates
* Commit: This is useful for linux self-hosters - the bot will check for any new commit on the NadekoBot repository.
* Release: This is useful for windows self-hosters - the bot will check for any new releases published on the NadekoBot repository. This setting is also useful for linux self-hosters who only want to update when it's pretty safe to do so :)

__Default is Release__

## PatreonCurrencyPerCent
You need this only if you have a patreon page, and you've specified the PatreonCampaignId and PatreonAccessToken in credentials.json. This value is the amount of currency the users will get with `.clparew` for each cent they've pledged. Also make sure your patreon is set to charge upfront, otherwise people will be able to pledge, claim reward and unpledge without getting charged.
__Default is 1__

[1]: Embed%20Guide "Embed guide"