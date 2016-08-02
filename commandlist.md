######For more information and how to setup your own NadekoBot, go to: **http://github.com/Kwoth/NadekoBot/**
######You can donate on patreon: `https://patreon.com/nadekobot`
######or paypal: `nadekodiscordbot@gmail.com`

#NadekoBot List Of Commands  
Version: `NadekoBot v0.9.6054.4837`
### Help  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`-h`, `-help`, `@BotName help`, `@BotName h`, `~h`  |  Either shows a help for a single command, or PMs you help link if no arguments are specified. |  `-h !m q` or just `-h` 
`-hgit`  |  Generates the commandlist.md file. **Bot Owner Only!** |  `-hgit`
`-readme`, `-guide`  |  Sends a readme and a guide links to the channel. |  `-readme` or `-guide`
`-donate`, `~donate`  |  Instructions for helping the project! |  `{Prefix}donate` or `~donate`
`-modules`, `.modules`  |  List all bot modules. |  `{Prefix}modules` or `.modules`
`-commands`, `.commands`  |  List all of the bot's commands from a certain module. |  `{Prefix}commands` or `.commands`

### Administration  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`.grdel`  |  Toggles automatic deletion of greet and bye messages. |  `.grdel`
`.greet`  |  Toggles anouncements on the current channel when someone joins the server. |  `.greet`
`.greetmsg`  |  Sets a new join announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current greet message. |  `.greetmsg Welcome to the server, %user%.`
`.bye`  |  Toggles anouncements on the current channel when someone leaves the server. |  `.bye`
`.byemsg`  |  Sets a new leave announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current bye message. |  `.byemsg %user% has left the server.`
`.byepm`  |  Toggles whether the good bye messages will be sent in a PM or in the text channel. |  `.byepm`
`.greetpm`  |  Toggles whether the greet messages will be sent in a PM or in the text channel. |  `.greetpm`
`.spmom`  |  Toggles whether mentions of other offline users on your server will send a pm to them. |  `.spmom`
`.logserver`  |  Toggles logging in this channel. Logs every message sent/deleted/edited on the server. **Bot Owner Only!** |  `.logserver`
`.logignore`  |  Toggles whether the .logserver command ignores this channel. Useful if you have hidden admin channel and public log channel. |  `.logignore`
`.userpresence`  |  Starts logging to this channel when someone from the server goes online/offline/idle. |  `.userpresence`
`.voicepresence`  |  Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now. |  `{Prefix}voicerpresence`
`.repeatinvoke`, `.repinv`  |  Immediately shows the repeat message and restarts the timer. |  `{Prefix}repinv`
`.repeat`  |  Repeat a message every X minutes. If no parameters are specified, repeat is disabled. Requires manage messages. | `.repeat 5 Hello there`
`.rotateplaying`, `.ropl`  |  Toggles rotation of playing status of the dynamic strings you specified earlier. |  `.ropl`
`.addplaying`, `.adpl`  |  Adds a specified string to the list of playing strings to rotate. Supported placeholders: %servers%, %users%, %playing%, %queued%, %trivia% |  `.adpl`
`.listplaying`, `.lipl`  |  Lists all playing statuses with their corresponding number. |  `.lipl`
`.removeplaying`, `.repl`, `.rmpl`  |  Removes a playing string on a given number. |  `.rmpl`
`.slowmode`  |  Toggles slow mode. When ON, users will be able to send only 1 message every 5 seconds. |  `.slowmode`
`.cleanv+t`, `.cv+t`  |  Deletes all text channels ending in `-voice` for which voicechannels are not found. **Use at your own risk.** |  `.cleanv+t`
`.voice+text`, `.v+t`  |  Creates a text channel for each voice channel only users in that voice channel can see.If you are server owner, keep in mind you will see them all the time regardless. |  `.voice+text`
`.scsc`  |  Starts an instance of cross server channel. You will get a token as a DM that other people will use to tune in to the same instance. |  `.scsc`
`.jcsc`  |  Joins current channel to an instance of cross server channel using the token. |  `.jcsc`
`.lcsc`  |  Leaves Cross server channel instance from this channel. |  `.lcsc`
`.asar`  |  Adds a role, or list of roles separated by whitespace(use quotations for multiword roles) to the list of self-assignable roles. |  .asar Gamer
`.rsar`  |  Removes a specified role from the list of self-assignable roles. |  `.rsar`
`.lsar`  |  Lists all self-assignable roles. |  `.lsar`
`.togglexclsar`, `.tesar`  |  toggle whether the self-assigned roles should be exclusive |  `.tesar`
`.iam`  |  Adds a role to you that you choose. Role must be on a list of self-assignable roles. |  .iam Gamer
`.iamnot`, `.iamn`  |  Removes a role to you that you choose. Role must be on a list of self-assignable roles. |  .iamn Gamer
`.addcustreact`, `.acr`  |  Add a custom reaction. Guide here: <https://github.com/Kwoth/NadekoBot/wiki/Custom-Reactions> **Bot Owner Only!**   |  `.acr "hello" I love saying hello to %user%`
`.listcustreact`, `.lcr`  |  Lists custom reactions (paginated with 30 commands per page). Use 'all' instead of page number to get all custom reactions DM-ed to you.  | `.lcr 1`
`.showcustreact`, `.scr`  |  Shows all possible responses from a single custom reaction. | `.scr %mention% bb`
`.editcustreact`, `.ecr`  |  Edits a custom reaction, arguments are custom reactions name, index to change, and a (multiword) message **Bot Owner Only** |  `.ecr "%mention% disguise" 2 Test 123`
`.delcustreact`, `.dcr`  |  Deletes a custom reaction with given name (and index). |  `.dcr index`
`.autoassignrole`, `.aar`  |  Automaticaly assigns a specified role to every user who joins the server. Type `.aar` to disable, `.aar Role Name` to enable
`.leave`  |  Makes Nadeko leave the server. Either name or id required. |  `.leave 123123123331`
`.listincidents`, `.lin`  |  List all UNREAD incidents and flags them as read. |  `.lin`
`.listallincidents`, `.lain`  |  Sends you a file containing all incidents and flags them as read. |  `.lain`
`.delmsgoncmd`  |  Toggles the automatic deletion of user's successful command message to prevent chat flood. Server Manager Only. |  `.delmsgoncmd`
`.restart`  |  Restarts the bot. Might not work. **Bot Owner Only** |  `.restart`
`.setrole`, `.sr`  |  Sets a role for a given user. |  `.sr @User Guest`
`.removerole`, `.rr`  |  Removes a role from a given user. |  `.rr @User Admin`
`.renamerole`, `.renr`  |  Renames a role. Role you are renaming must be lower than bot's highest role. |  `.renr "First role" SecondRole`
`.removeallroles`, `.rar`  |  Removes all roles from a mentioned user. |  `.rar @User`
`.createrole`, `.cr`  |  Creates a role with a given name. |  `.cr Awesome Role`
`.rolecolor`, `.rc`  |  Set a role's color to the hex or 0-255 rgb color value provided. |  `.rc Admin 255 200 100` or `.rc Admin ffba55`
`.ban`, `.b`  |  Bans a user by id or name with an optional message. |  `.b "@some Guy" Your behaviour is toxic.`
`.softban`, `.sb`  |  Bans and then unbans a user by id or name with an optional message. |  `.sb "@some Guy" Your behaviour is toxic.`
`.kick`, `.k`  |  Kicks a mentioned user. |  `.k "@some Guy" Your behaviour is toxic.`
`.mute`  |  Mutes mentioned user or users. |  `.mute "@Someguy"` or `.mute "@Someguy" "@Someguy"`
`.unmute`  |  Unmutes mentioned user or users. |  `.unmute "@Someguy"` or `.unmute "@Someguy" "@Someguy"`
`.deafen`, `.deaf`  |  Deafens mentioned user or users |  `.deaf "@Someguy"` or `.deaf "@Someguy" "@Someguy"`
`.undeafen`, `.undef`  |  Undeafens mentioned user or users |  `.undef "@Someguy"` or `.undef "@Someguy" "@Someguy"`
`.delvoichanl`, `.dvch`  |  Deletes a voice channel with a given name. |  `.dvch VoiceChannelName`
`.creatvoichanl`, `.cvch`  |  Creates a new voice channel with a given name. |  `.cvch VoiceChannelName`
`.deltxtchanl`, `.dtch`  |  Deletes a text channel with a given name. |  `.dtch TextChannelName`
`.creatxtchanl`, `.ctch`  |  Creates a new text channel with a given name. |  `.ctch TextChannelName`
`.settopic`, `.st`  |  Sets a topic on the current channel. |  `.st My new topic`
`.setchanlname`, `.schn`  |  Changed the name of the current channel.| `.schn NewName`
`.heap`  |  Shows allocated memory - **Bot Owner Only!** |  `.heap`
`.prune`, `.clr`  |  `.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel. |  `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X`
`.die`  |  Shuts the bot down and notifies users about the restart. **Bot Owner Only!** |  `.die`
`.setname`, `.newnm`  |  Give the bot a new name. **Bot Owner Only!** |  .newnm BotName
`.newavatar`, `.setavatar`  |  Sets a new avatar image for the NadekoBot. Argument is a direct link to an image. **Bot Owner Only!** |  `.setavatar https://i.ytimg.com/vi/WDudkR1eTMM/maxresdefault.jpg`
`.setgame`  |  Sets the bots game. **Bot Owner Only!** |  `.setgame Playing with kwoth`
`.send`  |  Send a message to someone on a different server through the bot. **Bot Owner Only!** |  `.send serverid|u:user_id Send this to a user!` or `.send serverid|c:channel_id Send this to a channel!`
`.mentionrole`, `.menro`  |  Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission. |  `.menro RoleName`
`.unstuck`  |  Clears the message queue. **Bot Owner Only!** |  `.unstuck`
`.donators`  |  List of lovely people who donated to keep this project alive.
`.donadd`  |  Add a donator to the database. |  `.donadd Donate Amount`
`.announce`  |  Sends a message to all servers' general channel bot is connected to.**Bot Owner Only!** |  `.announce Useless spam`
`.savechat`  |  Saves a number of messages to a text file and sends it to you. **Bot Owner Only** |  `.savechat 150`

