You can support the project on patreon: <https://patreon.com/nadekobot> or paypal: <https://paypal.me/Kwoth>

##Table of contents
- [Help](#help)
- [Administration](#administration)
- [CustomReactions](#customreactions)
- [Gambling](#gambling)
- [Games](#games)
- [Music](#music)
- [NSFW](#nsfw)
- [Permissions](#permissions)
- [Pokemon](#pokemon)
- [Searches](#searches)
- [Utility](#utility)
- [Xp](#xp)


### Administration  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.delmsgoncmd` | Toggles the automatic deletion of the user's successful command message to prevent chat flood. **Requires Administrator server permission.** | `.delmsgoncmd`
`.setrole` `.sr` | Sets a role for a given user. **Requires ManageRoles server permission.** | `.sr @User Guest`
`.removerole` `.rr` | Removes a role from a given user. **Requires ManageRoles server permission.** | `.rr @User Admin`
`.renamerole` `.renr` | Renames a role. The role you are renaming must be lower than bot's highest role. **Requires ManageRoles server permission.** | `.renr "First role" SecondRole`
`.removeallroles` `.rar` | Removes all roles from a mentioned user. **Requires ManageRoles server permission.** | `.rar @User`
`.createrole` `.cr` | Creates a role with a given name. **Requires ManageRoles server permission.** | `.cr Awesome Role`
`.rolehoist` `.rh` | Toggles whether this role is displayed in the sidebar or not. **Requires ManageRoles server permission.** | `.rh Guests` or `.rh "Space Wizards"`
`.rolecolor` `.roleclr` | Set a role's color to the hex or 0-255 rgb color value provided. **Requires ManageRoles server permission.** | `.roleclr Admin 255 200 100` or `.roleclr Admin ffba55`
`.deafen` `.deaf` | Deafens mentioned user or users. **Requires DeafenMembers server permission.** | `.deaf "@Someguy"` or `.deaf "@Someguy" "@Someguy"`
`.undeafen` `.undef` | Undeafens mentioned user or users. **Requires DeafenMembers server permission.** | `.undef "@Someguy"` or `.undef "@Someguy" "@Someguy"`
`.delvoichanl` `.dvch` | Deletes a voice channel with a given name. **Requires ManageChannels server permission.** | `.dvch VoiceChannelName`
`.creatvoichanl` `.cvch` | Creates a new voice channel with a given name. **Requires ManageChannels server permission.** | `.cvch VoiceChannelName`
`.deltxtchanl` `.dtch` | Deletes a text channel with a given name. **Requires ManageChannels server permission.** | `.dtch TextChannelName`
`.creatxtchanl` `.ctch` | Creates a new text channel with a given name. **Requires ManageChannels server permission.** | `.ctch TextChannelName`
`.settopic` `.st` | Sets a topic on the current channel. **Requires ManageChannels server permission.** | `.st My new topic`
`.setchanlname` `.schn` | Changes the name of the current channel. **Requires ManageChannels server permission.** | `.schn NewName`
`.mentionrole` `.menro` | Mentions every person from the provided role or roles (separated by a ',') on this server. **Requires MentionEveryone server permission.** | `.menro RoleName`
`.donators` | List of the lovely people who donated to keep this project alive.  | `.donators`
`.donadd` | Add a donator to the database. **Bot owner only** | `.donadd Donate Amount`
`.autoassignrole` `.aar` | Automaticaly assigns a specified role to every user who joins the server. **Requires ManageRoles server permission.** | `.aar to disable` or `.aar Role Name to enable`
`.gvc` | Toggles game voice channel feature in the voice channel you're currently in. Users who join the game voice channel will get automatically redirected to the voice channel with the name of their current game, if it exists. Can't move users to channels that the bot has no connect permission for. One per server. **Requires Administrator server permission.** | `.gvc`
`.languageset` `.langset` | Sets this server's response language. If bot's response strings have been translated to that language, bot will use that language in this server. Reset by using `default` as the locale name. Provide no arguments to see currently set language.  | `.langset de-DE ` or `.langset default`
`.langsetdefault` `.langsetd` | Sets the bot's default response language. All servers which use a default locale will use this one. Setting to `default` will use the host's current culture. Provide no arguments to see currently set language.  | `.langsetd en-US` or `.langsetd default`
`.languageslist` `.langli` | List of languages for which translation (or part of it) exist atm.  | `.langli`
`.logserver` | Enables or Disables ALL log events. If enabled, all log events will log to this channel. **Requires Administrator server permission.** **Bot owner only** | `.logserver enable` or `.logserver disable`
`.logignore` | Toggles whether the `.logserver` command ignores this channel. Useful if you have hidden admin channel and public log channel. **Requires Administrator server permission.** **Bot owner only** | `.logignore`
`.logevents` | Shows a list of all events you can subscribe to with `.log` **Requires Administrator server permission.** **Bot owner only** | `.logevents`
`.log` | Toggles logging event. Disables it if it is active anywhere on the server. Enables if it isn't active. Use `.logevents` to see a list of all events you can subscribe to. **Requires Administrator server permission.** **Bot owner only** | `.log userpresence` or `.log userbanned`
`.migratedata` | Migrate data from old bot configuration **Bot owner only** | `.migratedata`
`.setmuterole` | Sets a name of the role which will be assigned to people who should be muted. Default is nadeko-mute. **Requires ManageRoles server permission.** | `.setmuterole Silenced`
`.mute` | Mutes a mentioned user both from speaking and chatting. You can also specify time in minutes (up to 1440) for how long the user should be muted. **Requires ManageRoles server permission.** **Requires MuteMembers server permission.** | `.mute @Someone` or `.mute 30 @Someone`
`.unmute` | Unmutes a mentioned user previously muted with `.mute` command. **Requires ManageRoles server permission.** **Requires MuteMembers server permission.** | `.unmute @Someone`
`.chatmute` | Prevents a mentioned user from chatting in text channels. **Requires ManageRoles server permission.** | `.chatmute @Someone`
`.chatunmute` | Removes a mute role previously set on a mentioned user with `.chatmute` which prevented him from chatting in text channels. **Requires ManageRoles server permission.** | `.chatunmute @Someone`
`.voicemute` | Prevents a mentioned user from speaking in voice channels. **Requires MuteMembers server permission.** | `.voicemute @Someone`
`.voiceunmute` | Gives a previously voice-muted user a permission to speak. **Requires MuteMembers server permission.** | `.voiceunmute @Someguy`
`.rotateplaying` `.ropl` | Toggles rotation of playing status of the dynamic strings you previously specified. **Bot owner only** | `.ropl`
`.addplaying` `.adpl` | Adds a specified string to the list of playing strings to rotate. Supported placeholders: `%servers%`, `%users%`, `%playing%`, `%queued%`, `%time%`, `%shardid%`, `%shardcount%`, `%shardguilds%`. **Bot owner only** | `.adpl`
`.listplaying` `.lipl` | Lists all playing statuses with their corresponding number. **Bot owner only** | `.lipl`
`.removeplaying` `.rmpl` `.repl` | Removes a playing string on a given number. **Bot owner only** | `.rmpl`
`.prefix` | Sets this server's prefix for all bot commands. Provide no arguments to see the current server prefix.  | `.prefix +`
`.defprefix` | Sets bot's default prefix for all bot commands. Provide no arguments to see the current default prefix. This will not change this server's current prefix. **Bot owner only** | `.defprefix +`
`.antiraid` | Sets an anti-raid protection on the server. First argument is number of people which will trigger the protection. Second one is a time interval in which that number of people needs to join in order to trigger the protection, and third argument is punishment for those people (Kick, Ban, Mute) **Requires Administrator server permission.** | `.antiraid 5 20 Kick`
`.antispam` | Stops people from repeating same message X times in a row. You can specify to either mute, kick or ban the offenders. Max message count is 10. **Requires Administrator server permission.** | `.antispam 3 Mute` or `.antispam 4 Kick` or `.antispam 6 Ban`
`.antispamignore` | Toggles whether antispam ignores current channel. Antispam must be enabled. **Requires Administrator server permission.** | `.antispamignore`
`.antilist` `.antilst` | Shows currently enabled protection features.  | `.antilist`
`.prune` `.clear` | `.prune` removes all Nadeko's messages in the last 100 messages. `.prune X` removes last `X` number of messages from the channel (up to 100). `.prune @Someone` removes all Someone's messages in the last 100 messages. `.prune @Someone X` removes last `X` number of 'Someone's' messages in the channel.  | `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X`
`.slowmode` | Toggles slowmode. Disable by specifying no parameters. To enable, specify a number of messages each user can send, and an interval in seconds. For example 1 message every 5 seconds. **Requires ManageMessages server permission.** | `.slowmode 1 5` or `.slowmode`
`.slowmodewl` | Ignores a role or a user from the slowmode feature. **Requires ManageMessages server permission.** | `.slowmodewl SomeRole` or `.slowmodewl AdminDude`
`.adsarm` | Toggles the automatic deletion of confirmations for `.iam` and `.iamn` commands. **Requires ManageMessages server permission.** | `.adsarm`
`.asar` | Adds a role to the list of self-assignable roles. **Requires ManageRoles server permission.** | `.asar Gamer`
`.rsar` | Removes a specified role from the list of self-assignable roles. **Requires ManageRoles server permission.** | `.rsar`
`.lsar` | Lists all self-assignable roles.  | `.lsar`
`.togglexclsar` `.tesar` | Toggles whether the self-assigned roles are exclusive. (So that any person can have only one of the self assignable roles) **Requires ManageRoles server permission.** | `.tesar`
`.iam` | Adds a role to you that you choose. Role must be on a list of self-assignable roles.  | `.iam Gamer`
`.iamnot` `.iamn` | Removes a specified role from you. Role must be on a list of self-assignable roles.  | `.iamn Gamer`
`.scadd` | Adds a command to the list of commands which will be executed automatically in the current channel, in the order they were added in, by the bot when it startups up. **Bot owner only** | `.scadd .stats`
`.sclist` | Lists all startup commands in the order they will be executed in. **Bot owner only** | `.sclist`
`.wait` | Used only as a startup command. Waits a certain number of miliseconds before continuing the execution of the following startup commands. **Bot owner only** | `.wait 3000`
`.scrm` | Removes a startup command with the provided command text. **Bot owner only** | `.scrm .stats`
`.scclr` | Removes all startup commands. **Bot owner only** | `.scclr`
`.fwmsgs` | Toggles forwarding of non-command messages sent to bot's DM to the bot owners **Bot owner only** | `.fwmsgs`
`.fwtoall` | Toggles whether messages will be forwarded to all bot owners or only to the first one specified in the credentials.json file **Bot owner only** | `.fwtoall`
`.leave` | Makes Nadeko leave the server. Either server name or server ID is required. **Bot owner only** | `.leave 123123123331`
`.die` | Shuts the bot down. **Bot owner only** | `.die`
`.setname` `.newnm` | Gives the bot a new name. **Bot owner only** | `.newnm BotName`
`.setnick` | Changes the nickname of the bot on this server. You can also target other users to change their nickname. **Requires ManageNicknames server permission.** | `.setnick BotNickname` or `.setnick @SomeUser New Nickname`
`.setstatus` | Sets the bot's status. (Online/Idle/Dnd/Invisible) **Bot owner only** | `.setstatus Idle`
`.setavatar` `.setav` | Sets a new avatar image for the NadekoBot. Argument is a direct link to an image. **Bot owner only** | `.setav http://i.imgur.com/xTG3a1I.jpg`
`.setgame` | Sets the bots game. **Bot owner only** | `.setgame with snakes`
`.setstream` | Sets the bots stream. First argument is the twitch link, second argument is stream name. **Bot owner only** | `.setstream TWITCHLINK Hello`
`.send` | Sends a message to someone on a different server through the bot.  Separate server and channel/user ids with `|` and prefix the channel id with `c:` and the user id with `u:`. **Bot owner only** | `.send serverid|c:channelid message` or `.send serverid|u:userid message`
`.reloadimages` | Reloads images bot is using. Safe to use even when bot is being used heavily. **Bot owner only** | `.reloadimages`
`.greetdel` `.grdel` | Sets the time it takes (in seconds) for greet messages to be auto-deleted. Set it to 0 to disable automatic deletion. **Requires ManageServer server permission.** | `.greetdel 0` or `.greetdel 30`
`.greet` | Toggles anouncements on the current channel when someone joins the server. **Requires ManageServer server permission.** | `.greet`
`.greetmsg` | Sets a new join announcement message which will be shown in the server's channel. Type `%user%` if you want to mention the new member. Using it with no message will show the current greet message. You can use embed json from <http://nadekobot.me/embedbuilder/> instead of a regular text, if you want the message to be embedded. **Requires ManageServer server permission.** | `.greetmsg Welcome, %user%.`
`.greetdm` | Toggles whether the greet messages will be sent in a DM (This is separate from greet - you can have both, any or neither enabled). **Requires ManageServer server permission.** | `.greetdm`
`.greetdmmsg` | Sets a new join announcement message which will be sent to the user who joined. Type `%user%` if you want to mention the new member. Using it with no message will show the current DM greet message. You can use embed json from <http://nadekobot.me/embedbuilder/> instead of a regular text, if you want the message to be embedded. **Requires ManageServer server permission.** | `.greetdmmsg Welcome to the server, %user%`
`.bye` | Toggles anouncements on the current channel when someone leaves the server. **Requires ManageServer server permission.** | `.bye`
`.byemsg` | Sets a new leave announcement message. Type `%user%` if you want to show the name the user who left. Type `%id%` to show id. Using this command with no message will show the current bye message. You can use embed json from <http://nadekobot.me/embedbuilder/> instead of a regular text, if you want the message to be embedded. **Requires ManageServer server permission.** | `.byemsg %user% has left.`
`.byedel` | Sets the time it takes (in seconds) for bye messages to be auto-deleted. Set it to `0` to disable automatic deletion. **Requires ManageServer server permission.** | `.byedel 0` or `.byedel 30`
`.timezones` | Lists all timezones available on the system to be used with `.timezone`.  | `.timezones`
`.timezone` | Sets this guilds timezone. This affects bot's time output in this server (logs, etc..)  | `.timezone` or `.timezone GMT Standard Time`
`.warn` | Warns a user. **Requires BanMembers server permission.** | `.warn @b1nzy Very rude person`
`.warnlog` | See a list of warnings of a certain user. **Requires BanMembers server permission.** | `.warnlog @b1nzy`
`.warnlogall` | See a list of all warnings on the server. 15 users per page. **Requires BanMembers server permission.** | `.warnlogall` or `.warnlogall 2`
`.warnclear` `.warnc` | Clears all warnings from a certain user. **Requires BanMembers server permission.** | `.warnclear @PoorDude`
`.warnpunish` `.warnp` | Sets a punishment for a certain number of warnings. Provide no punishment to remove. **Requires BanMembers server permission.** | `.warnpunish 5 Ban` or `.warnpunish 3`
`.warnpunishlist` `.warnpl` | Lists punishments for warnings.  | `.warnpunishlist`
`.ban` `.b` | Bans a user by ID or name with an optional message. **Requires BanMembers server permission.** | `.b "@some Guy" Your behaviour is toxic.`
`.unban` | Unbans a user with the provided user#discrim or id. **Requires BanMembers server permission.** | `.unban kwoth#1234` or `.unban 123123123`
`.softban` `.sb` | Bans and then unbans a user by ID or name with an optional message. **Requires KickMembers server permission.** **Requires ManageMessages server permission.** | `.sb "@some Guy" Your behaviour is toxic.`
`.kick` `.k` | Kicks a mentioned user. **Requires KickMembers server permission.** | `.k "@some Guy" Your behaviour is toxic.`
`.vcrole` | Sets or resets a role which will be given to users who join the voice channel you're in when you run this command. Provide no role name to disable. You must be in a voice channel to run this command. **Requires ManageRoles server permission.** **Requires ManageChannels server permission.** | `.vcrole SomeRole` or `.vcrole`
`.vcrolelist` | Shows a list of currently set voice channel roles.  | `.vcrolelist`
`.voice+text` `.v+t` | Creates a text channel for each voice channel only users in that voice channel can see. If you are server owner, keep in mind you will see them all the time regardless. **Requires ManageRoles server permission.** **Requires ManageChannels server permission.** | `.v+t`
`.cleanvplust` `.cv+t` | Deletes all text channels ending in `-voice` for which voicechannels are not found. Use at your own risk. **Requires ManageChannels server permission.** **Requires ManageRoles server permission.** | `.cleanv+t`

###### [Back to ToC](#table-of-contents)

### CustomReactions  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.addcustreact` `.acr` | Add a custom reaction with a trigger and a response. Running this command in server requires the Administration permission. Running this command in DM is Bot Owner only and adds a new global custom reaction. Guide here: <http://nadekobot.readthedocs.io/en/latest/Custom%20Reactions/>  | `.acr "hello" Hi there %user%`
`.listcustreact` `.lcr` | Lists global or server custom reactions (20 commands per page). Running the command in DM will list global custom reactions, while running it in server will list that server's custom reactions. Specifying `all` argument instead of the number will DM you a text file with a list of all custom reactions.  | `.lcr 1` or `.lcr all`
`.listcustreactg` `.lcrg` | Lists global or server custom reactions (20 commands per page) grouped by trigger, and show a number of responses for each. Running the command in DM will list global custom reactions, while running it in server will list that server's custom reactions.  | `.lcrg 1`
`.showcustreact` `.scr` | Shows a custom reaction's response on a given ID.  | `.scr 1`
`.delcustreact` `.dcr` | Deletes a custom reaction on a specific index. If ran in DM, it is bot owner only and deletes a global custom reaction. If ran in a server, it requires Administration privileges and removes server custom reaction.  | `.dcr 5`
`.crca` | Toggles whether the custom reaction will trigger if the triggering message contains the keyword (instead of only starting with it).  | `.crca 44`
`.crdm` | Toggles whether the response message of the custom reaction will be sent as a direct message.  | `.crdm 44`
`.crad` | Toggles whether the message triggering the custom reaction will be automatically deleted.  | `.crad 59`
`.crstatsclear` | Resets the counters on `.crstats`. You can specify a trigger to clear stats only for that trigger. **Bot owner only** | `.crstatsclear` or `.crstatsclear rng`
`.crstats` | Shows a list of custom reactions and the number of times they have been executed. Paginated with 10 per page. Use `.crstatsclear` to reset the counters.  | `.crstats` or `.crstats 3`

###### [Back to ToC](#table-of-contents)

### Gambling  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.raffle` | Prints a name and ID of a random user from the online list from the (optional) role.  | `.raffle` or `.raffle RoleName`
`.$` `.currency` `.$$` `.$$$` `.cash` `.cur` | Check how much currency a person has. (Defaults to yourself)  | `.$` or `.$ @SomeGuy`
`.give` | Give someone a certain amount of currency.  | `.give 1 @SomeGuy`
`.award` | Awards someone a certain amount of currency.  You can also specify a role name to award currency to all users in a role. **Bot owner only** | `.award 100 @person` or `.award 5 Role Of Gamblers`
`.take` | Takes a certain amount of currency from someone. **Bot owner only** | `.take 1 @SomeGuy`
`.betroll` `.br` | Bets a certain amount of currency and rolls a dice. Rolling over 66 yields x2 of your currency, over 90 - x4 and 100 x10.  | `.br 5`
`.leaderboard` `.lb` | Displays the bot's currency leaderboard.  | `.lb`
`.race` | Starts a new animal race.  | `.race`
`.joinrace` `.jr` | Joins a new race. You can specify an amount of currency for betting (optional). You will get YourBet*(participants-1) back if you win.  | `.jr` or `.jr 5`
`.startevent` | Starts one of the events seen on public nadeko. `reaction` and `sneakygamestatus` are the only 2 available now. **Bot owner only** | `.startevent reaction`
`.roll` | Rolls 0-100. If you supply a number `X` it rolls up to 30 normal dice. If you split 2 numbers with letter `d` (`xdy`) it will roll `X` dice from 1 to `y`. `Y` can be a letter 'F' if you want to roll fate dice instead of dnd.  | `.roll` or `.roll 7` or `.roll 3d5` or `.roll 5dF`
`.rolluo` | Rolls `X` normal dice (up to 30) unordered. If you split 2 numbers with letter `d` (`xdy`) it will roll `X` dice from 1 to `y`.  | `.rolluo` or `.rolluo 7` or `.rolluo 3d5`
`.nroll` | Rolls in a given range.  | `.nroll 5` (rolls 0-5)` or `.nroll 5-15`
`.draw` | Draws a card from this server's deck. You can draw up to 10 cards by supplying a number of cards to draw.  | `.draw` or `.draw 5`
`.drawnew` | Draws a card from the NEW deck of cards. You can draw up to 10 cards by supplying a number of cards to draw.  | `.drawnew` or `.drawnew 5`
`.deckshuffle` `.dsh` | Reshuffles all cards back into the deck.  | `.dsh`
`.flip` | Flips coin(s) - heads or tails, and shows an image.  | `.flip` or `.flip 3`
`.betflip` `.bf` | Bet to guess will the result be heads or tails. Guessing awards you 1.95x the currency you've bet (rounded up). Multiplier can be changed by the bot owner.  | `.bf 5 heads` or `.bf 3 t`
`.shop` | Lists this server's administrators' shop. Paginated.  | `.shop` or `.shop 2`
`.buy` | Buys an item from the shop on a given index. If buying items, make sure that the bot can DM you.  | `.buy 2`
`.shopadd` | Adds an item to the shop by specifying type price and name. Available types are role and list. **Requires Administrator server permission.** | `.shopadd role 1000 Rich`
`.shoplistadd` | Adds an item to the list of items for sale in the shop entry given the index. You usually want to run this command in the secret channel, so that the unique items are not leaked. **Requires Administrator server permission.** | `.shoplistadd 1 Uni-que-Steam-Key`
`.shoprem` `.shoprm` | Removes an item from the shop by its ID. **Requires Administrator server permission.** | `.shoprm 1`
`.slotstats` | Shows the total stats of the slot command for this bot's session. **Bot owner only** | `.slotstats`
`.slottest` | Tests to see how much slots payout for X number of plays. **Bot owner only** | `.slottest 1000`
`.slot` | Play Nadeko slots. Max bet is 9999. 1.5 second cooldown per user.  | `.slot 5`
`.claimwaifu` `.claim` | Claim a waifu for yourself by spending currency.  You must spend at least 10% more than her current value unless she set `.affinity` towards you.  | `.claim 50 @Himesama`
`.divorce` | Releases your claim on a specific waifu. You will get some of the money you've spent back unless that waifu has an affinity towards you. 6 hours cooldown.  | `.divorce @CheatingSloot`
`.affinity` | Sets your affinity towards someone you want to be claimed by. Setting affinity will reduce their `.claim` on you by 20%. You can leave second argument empty to clear your affinity. 30 minutes cooldown.  | `.affinity @MyHusband` or `.affinity`
`.waifus` `.waifulb` | Shows top 9 waifus. You can specify another page to show other waifus.  | `.waifus` or `.waifulb 3`
`.waifuinfo` `.waifustats` | Shows waifu stats for a target person. Defaults to you if no user is provided.  | `.waifuinfo @MyCrush` or `.waifuinfo`
`.waifugift` `.gift` `.gifts` | Gift an item to someone. This will increase their waifu value by 50% of the gifted item's value if they don't have affinity set towards you, or 100% if they do. Provide no arguments to see a list of items that you can gift.  | `.gifts` or `.gift Rose @Himesama`
`.wheeloffortune` `.wheel` | Bets a certain amount of currency on the wheel of fortune. Wheel can stop on one of many different multipliers. Won amount is rounded down to the nearest whole number.  | `.wheel 10`

###### [Back to ToC](#table-of-contents)

### Games  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.choose` | Chooses a thing from a list of things  | `.choose Get up;Sleep;Sleep more`
`.8ball` | Ask the 8ball a yes/no question.  | `.8ball Is b1nzy a nice guy?`
`.rps` | Play a game of Rocket-Paperclip-Scissors with Nadeko.  | `.rps scissors`
`.rategirl` | Use the universal hot-crazy wife zone matrix to determine the girl's worth. It is everything young men need to know about women. At any moment in time, any woman you have previously located on this chart can vanish from that location and appear anywhere else on the chart.  | `.rategirl @SomeGurl`
`.linux` | Prints a customizable Linux interjection  | `.linux Spyware Windows`
`.leet` | Converts a text to leetspeak with 6 (1-6) severity levels  | `.leet 3 Hello`
`.acrophobia` `.acro` | Starts an Acrophobia game. Second argument is optional round length in seconds. (default is 60)  | `.acro` or `.acro 30`
`.cleverbot` | Toggles cleverbot session. When enabled, the bot will reply to messages starting with bot mention in the server. Custom reactions starting with %mention% won't work if cleverbot is enabled. **Requires ManageMessages server permission.** | `.cleverbot`
`.connect4` `.con4` | Creates or joins an existing connect4 game. 2 players are required for the game. Objective of the game is to get 4 of your pieces next to each other in a vertical, horizontal or diagonal line.  | `.connect4`
`.hangmanlist` | Shows a list of hangman term types.  | `.hangmanlist`
`.hangman` | Starts a game of hangman in the channel. Use `.hangmanlist` to see a list of available term types. Defaults to 'all'.  | `.hangman` or `.hangman movies`
`.hangmanstop` | Stops the active hangman game on this channel if it exists.  | `.hangmanstop`
`.nunchi` | Creates or joins an existing nunchi game. Users have to count up by 1 from the starting number shown by the bot. If someone makes a mistake (types an incorrent number, or repeats the same number) they are out of the game and a new round starts without them.  Minimum 3 users required.  | `.nunchi`
`.pick` | Picks the currency planted in this channel. 60 seconds cooldown.  | `.pick`
`.plant` | Spend an amount of currency to plant it in this channel. Default is 1. (If bot is restarted or crashes, the currency will be lost)  | `.plant` or `.plant 5`
`.gencurrency` `.gc` | Toggles currency generation on this channel. Every posted message will have chance to spawn currency. Chance is specified by the Bot Owner. (default is 2%) **Requires ManageMessages server permission.** | `.gc`
`.poll` `.ppoll` | Creates a public poll which requires users to type a number of the voting option in the channel command is ran in. **Requires ManageMessages server permission.** | `.ppoll Question?;Answer1;Answ 2;A_3`
`.pollstats` | Shows the poll results without stopping the poll on this server. **Requires ManageMessages server permission.** | `.pollstats`
`.pollend` | Stops active poll on this server and prints the results in this channel. **Requires ManageMessages server permission.** | `.pollend`
`.typestart` | Starts a typing contest.  | `.typestart`
`.typestop` | Stops a typing contest on the current channel.  | `.typestop`
`.typeadd` | Adds a new article to the typing contest. **Bot owner only** | `.typeadd wordswords`
`.typelist` | Lists added typing articles with their IDs. 15 per page.  | `.typelist` or `.typelist 3`
`.typedel` | Deletes a typing article given the ID. **Bot owner only** | `.typedel 3`
`.tictactoe` `.ttt` | Starts a game of tic tac toe. Another user must run the command in the same channel in order to accept the challenge. Use numbers 1-9 to play. 15 seconds per move.  | `.ttt`
`.trivia` `.t` | Starts a game of trivia. You can add `nohint` to prevent hints. First player to get to 10 points wins by default. You can specify a different number. 30 seconds per question.  | `.t` or `.t 5 nohint`
`.tl` | Shows a current trivia leaderboard.  | `.tl`
`.tq` | Quits current trivia after current question.  | `.tq`

###### [Back to ToC](#table-of-contents)

### Help  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.modules` `.mdls` | Lists all bot modules.  | `.modules`
`.commands` `.cmds` | List all of the bot's commands from a certain module. You can either specify the full name or only the first few letters of the module name.  | `.commands Administration` or `.cmds Admin`
`.help` `.h` | Either shows a help for a single command, or DMs you help link if no arguments are specified.  | `.h .cmds` or `.h`
`.hgit` | Generates the commandlist.md file. **Bot owner only** | `.hgit`
`.readme` `.guide` | Sends a readme and a guide links to the channel.  | `.readme` or `.guide`
`.donate` | Instructions for helping the project financially.  | `.donate`

###### [Back to ToC](#table-of-contents)

### Music  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.play` `.start` | If no arguments are specified, acts as `.next 1` command. If you specify a song number, it will jump to that song. If you specify a search query, acts as a `.q` command  | `.play` or `.play 5` or `.play Dream Of Venice`
`.queue` `.q` `.yq` | Queue a song using keywords or a link. Bot will join your voice channel. **You must be in a voice channel**.  | `.q Dream Of Venice`
`.queuenext` `.qn` | Works the same as `.queue` command, except it enqueues the new song after the current one. **You must be in a voice channel**.  | `.qn Dream Of Venice`
`.queuesearch` `.qs` `.yqs` | Search for top 5 youtube song result using keywords, and type the index of the song to play that song. Bot will join your voice channel. **You must be in a voice channel**.  | `.qs Dream Of Venice`
`.listqueue` `.lq` | Lists 10 currently queued songs per page. Default page is 1.  | `.lq` or `.lq 2`
`.next` `.n` | Goes to the next song in the queue. You have to be in the same voice channel as the bot. You can skip multiple songs, but in that case songs will not be requeued if .rcs or .rpl is enabled.  | `.n` or `.n 5`
`.stop` `.s` | Stops the music and preserves the current song index. Stays in the channel.  | `.s`
`.destroy` `.d` | Completely stops the music and unbinds the bot from the channel. (may cause weird behaviour)  | `.d`
`.pause` `.p` | Pauses or Unpauses the song.  | `.p`
`.volume` `.vol` | Sets the music playback volume (0-100%)  | `.vol 50`
`.defvol` `.dv` | Sets the default music volume when music playback is started (0-100). Persists through restarts.  | `.dv 80`
`.songremove` `.srm` | Remove a song by its # in the queue, or 'all' to remove all songs from the queue and reset the song index.  | `.srm 5`
`.playlists` `.pls` | Lists all playlists. Paginated, 20 per page. Default page is 0.  | `.pls 1`
`.deleteplaylist` `.delpls` | Deletes a saved playlist. Works only if you made it or if you are the bot owner.  | `.delpls animu-5`
`.save` | Saves a playlist under a certain name. Playlist name must be no longer than 20 characters and must not contain dashes.  | `.save classical1`
`.load` | Loads a saved playlist using its ID. Use `.pls` to list all saved playlists and `.save` to save new ones.  | `.load 5`
`.fairplay` `.fp` | Toggles fairplay. While enabled, the bot will prioritize songs from users who didn't have their song recently played instead of the song's position in the queue.  | `.fp`
`.songautodelete` `.sad` | Toggles whether the song should be automatically removed from the music queue when it finishes playing.  | `.sad`
`.soundcloudqueue` `.sq` | Queue a soundcloud song using keywords. Bot will join your voice channel. **You must be in a voice channel**.  | `.sq Dream Of Venice`
`.soundcloudpl` `.scpl` | Queue a Soundcloud playlist using a link.  | `.scpl soundcloudseturl`
`.nowplaying` `.np` | Shows the song that the bot is currently playing.  | `.np`
`.shuffle` `.sh` `.plsh` | Shuffles the current playlist.  | `.plsh`
`.playlist` `.pl` | Queues up to 500 songs from a youtube playlist specified by a link, or keywords.  | `.pl <youtube_playlist_link>`
`.radio` `.ra` | Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf (Usage Video: <https://streamable.com/al54>)  | `.ra radio link here`
`.local` `.lo` | Queues a local file by specifying a full path. **Bot owner only** | `.lo C:/music/mysong.mp3`
`.localplaylst` `.lopl` | Queues all songs from a directory. **Bot owner only** | `.lopl C:/music/classical`
`.move` `.mv` | Moves the bot to your voice channel. (works only if music is already playing)  | `.mv`
`.movesong` `.ms` | Moves a song from one position to another.  | `.ms 5>3`
`.setmaxqueue` `.smq` | Sets a maximum queue size. Supply 0 or no argument to have no limit.  | `.smq 50` or `.smq`
`.setmaxplaytime` `.smp` | Sets a maximum number of seconds (>14) a song can run before being skipped automatically. Set 0 to have no limit.  | `.smp 0` or `.smp 270`
`.reptcursong` `.rcs` | Toggles repeat of current song.  | `.rcs`
`.rpeatplaylst` `.rpl` | Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue).  | `.rpl`
`.autoplay` `.ap` | Toggles autoplay - When the song is finished, automatically queue a related Youtube song. (Works only for Youtube songs and when queue is empty)  | `.ap`
`.setmusicchannel` `.smch` | Sets the current channel as the default music output channel. This will output playing, finished, paused and removed songs to that channel instead of the channel where the first song was queued in. **Requires ManageMessages server permission.** | `.smch`

###### [Back to ToC](#table-of-contents)

### NSFW  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.autohentai` | Posts a hentai every X seconds with a random tag from the provided tags. Use `|` to separate tags. 20 seconds minimum. Provide no arguments to disable. **Requires ManageMessages channel permission.** | `.autohentai 30 yuri|tail|long_hair` or `.autohentai`
`.autoboobs` | Posts a boobs every X seconds. 20 seconds minimum. Provide no arguments to disable. **Requires ManageMessages channel permission.** | `.autoboobs 30` or `.autoboobs`
`.autobutts` | Posts a butts every X seconds. 20 seconds minimum. Provide no arguments to disable. **Requires ManageMessages channel permission.** | `.autobutts 30` or `.autobutts`
`.hentai` | Shows a hentai image from a random website (gelbooru or danbooru or konachan or atfbooru or yandere) with a given tag. Tag is optional but preferred. Only 1 tag allowed.  | `.hentai yuri`
`.hentaibomb` | Shows a total 5 images (from gelbooru, danbooru, konachan, yandere and atfbooru). Tag is optional but preferred.  | `.hentaibomb yuri`
`.yandere` | Shows a random image from yandere with a given tag. Tag is optional but preferred. (multiple tags are appended with +)  | `.yandere tag1+tag2`
`.konachan` | Shows a random hentai image from konachan with a given tag. Tag is optional but preferred.  | `.konachan yuri`
`.e621` | Shows a random hentai image from e621.net with a given tag. Tag is optional but preferred. Use spaces for multiple tags.  | `.e621 yuri kissing`
`.rule34` | Shows a random image from rule34.xx with a given tag. Tag is optional but preferred. (multiple tags are appended with +)  | `.rule34 yuri+kissing`
`.danbooru` | Shows a random hentai image from danbooru with a given tag. Tag is optional but preferred. (multiple tags are appended with +)  | `.danbooru yuri+kissing`
`.gelbooru` | Shows a random hentai image from gelbooru with a given tag. Tag is optional but preferred. (multiple tags are appended with +)  | `.gelbooru yuri+kissing`
`.boobs` | Real adult content.  | `.boobs`
`.butts` `.ass` `.butt` | Real adult content.  | `.butts` or `.ass`
`.nsfwtagbl` `.nsfwtbl` | Toggles whether the tag is blacklisted or not in nsfw searches. Provide no parameters to see the list of blacklisted tags.  | `.nsfwtbl poop`
`.nsfwcc` | Clears nsfw cache. **Bot owner only** | `.nsfwcc`

###### [Back to ToC](#table-of-contents)

### Permissions  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.verbose` `.v` | Sets whether to show when a command/module is blocked.  | `.verbose true`
`.permrole` `.pr` | Sets a role which can change permissions. Supply no parameters to see the current one. Default is 'Nadeko'.  | `.pr role`
`.listperms` `.lp` | Lists whole permission chain with their indexes. You can specify an optional page number if there are a lot of permissions.  | `.lp` or `.lp 3`
`.removeperm` `.rp` | Removes a permission from a given position in the Permissions list.  | `.rp 1`
`.moveperm` `.mp` | Moves permission from one position to another in the Permissions list.  | `.mp 2 4`
`.srvrcmd` `.sc` | Sets a command's permission at the server level.  | `.sc "command name" disable`
`.srvrmdl` `.sm` | Sets a module's permission at the server level.  | `.sm ModuleName enable`
`.usrcmd` `.uc` | Sets a command's permission at the user level.  | `.uc "command name" enable SomeUsername`
`.usrmdl` `.um` | Sets a module's permission at the user level.  | `.um ModuleName enable SomeUsername`
`.rolecmd` `.rc` | Sets a command's permission at the role level.  | `.rc "command name" disable MyRole`
`.rolemdl` `.rm` | Sets a module's permission at the role level.  | `.rm ModuleName enable MyRole`
`.chnlcmd` `.cc` | Sets a command's permission at the channel level.  | `.cc "command name" enable SomeChannel`
`.chnlmdl` `.cm` | Sets a module's permission at the channel level.  | `.cm ModuleName enable SomeChannel`
`.allchnlmdls` `.acm` | Enable or disable all modules in a specified channel.  | `.acm enable #SomeChannel`
`.allrolemdls` `.arm` | Enable or disable all modules for a specific role.  | `.arm [enable/disable] MyRole`
`.allusrmdls` `.aum` | Enable or disable all modules for a specific user.  | `.aum enable @someone`
`.allsrvrmdls` `.asm` | Enable or disable all modules for your server.  | `.asm [enable/disable]`
`.ubl` | Either [add]s or [rem]oves a user specified by a Mention or an ID from a blacklist. **Bot owner only** | `.ubl add @SomeUser` or `.ubl rem 12312312313`
`.cbl` | Either [add]s or [rem]oves a channel specified by an ID from a blacklist. **Bot owner only** | `.cbl rem 12312312312`
`.sbl` | Either [add]s or [rem]oves a server specified by a Name or an ID from a blacklist. **Bot owner only** | `.sbl add 12312321312` or `.sbl rem SomeTrashServer`
`.cmdcooldown` `.cmdcd` | Sets a cooldown per user for a command. Set it to 0 to remove the cooldown.  | `.cmdcd "some cmd" 5`
`.allcmdcooldowns` `.acmdcds` | Shows a list of all commands and their respective cooldowns.  | `.acmdcds`
`.srvrfilterinv` `.sfi` | Toggles automatic deletion of invites posted in the server. Does not affect the Bot Owner.  | `.sfi`
`.chnlfilterinv` `.cfi` | Toggles automatic deletion of invites posted in the channel. Does not negate the `.srvrfilterinv` enabled setting. Does not affect the Bot Owner.  | `.cfi`
`.srvrfilterwords` `.sfw` | Toggles automatic deletion of messages containing filtered words on the server. Does not affect the Bot Owner.  | `.sfw`
`.chnlfilterwords` `.cfw` | Toggles automatic deletion of messages containing filtered words on the channel. Does not negate the `.srvrfilterwords` enabled setting. Does not affect the Bot Owner.  | `.cfw`
`.fw` | Adds or removes (if it exists) a word from the list of filtered words. Use`.sfw` or `.cfw` to toggle filtering.  | `.fw poop`
`.lstfilterwords` `.lfw` | Shows a list of filtered words.  | `.lfw`
`.listglobalperms` `.lgp` | Lists global permissions set by the bot owner. **Bot owner only** | `.lgp`
`.globalmodule` `.gmod` | Toggles whether a module can be used on any server. **Bot owner only** | `.gmod nsfw`
`.globalcommand` `.gcmd` | Toggles whether a command can be used on any server. **Bot owner only** | `.gcmd .stats`
`.resetperms` | Resets the bot's permissions module on this server to the default value. **Requires Administrator server permission.** | `.resetperms`
`.resetglobalperms` | Resets global permissions set by bot owner. **Bot owner only** | `.resetglobalperms`

###### [Back to ToC](#table-of-contents)

### Pokemon  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.attack` | Attacks a target with the given move. Use `.movelist` to see a list of moves your type can use.  | `.attack "vine whip" @someguy`
`.movelist` `.ml` | Lists the moves you are able to use  | `.ml`
`.heal` | Heals someone. Revives those who fainted. Costs one Currency.   | `.heal @someone`
`.type` | Get the poketype of the target.  | `.type @someone`
`.settype` | Set your poketype. Costs one Currency. Provide no arguments to see a list of available types.  | `.settype fire` or `.settype`

###### [Back to ToC](#table-of-contents)

### Searches  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.lolban` | Shows top banned champions ordered by ban rate.  | `.lolban`
`.weather` `.we` | Shows weather data for a specified city. You can also specify a country after a comma.  | `.we Moscow, RU`
`.time` | Shows the current time and timezone in the specified location.  | `.time London, UK`
`.youtube` `.yt` | Searches youtubes and shows the first result  | `.yt query`
`.imdb` `.omdb` | Queries omdb for movies or series, show first result.  | `.imdb Batman vs Superman`
`.randomcat` `.meow` | Shows a random cat image.  | `.meow`
`.randomdog` `.woof` | Shows a random dog image.  | `.woof`
`.image` `.img` | Pulls the first image found using a search parameter. Use `.rimg` for different results.  | `.img cute kitten`
`.randomimage` `.rimg` | Pulls a random image using a search parameter.  | `.rimg cute kitten`
`.lmgtfy` | Google something for an idiot.  | `.lmgtfy query`
`.shorten` | Attempts to shorten an URL, if it fails, returns the input URL.  | `.shorten https://google.com`
`.google` `.g` | Get a Google search link for some terms.  | `.google query`
`.magicthegathering` `.mtg` | Searches for a Magic The Gathering card.  | `.magicthegathering about face` or `.mtg about face`
`.hearthstone` `.hs` | Searches for a Hearthstone card and shows its image. Takes a while to complete.  | `.hs Ysera`
`.yodify` `.yoda` | Translates your normal sentences into Yoda styled sentences!  | `.yoda my feelings hurt`
`.urbandict` `.ud` | Searches Urban Dictionary for a word.  | `.ud Pineapple`
`.define` `.def` | Finds a definition of a word.  | `.def heresy`
`.#` | Searches Tagdef.com for a hashtag.  | `.# ff`
`.catfact` | Shows a random catfact from <http://catfacts-api.appspot.com/api/facts>  | `.catfact`
`.revav` | Returns a Google reverse image search for someone's avatar.  | `.revav @SomeGuy`
`.revimg` | Returns a Google reverse image search for an image from a link.  | `.revimg Image link`
`.safebooru` | Shows a random image from safebooru with a given tag. Tag is optional but preferred. (multiple tags are appended with +)  | `.safebooru yuri+kissing`
`.wikipedia` `.wiki` | Gives you back a wikipedia link  | `.wiki query`
`.color` | Shows you what color corresponds to that hex.  | `.color 00ff00`
`.videocall` | Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message.  | `.videocall "@the First" "@Xyz"`
`.avatar` `.av` | Shows a mentioned person's avatar.  | `.av @SomeGuy`
`.wikia` | Gives you back a wikia link  | `.wikia mtg Vigilance` or `.wikia mlp Dashy`
`.mal` | Shows basic info from a MyAnimeList profile.  | `.mal straysocks`
`.anime` `.ani` `.aq` | Queries anilist for an anime and shows the first result.  | `.ani aquarion evol`
`.manga` `.mang` `.mq` | Queries anilist for a manga and shows the first result.  | `.mq Shingeki no kyojin`
`.feed` `.feedadd` | Subscribes to a feed. Bot will post an update up to once every 10 seconds. You can have up to 10 feeds on one server. All feeds must have unique URLs. **Requires ManageMessages server permission.** | `.feed https://www.rt.com/rss/`
`.feedremove` `.feedrm` `.feeddel` | Stops tracking a feed on the given index. Use `.feeds` command to see a list of feeds and their indexes. **Requires ManageMessages server permission.** | `.feedremove 3`
`.feeds` `.feedlist` | Shows the list of feeds you've subscribed to on this server. **Requires ManageMessages server permission.** | `.feeds`
`.yomama` `.ym` | Shows a random joke from <http://api.yomomma.info/>  | `.ym`
`.randjoke` `.rj` | Shows a random joke from <http://tambal.azurewebsites.net/joke/random>  | `.rj`
`.chucknorris` `.cn` | Shows a random Chuck Norris joke from <http://api.icndb.com/jokes/random/>  | `.cn`
`.wowjoke` | Get one of Kwoth's penultimate WoW jokes.  | `.wowjoke`
`.magicitem` `.mi` | Shows a random magic item from <https://1d4chan.org/wiki/List_of_/tg/%27s_magic_items>  | `.mi`
`.memelist` | Pulls a list of memes you can use with `.memegen` from http://memegen.link/templates/  | `.memelist`
`.memegen` | Generates a meme from memelist with top and bottom text.  | `.memegen biw "gets iced coffee" "in the winter"`
`.osu` | Shows osu stats for a player.  | `.osu Name` or `.osu Name taiko`
`.osub` | Shows information about an osu beatmap.  | `.osub https://osu.ppy.sh/s/127712`
`.osu5` | Displays a user's top 5 plays.  | `.osu5 Name`
`.overwatch` `.ow` | Show's basic stats on a player (competitive rank, playtime, level etc) Region codes are: `eu` `us` `cn` `kr`  | `.ow us Battletag#1337` or `.overwatch eu Battletag#2016`
`.placelist` | Shows the list of available tags for the `.place` command.  | `.placelist`
`.place` | Shows a placeholder image of a given tag. Use `.placelist` to see all available tags. You can specify the width and height of the image as the last two optional arguments.  | `.place Cage` or `.place steven 500 400`
`.pokemon` `.poke` | Searches for a pokemon.  | `.poke Sylveon`
`.pokemonability` `.pokeab` | Searches for a pokemon ability.  | `.pokeab overgrow`
`.smashcast` `.hb` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.** | `.smashcast SomeStreamer`
`.twitch` `.tw` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.** | `.twitch SomeStreamer`
`.mixer` `.bm` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.** | `.mixer SomeStreamer`
`.liststreams` `.ls` | Lists all streams you are following on this server.  | `.ls`
`.removestream` `.rms` | Removes notifications of a certain streamer from a certain platform on this channel. **Requires ManageMessages server permission.** | `.rms Twitch SomeGuy` or `.rms mixer SomeOtherGuy`
`.checkstream` `.cs` | Checks if a user is online on a certain streaming platform.  | `.cs twitch MyFavStreamer`
`.translate` `.trans` | Translates from>to text. From the given language to the destination language.  | `.trans en>fr Hello`
`.autotrans` `.at` | Starts automatic translation of all messages by users who set their `.atl` in this channel. You can set "del" argument to automatically delete all translated user messages. **Requires Administrator server permission.** **Bot owner only** | `.at` or `.at del`
`.autotranslang` `.atl` | Sets your source and target language to be used with `.at`. Specify no arguments to remove previously set value.  | `.atl en>fr`
`.translangs` | Lists the valid languages for translation.  | `.translangs`
`.xkcd` | Shows a XKCD comic. No arguments will retrieve random one. Number argument will retrieve a specific comic, and "latest" will get the latest one.  | `.xkcd` or `.xkcd 1400` or `.xkcd latest`

###### [Back to ToC](#table-of-contents)

### Utility  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.togethertube` `.totube` | Creates a new room on <https://togethertube.com> and shows the link in the chat.  | `.totube`
`.whosplaying` `.whpl` | Shows a list of users who are playing the specified game.  | `.whpl Overwatch`
`.inrole` | Lists every person from the specified role on this server. You can use role ID, role name.  | `.inrole Some Role`
`.checkmyperms` | Checks your user-specific permissions on this channel.  | `.checkmyperms`
`.userid` `.uid` | Shows user ID.  | `.uid` or `.uid @SomeGuy`
`.channelid` `.cid` | Shows current channel ID.  | `.cid`
`.serverid` `.sid` | Shows current server ID.  | `.sid`
`.roles` | List roles on this server or a roles of a specific user if specified. Paginated, 20 roles per page.  | `.roles 2` or `.roles @Someone`
`.channeltopic` `.ct` | Sends current channel's topic as a message.  | `.ct`
`.createinvite` `.crinv` | Creates a new invite which has infinite max uses and never expires. **Requires CreateInstantInvite channel permission.** | `.crinv`
`.shardstats` | Stats for shards. Paginated with 25 shards per page.  | `.shardstats` or `.shardstats 2`
`.stats` | Shows some basic stats for Nadeko.  | `.stats`
`.showemojis` `.se` | Shows a name and a link to every SPECIAL emoji in the message.  | `.se A message full of SPECIAL emojis`
`.listservers` | Lists servers the bot is on with some basic info. 15 per page. **Bot owner only** | `.listservers 3`
`.savechat` | Saves a number of messages to a text file and sends it to you. **Bot owner only** | `.savechat 150`
`.ping` | Ping the bot to see if there are latency issues.  | `.ping`
`.botconfigedit` `.bce` | Sets one of available bot config settings to a specified value. Use the command without any parameters to get a list of available settings. **Bot owner only** | `.bce CurrencyName b1nzy` or `.bce`
`.calculate` `.calc` | Evaluate a mathematical expression.  | `.calc 1+1`
`.calcops` | Shows all available operations in the `.calc` command  | `.calcops`
`.alias` `.cmdmap` | Create a custom alias for a certain Nadeko command. Provide no alias to remove the existing one. **Requires Administrator server permission.** | `.alias allin $bf 100 h` or `.alias "linux thingy" >loonix Spyware Windows`
`.aliaslist` `.cmdmaplist` `.aliases` | Shows the list of currently set aliases. Paginated.  | `.aliaslist` or `.aliaslist 3`
`.serverinfo` `.sinfo` | Shows info about the server the bot is on. If no server is supplied, it defaults to current one.  | `.sinfo Some Server`
`.channelinfo` `.cinfo` | Shows info about the channel. If no channel is supplied, it defaults to current one.  | `.cinfo #some-channel`
`.userinfo` `.uinfo` | Shows info about the user. If no user is supplied, it defaults a user running the command.  | `.uinfo @SomeUser`
`.activity` | Checks for spammers. **Bot owner only** | `.activity`
`.parewrel` | Forces the update of the list of patrons who are eligible for the reward. **Bot owner only** | `.parewrel`
`.clparew` | Claim patreon rewards. If you're subscribed to bot owner's patreon you can use this command to claim your rewards - assuming bot owner did setup has their patreon key.  | `.clparew`
`.listquotes` `.liqu` | Lists all quotes on the server ordered alphabetically. 15 Per page.  | `.liqu` or `.liqu 3`
`...` | Shows a random quote with a specified name.  | `... abc`
`.qsearch` | Shows a random quote for a keyword that contains any text specified in the search.  | `.qsearch keyword text`
`.quoteid` `.qid` | Displays the quote with the specified ID number. Quote ID numbers can be found by typing `.liqu [num]` where `[num]` is a number of a page which contains 15 quotes.  | `.qid 123456`
`..` | Adds a new quote with the specified name and message.  | `.. sayhi Hi`
`.quotedel` `.qdel` | Deletes a quote with the specified ID. You have to be either server Administrator or the creator of the quote to delete it.  | `.qdel 123456`
`.delallq` `.daq` | Deletes all quotes on a specified keyword. **Requires Administrator server permission.** | `.delallq kek`
`.remind` | Sends a message to you or a channel after certain amount of time. First argument is `me`/`here`/'channelname'. Second argument is time in a descending order (mo>w>d>h>m) example: 1w5d3h10m. Third argument is a (multiword) message.  | `.remind me 1d5h Do something` or `.remind #general 1m Start now!`
`.remindtemplate` | Sets message for when the remind is triggered.  Available placeholders are `%user%` - user who ran the command, `%message%` - Message specified in the remind, `%target%` - target channel of the remind. **Bot owner only** | `.remindtemplate %user%, do %message%!`
`.repeatinvoke` `.repinv` | Immediately shows the repeat message on a certain index and restarts its timer. **Requires ManageMessages server permission.** | `.repinv 1`
`.repeatremove` `.reprm` | Removes a repeating message on a specified index. Use `.repeatlist` to see indexes. **Requires ManageMessages server permission.** | `.reprm 2`
`.repeat` | Repeat a message every `X` minutes in the current channel. You can instead specify time of day for the message to be repeated at daily (make sure you've set your server's timezone). You can have up to 5 repeating messages on the server in total. **Requires ManageMessages server permission.** | `.repeat 5 Hello there` or `.repeat 17:30 tea time`
`.repeatlist` `.replst` | Shows currently repeating messages and their indexes. **Requires ManageMessages server permission.** | `.repeatlist`
`.streamrole` | Sets a role which is monitored for streamers (FromRole), and a role to add if a user from 'FromRole' is streaming (AddRole). When a user from 'FromRole' starts streaming, they will receive an 'AddRole'. Provide no arguments to disable **Requires ManageRoles server permission.** | `.streamrole "Eligible Streamers" "Featured Streams"`
`.streamrolekw` `.srkw` | Sets keyword which is required in the stream's title in order for the streamrole to apply. Provide no keyword in order to reset. **Requires ManageRoles server permission.** | `.srkw` or `.srkw PUBG`
`.streamrolebl` `.srbl` | Adds or removes a blacklisted user. Blacklisted users will never receive the stream role. **Requires ManageRoles server permission.** | `.srbl add @b1nzy#1234` or `.srbl rem @b1nzy#1234`
`.streamrolewl` `.srwl` | Adds or removes a whitelisted user. Whitelisted users will receive the stream role even if they don't have the specified keyword in their stream title. **Requires ManageRoles server permission.** | `.srwl add @b1nzy#1234` or `.srwl rem @b1nzy#1234`
`.convertlist` | List of the convertible dimensions and currencies.  | `.convertlist`
`.convert` | Convert quantities. Use `.convertlist` to see supported dimensions and currencies.  | `.convert m km 1000`
`.verboseerror` `.ve` | Toggles whether the bot should print command errors when a command is incorrectly used. **Requires ManageMessages server permission.** | `.ve`

###### [Back to ToC](#table-of-contents)

### Xp  
Commands and aliases | Description | Usage
----------------|--------------|-------
`.experience` `.xp` | Shows your xp stats. Specify the user to show that user's stats instead.  | `.xp`
`.xprolerewards` `.xprrs` | Shows currently set role rewards.  | `.xprrs`
`.xprolereward` `.xprr` | Sets a role reward on a specified level. Provide no role name in order to remove the role reward. **Requires ManageRoles server permission.** | `.xprr 3 Social`
`.xpnotify` `.xpn` | Sets how the bot should notify you when you get a `server` or `global` level. You can set `dm` (for the bot to send a direct message), `channel` (to get notified in the channel you sent the last message in) or `none` to disable.  | `.xpn global dm` or `.xpn server channel`
`.xpexclude` `.xpex` | Exclude a channel, role or current server from the xp system. **Requires Administrator server permission.** | `.xpex Role Excluded-Role` or `.xpex Server`
`.xpexclusionlist` `.xpexl` | Shows the roles and channels excluded from the XP system on this server, as well as whether the whole server is excluded.  | `.xpexl`
`.xpleaderboard` `.xplb` | Shows current server's xp leaderboard.  | `.xplb`
`.xpgleaderboard` `.xpglb` | Shows the global xp leaderboard.  | `.xpglb`
`.xpadd` | Adds xp to a user on the server. This does not affect their global ranking. You can use negative values. **Requires Administrator server permission.** | `.xpadd 100 @b1nzy`
`.clubadmin` | Assigns (or unassigns) staff role to the member of the club. Admins can ban, kick and accept applications.  | `.clubadmin`
`.clubcreate` | Creates a club. You must be atleast level 5 and not be in the club already.  | `.clubcreate b1nzy's friends`
`.clubicon` | Sets the club icon.  | `.clubicon https://i.imgur.com/htfDMfU.png`
`.clubinfo` | Shows information about the club.  | `.clubinfo b1nzy's friends#123`
`.clubbans` | Shows the list of users who have banned from your club. Paginated. You must be club owner to use this command.  | `.clubbans 2`
`.clubapps` | Shows the list of users who have applied to your club. Paginated. You must be club owner to use this command.  | `.clubapps 2`
`.clubapply` | Apply to join a club. You must meet that club's minimum level requirement, and not be on its ban list.  | `.clubapply b1nzy's friends#123`
`.clubaccept` | Accept a user who applied to your club.  | `.clubaccept b1nzy#1337`
`.clubleave` | Leaves the club you're currently in.  | `.clubleave`
`.clubkick` | Kicks the user from the club. You must be the club owner. They will be able to apply again.  | `.clubkick b1nzy#1337`
`.clubban` | Bans the user from the club. You must be the club owner. They will not be able to apply again.  | `.clubban b1nzy#1337`
`.clubunban` | Unbans the previously banned user from the club. You must be the club owner.  | `.clubunban b1nzy#1337`
`.clublevelreq` | Sets the club required level to apply to join the club. You must be club owner. You can't set this number below 5.  | `.clublevelreq 7`
`.clubdisband` | Disbands the club you're the owner of. This action is irreversible.  | `.clubdisband`
`.clublb` | Shows club rankings on the specified page.  | `.clublb 2`