### Utility  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`.remind`  |  Sends a message to you or a channel after certain amount of time. First argument is me/here/'channelname'. Second argument is time in a descending order (mo>w>d>h>m) example: 1w5d3h10m. Third argument is a (multiword)message.  |  `.remind me 1d5h Do something` or `.remind #general Start now!`
`.remindmsg`  |  Sets message for when the remind is triggered.  Available placeholders are %user% - user who ran the command, %message% - Message specified in the remind, %target% - target channel of the remind. **Bot Owner Only!** |  `.remindmsg do something else`
`.serverinfo`, `.sinfo`  |  Shows info about the server the bot is on. If no channel is supplied, it defaults to current one. | `.sinfo Some Server`
`.channelinfo`, `.cinfo`  |  Shows info about the channel. If no channel is supplied, it defaults to current one. | `.cinfo #some-channel`
`.userinfo`, `.uinfo`  |  Shows info about the user. If no user is supplied, it defaults a user running the command. | `.uinfo @SomeUser`
`.whoplays`  |  Shows a list of users who are playing the specified game. |  `.whoplays Overwatch`
`.inrole`  |  Lists every person from the provided role or roles (separated by a ',') on this server. If the list is too long for 1 message, you must have Manage Messages permission. |  `.inrole Role`
`.checkmyperms`  |  Checks your userspecific permissions on this channel. |  `.checkmyperms`
`.stats`  |  Shows some basic stats for Nadeko. |  `.stats`
`.dysyd`  |  Shows some basic stats for Nadeko. |  `.dysyd`
`.userid`, `.uid`  |  Shows user ID. |  `.uid` or `.uid "@SomeGuy"`
`.channelid`, `.cid`  |  Shows current channel ID. |  `.cid`
`.serverid`, `.sid`  |  Shows current server ID. |  `.sid`
`.roles`  |  List all roles on this server or a single user if specified.
`.channeltopic`, `.ct`  |  Sends current channel's topic as a message. |  `.ct`

### Permissions  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`;chnlfilterinv`, `;cfi`  |  Enables or disables automatic deleting of invites on the channel.If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once. |  ;cfi enable #general-chat
`;srvrfilterinv`, `;sfi`  |  Enables or disables automatic deleting of invites on the server. |  ;sfi disable
`;chnlfilterwords`, `;cfw`  |  Enables or disables automatic deleting of messages containing banned words on the channel.If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once. |  ;cfw enable #general-chat
`;addfilterword`, `;afw`  |  Adds a new word to the list of filtered words |  ;afw poop
`;rmvfilterword`, `;rfw`  |  Removes the word from the list of filtered words |  ;rw poop
`;lstfilterwords`, `;lfw`  |  Shows a list of filtered words |  ;lfw
`;srvrfilterwords`, `;sfw`  |  Enables or disables automatic deleting of messages containing forbidden words on the server. |  ;sfw disable
`;permrole`, `;pr`  |  Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'. |  `;pr role`
`;rolepermscopy`, `;rpc`  |  Copies BOT PERMISSIONS (not discord permissions) from one role to another. | `;rpc Some Role ~ Some other role`
`;chnlpermscopy`, `;cpc`  |  Copies BOT PERMISSIONS (not discord permissions) from one channel to another. | `;cpc Some Channel ~ Some other channel`
`;usrpermscopy`, `;upc`  |  Copies BOT PERMISSIONS (not discord permissions) from one role to another. | `;upc @SomeUser ~ @SomeOtherUser`
`;verbose`, `;v`  |  Sets whether to show when a command/module is blocked. |  `;verbose true`
`;srvrperms`, `;sp`  |  Shows banned permissions for this server. |  `;sp`
`;roleperms`, `;rp`  |  Shows banned permissions for a certain role. No argument means for everyone. |  `;rp AwesomeRole`
`;chnlperms`, `;cp`  |  Shows banned permissions for a certain channel. No argument means for this channel. |  `;cp #dev`
`;userperms`, `;up`  |  Shows banned permissions for a certain user. No argument means for yourself. |  `;up Kwoth`
`;srvrmdl`, `;sm`  |  Sets a module's permission at the server level. |  `;sm "module name" enable`
`;srvrcmd`, `;sc`  |  Sets a command's permission at the server level. |  `;sc "command name" disable`
`;rolemdl`, `;rm`  |  Sets a module's permission at the role level. |  `;rm "module name" enable MyRole`
`;rolecmd`, `;rc`  |  Sets a command's permission at the role level. |  `;rc "command name" disable MyRole`
`;chnlmdl`, `;cm`  |  Sets a module's permission at the channel level. |  `;cm "module name" enable SomeChannel`
`;chnlcmd`, `;cc`  |  Sets a command's permission at the channel level. |  `;cc "command name" enable SomeChannel`
`;usrmdl`, `;um`  |  Sets a module's permission at the user level. |  `;um "module name" enable SomeUsername`
`;usrcmd`, `;uc`  |  Sets a command's permission at the user level. |  `;uc "command name" enable SomeUsername`
`;allsrvrmdls`, `;asm`  |  Sets permissions for all modules at the server level. |  `;asm [enable/disable]`
`;allsrvrcmds`, `;asc`  |  Sets permissions for all commands from a certain module at the server level. |  `;asc "module name" [enable/disable]`
`;allchnlmdls`, `;acm`  |  Sets permissions for all modules at the channel level. |  `;acm [enable/disable] SomeChannel`
`;allchnlcmds`, `;acc`  |  Sets permissions for all commands from a certain module at the channel level. |  `;acc "module name" [enable/disable] SomeChannel`
`;allrolemdls`, `;arm`  |  Sets permissions for all modules at the role level. |  `;arm [enable/disable] MyRole`
`;allrolecmds`, `;arc`  |  Sets permissions for all commands from a certain module at the role level. |  `;arc "module name" [enable/disable] MyRole`
`;ubl`  |  Blacklists a mentioned user. |  `;ubl [user_mention]`
`;uubl`  |  Unblacklists a mentioned user. |  `;uubl [user_mention]`
`;cbl`  |  Blacklists a mentioned channel (#general for example). |  `;cbl #some_channel`
`;cubl`  |  Unblacklists a mentioned channel (#general for example). |  `;cubl #some_channel`
`;sbl`  |  Blacklists a server by a name or id (#general for example). **BOT OWNER ONLY** |  `;sbl [servername/serverid]`
`;cmdcooldown`, `;cmdcd`  |  Sets a cooldown per user for a command. Set 0 to clear. |  `;cmdcd "some cmd" 5`
`;allcmdcooldowns`, `;acmdcds`  |  Shows a list of all commands and their respective cooldowns.

### Conversations  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`..`  |  Adds a new quote with the specified name (single word) and message (no limit). |  `.. abc My message`
`...`  |  Shows a random quote with a specified name. |  `... abc`
`..qdel`, `..quotedelete`  |  Deletes all quotes with the specified keyword. You have to either be bot owner or the creator of the quote to delete it. |  `..qdel abc`
`@BotName rip`  |  Shows a grave image of someone with a start year |  @NadekoBot rip @Someone 2000
`@BotName die`  |  Works only for the owner. Shuts the bot down. |  `@NadekoBot die`
`@BotName do you love me`  |  Replies with positive answer only to the bot owner. |  `@NadekoBot do you love me`
`@BotName how are you`, `@BotName how are you?`  |  Replies positive only if bot owner is online. |  `@NadekoBot how are you`
`@BotName fire`  |  Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire. |  `@NadekoBot fire [x]`
`@BotName dump`  |  Dumps all of the invites it can to dump.txt.** Owner Only.** |  `@NadekoBot dump`
`@BotName ab`  |  Try to get 'abalabahaha'| `@NadekoBot ab`

### Gambling  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`$draw`  |  Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck. |  `$draw [x]`
`$shuffle`, `$sh`  |  Reshuffles all cards back into the deck.|`$shuffle`
`$flip`  |  Flips coin(s) - heads or tails, and shows an image. |  `$flip` or `$flip 3`
`$betflip`, `$bf`  |  Bet to guess will the result be heads or tails. Guessing award you double flowers you've bet. |  `$bf 5 heads` or `$bf 3 t`
`$roll`  |  Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. |  `$roll` or `$roll 7` or `$roll 3d5`
`$rolluo`  |  Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice (unordered). If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. |  `$roll` or `$roll` 7 or `$roll 3d5`
`$nroll`  |  Rolls in a given range. |  `$nroll 5` (rolls 0-5) or `$nroll 5-15`
`$race`  |  Starts a new animal race. |  `$race`
`$joinrace`, `$jr`  |  Joins a new race. You can specify an amount of flowers for betting (optional). You will get YourBet*(participants-1) back if you win. |  `$jr` or `$jr 5`
`$raffle`  |  Prints a name and ID of a random user from the online list from the (optional) role. |  `$raffle` or `$raffle RoleName`
`$$$`  |  Check how much NadekoFlowers a person has. (Defaults to yourself) | `$$$` or `$$$ @Someone`
`$give`  |  Give someone a certain amount of NadekoFlowers|`$give 1 "@SomeGuy"`
`$award`  |  Gives someone a certain amount of flowers. **Bot Owner Only!** |  `$award 100 @person`
`$take`  |  Takes a certain amount of flowers from someone. **Bot Owner Only!** |  `$take 1 "@someguy"`
`$betroll`, `$br`  |  Bets a certain amount of NadekoFlowers and rolls a dice. Rolling over 66 yields x2 flowers, over 90 - x3 and 100 x10. |  `$br 5`
`$leaderboard`, `$lb`  |  Displays bot currency leaderboard |  $lb

### Games  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`>t`  |  Starts a game of trivia. You can add nohint to prevent hints.First player to get to 10 points wins by default. You can specify a different number. 30 seconds per question. | `>t nohint` or `>t 5 nohint`
`>tl`  |  Shows a current trivia leaderboard. |  `>tl`
`>tq`  |  Quits current trivia after current question. |  `>tq`
`>typestart`  |  Starts a typing contest. |  `>typestart`
`>typestop`  |  Stops a typing contest on the current channel. |  `>typestop`
`>typeadd`  |  Adds a new article to the typing contest. Owner only. |  `>typeadd wordswords`
`>poll`  |  Creates a poll, only person who has manage server permission can do it. |  `>poll Question?;Answer1;Answ 2;A_3`
`>pollend`  |  Stops active poll on this server and prints the results in this channel. |  `>pollend`
`>pick`  |  Picks a flower planted in this channel. |  `>pick`
`>plant`  |  Spend a flower to plant it in this channel. (If bot is restarted or crashes, flower will be lost) |  `>plant`
`>gencurrency`, `>gc`  |  Toggles currency generation on this channel. Every posted message will have 2% chance to spawn a NadekoFlower. Optional parameter cooldown time in minutes, 5 minutes by default. Requires Manage Messages permission. |  `>gc` or `>gc 60`
`>leet`  |  Converts a text to leetspeak with 6 (1-6) severity levels |  `>leet 3 Hello`
`>choose`  |  Chooses a thing from a list of things |  `>choose Get up;Sleep;Sleep more`
`>8ball`  |  Ask the 8ball a yes/no question. |  `>8ball should i do something`
`>rps`  |  Play a game of rocket paperclip scissors with Nadeko. |  `>rps scissors`
`>linux`  |  Prints a customizable Linux interjection |  `>linux Spyware Windows`

### Music  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`!!next`, `!!n`, `!!skip`  |  Goes to the next song in the queue. You have to be in the same voice channel as the bot. |  `!!n`
`!!stop`, `!!s`  |  Stops the music and clears the playlist. Stays in the channel. |  `!!s`
`!!destroy`, `!!d`  |  Completely stops the music and unbinds the bot from the channel. (may cause weird behaviour) |  `!!d`
`!!pause`, `!!p`  |  Pauses or Unpauses the song. |  `!!p`
`!!queue`, `!!q`, `!!yq`  |  Queue a song using keywords or a link. Bot will join your voice channel.**You must be in a voice channel**. |  `!!q Dream Of Venice`
`!!soundcloudqueue`, `!!sq`  |  Queue a soundcloud song using keywords. Bot will join your voice channel.**You must be in a voice channel**. |  `!!sq Dream Of Venice`
`!!listqueue`, `!!lq`  |  Lists 15 currently queued songs per page. Default page is 1. |  `!!lq` or `!!lq 2`
`!!nowplaying`, `!!np`  |  Shows the song currently playing. |  `!!np`
`!!volume`, `!!vol`  |  Sets the music volume 0-100% |  `!!vol 50`
`!!defvol`, `!!dv`  |  Sets the default music volume when music playback is started (0-100). Persists through restarts. |  `!!dv 80`
`!!mute`, `!!min`  |  Sets the music volume to 0% |  `!!min`
`!!max`  |  Sets the music volume to 100%. |  `!!max`
`!!half`  |  Sets the music volume to 50%. |  `!!half`
`!!shuffle`, `!!sh`  |  Shuffles the current playlist. |  `!!sh`
`!!playlist`, `!!pl`  |  Queues up to 500 songs from a youtube playlist specified by a link, or keywords. |  `!!pl playlist link or name`
`!!soundcloudpl`, `!!scpl`  |  Queue a soundcloud playlist using a link. |  `!!scpl https://soundcloud.com/saratology/sets/symphony`
`!!localplaylst`, `!!lopl`  |  Queues all songs from a directory. **Bot Owner Only!** |  `!!lopl C:/music/classical`
`!!radio`, `!!ra`  |  Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf (Usage Video: <https://streamable.com/al54>) |  `!!ra radio link here`
`!!local`, `!!lo`  |  Queues a local file by specifying a full path. **Bot Owner Only!** |  `!!lo C:/music/mysong.mp3`
`!!move`, `!!mv`  |  Moves the bot to your voice channel. (works only if music is already playing) |  `!!mv`
`!!remove`, `!!rm`  |  Remove a song by its # in the queue, or 'all' to remove whole queue. |  `!!rm 5`
`!!movesong`, `!!ms`  |  Moves a song from one position to another. |  `!! ms` 5>3
`!!setmaxqueue`, `!!smq`  |  Sets a maximum queue size. Supply 0 or no argument to have no limit.  |  `!!smq` 50 or `!!smq`
`!!cleanup`  |  Cleans up hanging voice connections. **Bot Owner Only!** |  `!!cleanup`
`!!reptcursong`, `!!rcs`  |  Toggles repeat of current song. |  `!!rcs`
`!!rpeatplaylst`, `!!rpl`  |  Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue). |  `!!rpl`
`!!save`  |  Saves a playlist under a certain name. Name must be no longer than 20 characters and mustn't contain dashes. |  `!!save classical1`
`!!load`  |  Loads a playlist under a certain name.  |  `!!load classical-1`
`!!playlists`, `!!pls`  |  Lists all playlists. Paginated. 20 per page. Default page is 0. | `!!pls 1`
`!!deleteplaylist`, `!!delpls`  |  Deletes a saved playlist. Only if you made it or if you are the bot owner. |  `!!delpls animu-5`
`!!goto`  |  Goes to a specific time in seconds in a song. |  `!!goto 30`
`!!getlink`, `!!gl`  |  Shows a link to the currently playing song.
`!!autoplay`, `!!ap`  |  Toggles autoplay - When the song is finished, automatically queue a related youtube song. (Works only for youtube songs and when queue is empty)

### Searches  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~lolchamp`  |  Shows League Of Legends champion statistics. If there are spaces/apostrophes or in the name - omit them. Optional second parameter is a role. | `~lolchamp Riven` or `~lolchamp Annie sup`
`~lolban`  |  Shows top 6 banned champions ordered by ban rate. Ban these champions and you will be Plat 5 in no time. |  `~lolban`
`~hitbox`, `~hb`  |  Notifies this channel when a certain user starts streaming. |  `~hitbox SomeStreamer`
`~twitch`, `~tw`  |  Notifies this channel when a certain user starts streaming. |  `~twitch SomeStreamer`
`~beam`, `~bm`  |  Notifies this channel when a certain user starts streaming. |  `~beam SomeStreamer`
`~checkhitbox`, `~chhb`  |  Checks if a certain user is streaming on the hitbox platform. |  `~chhb SomeStreamer`
`~checktwitch`, `~chtw`  |  Checks if a certain user is streaming on the twitch platform. |  `~chtw SomeStreamer`
`~checkbeam`, `~chbm`  |  Checks if a certain user is streaming on the beam platform. |  `~chbm SomeStreamer`
`~removestream`, `~rms`  |  Removes notifications of a certain streamer on this channel. |  `~rms SomeGuy`
`~liststreams`, `~ls`  |  Lists all streams you are following on this server. |  `~ls`
`~convert`  |  Convert quantities from>to. |  `~convert m>km 1000`
`~convertlist`  |  List of the convertable dimensions and currencies.
`~wowjoke`  |  Get one of Kwoth's penultimate WoW jokes. |  `~wowjoke`
`~calculate`, `~calc`  |  Evaluate a mathematical expression. |  ~calc 1+1
`~osu`  |  Shows osu stats for a player. |  `~osu Name` or `~osu Name taiko`
`~osu b`  |  Shows information about an osu beatmap. | `~osu b` https://osu.ppy.sh/s/127712`
`~osu top5`  |  Displays a user's top 5 plays.  | ~osu top5 Name
`~pokemon`, `~poke`  |  Searches for a pokemon. |  `~poke Sylveon`
`~pokemonability`, `~pokeab`  |  Searches for a pokemon ability. |  `~pokeab "water gun"`
`~memelist`  |  Pulls a list of memes you can use with `~memegen` from http://memegen.link/templates/ |  `~memelist`
`~memegen`  |  Generates a meme from memelist with top and bottom text. |  `~memegen biw "gets iced coffee" "in the winter"`
`~we`  |  Shows weather data for a specified city and a country. BOTH ARE REQUIRED. Use country abbrevations. |  `~we Moscow RF`
`~yt`  |  Searches youtubes and shows the first result |  `~yt query`
`~ani`, `~anime`, `~aq`  |  Queries anilist for an anime and shows the first result. |  `~aq aquerion evol`
`~imdb`  |  Queries imdb for movies or series, show first result. |  `~imdb query`
`~mang`, `~manga`, `~mq`  |  Queries anilist for a manga and shows the first result. |  `~mq query`
`~randomcat`, `~meow`  |  Shows a random cat image.
`~randomdog`, `~woof`  |  Shows a random dog image.
`~i`  |  Pulls the first image found using a search parameter. Use ~ir for different results. |  `~i cute kitten`
`~ir`  |  Pulls a random image using a search parameter. |  `~ir cute kitten`
`~lmgtfy`  |  Google something for an idiot. |  `~lmgtfy query`
`~google`, `~g`  |  Get a google search link for some terms. |  `~google query`
`~hs`  |  Searches for a Hearthstone card and shows its image. Takes a while to complete. |  `~hs Ysera`
`~ud`  |  Searches Urban Dictionary for a word. |  `~ud Pineapple`
`~#`  |  Searches Tagdef.com for a hashtag. |  `~# ff`
`~quote`  |  Shows a random quote. |  `~quote`
`~catfact`  |  Shows a random catfact from <http://catfacts-api.appspot.com/api/facts> |  `~catfact`
`~yomama`, `~ym`  |  Shows a random joke from <http://api.yomomma.info/> |  `~ym`
`~randjoke`, `~rj`  |  Shows a random joke from <http://tambal.azurewebsites.net/joke/random> |  `~rj`
`~chucknorris`, `~cn`  |  Shows a random chucknorris joke from <http://tambal.azurewebsites.net/joke/random> |  `~cn`
`~magicitem`, `~mi`  |  Shows a random magicitem from <https://1d4chan.org/wiki/List_of_/tg/%27s_magic_items> |  `~mi`
`~revav`  |  Returns a google reverse image search for someone's avatar. |  `~revav "@SomeGuy"`
`~revimg`  |  Returns a google reverse image search for an image from a link. |  `~revav Image link`
`~safebooru`  |  Shows a random image from safebooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  `~safebooru yuri+kissing`
`~wiki`  |  Gives you back a wikipedia link |  `~wiki query`
`~clr`  |  Shows you what color corresponds to that hex. |  `~clr 00ff00`
`~videocall`  |  Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message. |  `~videocall "@SomeGuy"`
`~av`, `~avatar`  |  Shows a mentioned person's avatar. |  `~av @X`

### NSFW  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~hentai`  |  Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  `~hentai yuri+kissing`
`~danbooru`  |  Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  `~danbooru yuri+kissing`
`~gelbooru`  |  Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  `~gelbooru yuri+kissing`
`~rule34`  |  Shows a random image from rule34.xx with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  `~rule34 yuri+kissing`
`~e621`  |  Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags. |  `~e621 yuri kissing`
`~cp`  |  We all know where this will lead you to. |  `~cp`
`~boobs`  |  Real adult content. |  `~boobs`
`~butts`, `~ass`, `~butt`  |  Real adult content. |  `~butts` or `~ass`

### ClashOfClans  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`,createwar`, `,cw`  |  Creates a new war by specifying a size (>10 and multiple of 5) and enemy clan name. | ,cw 15 The Enemy Clan
`,startwar`, `,sw`  |  Starts a war with a given number.
`,listwar`, `,lw`  |  Shows the active war claims by a number. Shows all wars in a short way if no number is specified. |  ,lw [war_number] or ,lw
`,claim`, `,call`, `,c`  |  Claims a certain base from a certain war. You can supply a name in the third optional argument to claim in someone else's place.  |  ,call [war_number] [base_number] [optional_other_name]
`,claimfinish`, `,cf`, `,cf3`, `,claimfinish3`  |  Finish your claim with 3 stars if you destroyed a base. Optional second argument finishes for someone else. |  ,cf [war_number] [optional_other_name]
`,claimfinish2`, `,cf2`  |  Finish your claim with 2 stars if you destroyed a base. Optional second argument finishes for someone else. |  ,cf [war_number] [optional_other_name]
`,claimfinish1`, `,cf1`  |  Finish your claim with 1 stars if you destroyed a base. Optional second argument finishes for someone else. |  ,cf [war_number] [optional_other_name]
`,unclaim`, `,uncall`, `,uc`  |  Removes your claim from a certain war. Optional second argument denotes a person in whose place to unclaim |  ,uc [war_number] [optional_other_name]
`,endwar`, `,ew`  |  Ends the war with a given index. | ,ew [war_number]

### Pokegame  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`>attack`  |  Attacks a target with the given move. Use `>movelist` to see a list of moves your type can use. |  `>attack "vine whip" @someguy`
`>movelist`, `>ml`  |  Lists the moves you are able to use |  `>ml`
`>heal`  |  Heals someone. Revives those who fainted. Costs a NadekoFlower |  `>heal @someone`
`>type`  |  Get the poketype of the target. |  `>type @someone`
`>settype`  |  Set your poketype. Costs a NadekoFlower. |  `>settype fire`

### Translator  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~translate`, `~trans`  |  Translates from>to text. From the given language to the destiation language. |  `~trans en>fr Hello`
`~translangs`  |  List the valid languages for translation. |  `{Prefix}translangs` or `{Prefix}translangs language`

### Customreactions  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`\o\`  |  Custom reaction. | \o\
`/o/`  |  Custom reaction. | /o/
`moveto`  |  Custom reaction. | moveto
`comeatmebro`  |  Custom reaction. | comeatmebro
`e`  |  Custom reaction. | e
`@BotName insult`, `<@!116275390695079945> insult`  |  Custom reaction. | %mention% insult
`@BotName praise`, `<@!116275390695079945> praise`  |  Custom reaction. | %mention% praise
`@BotName pat`, `<@!116275390695079945> pat`  |  Custom reaction. | %mention% pat
`@BotName cry`, `<@!116275390695079945> cry`  |  Custom reaction. | %mention% cry
`@BotName are you real?`, `<@!116275390695079945> are you real?`  |  Custom reaction. | %mention% are you real?
`@BotName are you there?`, `<@!116275390695079945> are you there?`  |  Custom reaction. | %mention% are you there?
`@BotName draw`, `<@!116275390695079945> draw`  |  Custom reaction. | %mention% draw
`@BotName bb`, `<@!116275390695079945> bb`  |  Custom reaction. | %mention% bb
`@BotName call`, `<@!116275390695079945> call`  |  Custom reaction. | %mention% call
`@BotName disguise`, `<@!116275390695079945> disguise`  |  Custom reaction. | %mention% disguise
`~hentai`  |  Custom reaction. | ~hentai

### Trello  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`trello bind`  |  Bind a trello bot to a single channel. You will receive notifications from your board when something is added or edited. |  `trello bind [board_id]`
`trello unbind`  |  Unbinds a bot from the channel and board.
`trello lists`, `trello list`  |  Lists all lists yo ;)
`trello cards`  |  Lists all cards from the supplied list. You can supply either a name or an index. |  `trello cards index`
