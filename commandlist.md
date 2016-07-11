######For more information and how to setup your own NadekoBot, go to: **http://github.com/Kwoth/NadekoBot/**
######You can donate on paypal: `nadekodiscordbot@gmail.com` or Bitcoin `17MZz1JAqME39akMLrVT4XBPffQJ2n1EPa`

#NadekoBot List Of Commands  
Version: `NadekoBot v0.9.6036.32870`
### Help  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`-h`, `-help`, `@BotName help`, `@BotName h`, `~h`  |  Either shows a help for a single command, or PMs you help link if no arguments are specified. |  '-h !m q' or just '-h' 
`-hgit`  |  Generates the commandlist.md file. **Bot Owner Only!**
`-readme`, `-guide`  |  Sends a readme and a guide links to the channel.
`-donate`, `~donate`  |  Instructions for helping the project!
`-modules`, `.modules`  |  List all bot modules.
`-commands`, `.commands`  |  List all of the bot's commands from a certain module.

### Administration  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`.grdel`  |  Toggles automatic deletion of greet and bye messages.
`.greet`  |  Toggles anouncements on the current channel when someone joins the server.
`.greetmsg`  |  Sets a new join announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current greet message. |  .greetmsg Welcome to the server, %user%.
`.bye`  |  Toggles anouncements on the current channel when someone leaves the server.
`.byemsg`  |  Sets a new leave announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current bye message. |  .byemsg %user% has left the server.
`.byepm`  |  Toggles whether the good bye messages will be sent in a PM or in the text channel.
`.greetpm`  |  Toggles whether the greet messages will be sent in a PM or in the text channel.
`.spmom`  |  Toggles whether mentions of other offline users on your server will send a pm to them.
`.logserver`  |  Toggles logging in this channel. Logs every message sent/deleted/edited on the server. **Bot Owner Only!**
`.userpresence`  |  Starts logging to this channel when someone from the server goes online/offline/idle.
`.voicepresence`  |  Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now.
`.repeatinvoke`, `.repinv`  |  Immediately shows the repeat message and restarts the timer.
`.repeat`  |  Repeat a message every X minutes. If no parameters are specified, repeat is disabled. Requires manage messages. | `.repeat 5 Hello there`
`.rotateplaying`, `.ropl`  |  Toggles rotation of playing status of the dynamic strings you specified earlier.
`.addplaying`, `.adpl`  |  Adds a specified string to the list of playing strings to rotate. Supported placeholders: %servers%, %users%, %playing%, %queued%, %trivia%
`.listplaying`, `.lipl`  |  Lists all playing statuses with their corresponding number.
`.removeplaying`, `.repl`, `.rmpl`  |  Removes a playing string on a given number.
`.slowmode`  |  Toggles slow mode. When ON, users will be able to send only 1 message every 5 seconds.
`.cleanv+t`, `.cv+t`  |  Deletes all text channels ending in `-voice` for which voicechannels are not found. **Use at your own risk.**
`.voice+text`, `.v+t`  |  Creates a text channel for each voice channel only users in that voice channel can see.If you are server owner, keep in mind you will see them all the time regardless.
`.scsc`  |  Starts an instance of cross server channel. You will get a token as a DMthat other people will use to tune in to the same instance
`.jcsc`  |  Joins current channel to an instance of cross server channel using the token.
`.lcsc`  |  Leaves Cross server channel instance from this channel
`.asar`  |  Adds a role, or list of roles separated by whitespace(use quotations for multiword roles) to the list of self-assignable roles. |  .asar Gamer
`.rsar`  |  Removes a specified role from the list of self-assignable roles.
`.lsar`  |  Lists all self-assignable roles.
`.iam`  |  Adds a role to you that you choose. Role must be on a list of self-assignable roles. |  .iam Gamer
`.iamnot`, `.iamn`  |  Removes a role to you that you choose. Role must be on a list of self-assignable roles. |  .iamn Gamer
`.addcustreact`, `.acr`  |  Add a custom reaction. Guide here: <https://github.com/Kwoth/NadekoBot/wiki/Custom-Reactions> **Bot Owner Only!**   |  .acr "hello" I love saying hello to %user%
`.listcustreact`, `.lcr`  |  Lists custom reactions (paginated with 30 commands per page). Use 'all' instead of page number to get all custom reactions DM-ed to you.  | .lcr 1
`.showcustreact`, `.scr`  |  Shows all possible responses from a single custom reaction. | .scr %mention% bb
`.editcustreact`, `.ecr`  |  Edits a custom reaction, arguments are custom reactions name, index to change, and a (multiword) message **Bot Owner Only** |  `.ecr "%mention% disguise" 2 Test 123`
`.delcustreact`, `.dcr`  |  Deletes a custom reaction with given name (and index)
`.autoassignrole`, `.aar`  |  Automaticaly assigns a specified role to every user who joins the server. Type `.aar` to disable, `.aar Role Name` to enable
`.leave`  |  Makes Nadeko leave the server. Either name or id required. |  `.leave 123123123331`
`.listincidents`, `.lin`  |  List all UNREAD incidents and flags them as read.
`.listallincidents`, `.lain`  |  Sends you a file containing all incidents and flags them as read.
`.delmsgoncmd`  |  Toggles the automatic deletion of user's successful command message to prevent chat flood. Server Manager Only.
`.restart`  |  Restarts the bot. Might not work. **Bot Owner Only**
`.setrole`, `.sr`  |  Sets a role for a given user. |  .sr @User Guest
`.removerole`, `.rr`  |  Removes a role from a given user. |  .rr @User Admin
`.renamerole`, `.renr`  |  Renames a role. Role you are renaming must be lower than bot's highest role. |  `.renr "First role" SecondRole`
`.removeallroles`, `.rar`  |  Removes all roles from a mentioned user. |  .rar @User
`.createrole`, `.cr`  |  Creates a role with a given name. |  `.r Awesome Role`
`.rolecolor`, `.rc`  |  Set a role's color to the hex or 0-255 rgb color value provided. |  `.color Admin 255 200 100` or `.color Admin ffba55`
`.ban`, `.b`  |  Bans a user by id or name with an optional message. |  .b "@some Guy" Your behaviour is toxic.
`.softban`, `.sb`  |  Bans and then unbans a user by id or name with an optional message. |  .sb "@some Guy" Your behaviour is toxic.
`.kick`, `.k`  |  Kicks a mentioned user.
`.mute`  |  Mutes mentioned user or users.
`.unmute`  |  Unmutes mentioned user or users.
`.deafen`, `.deaf`  |  Deafens mentioned user or users
`.undeafen`, `.undef`  |  Undeafens mentioned user or users
`.delvoichanl`, `.dvch`  |  Deletes a voice channel with a given name.
`.creatvoichanl`, `.cvch`  |  Creates a new voice channel with a given name.
`.deltxtchanl`, `.dtch`  |  Deletes a text channel with a given name.
`.creatxtchanl`, `.ctch`  |  Creates a new text channel with a given name.
`.settopic`, `.st`  |  Sets a topic on the current channel. |  `.st My new topic`
`.setchanlname`, `.schn`  |  Changed the name of the current channel.
`.heap`  |  Shows allocated memory - **Bot Owner Only!**
`.prune`, `.clr`  |  `.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel. |  `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X`
`.die`  |  Shuts the bot down and notifies users about the restart. **Bot Owner Only!**
`.setname`, `.newnm`  |  Give the bot a new name. **Bot Owner Only!**
`.newavatar`, `.setavatar`  |  Sets a new avatar image for the NadekoBot. Argument is a direct link to an image. **Bot Owner Only!** |  `.setavatar https://i.ytimg.com/vi/WDudkR1eTMM/maxresdefault.jpg`
`.setgame`  |  Sets the bots game. **Bot Owner Only!**
`.send`  |  Send a message to someone on a different server through the bot. **Bot Owner Only!** |  `.send serverid|u:user_id Send this to a user!` or `.send serverid|c:channel_id Send this to a channel!`
`.mentionrole`, `.menro`  |  Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission.
`.unstuck`  |  Clears the message queue. **Bot Owner Only!**
`.donators`  |  List of lovely people who donated to keep this project alive.
`.donadd`  |  Add a donator to the database.
`.announce`  |  Sends a message to all servers' general channel bot is connected to.**Bot Owner Only!** |  .announce Useless spam
`.leave`  |  Leaves a server with a supplied ID. |  `.leave 493243292839`
`.savechat`  |  Saves a number of messages to a text file and sends it to you. **Bot Owner Only** |  `.chatsave 150`

### Utility  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`.remind`  |  Sends a message to you or a channel after certain amount of time. First argument is me/here/'channelname'. Second argument is time in a descending order (mo>w>d>h>m) example: 1w5d3h10m. Third argument is a (multiword)message.  |  `.remind me 1d5h Do something` or `.remind #general Start now!`
`.remindmsg`  |  Sets message for when the remind is triggered.  Available placeholders are %user% - user who ran the command, %message% - Message specified in the remind, %target% - target channel of the remind. **Bot Owner Only!**
`.serverinfo`, `.sinfo`  |  Shows info about the server the bot is on. If no channel is supplied, it defaults to current one. | .sinfo Some Server
`.channelinfo`, `.cinfo`  |  Shows info about the channel. If no channel is supplied, it defaults to current one. | .cinfo #some-channel
`.userinfo`, `.uinfo`  |  Shows info about the user. If no user is supplied, it defaults a user running the command. | .uinfo @SomeUser
`.whoplays`  |  Shows a list of users who are playing the specified game.
`.inrole`  |  Lists every person from the provided role or roles (separated by a ',') on this server.
`.checkmyperms`  |  Checks your userspecific permissions on this channel.
`.stats`  |  Shows some basic stats for Nadeko.
`.dysyd`  |  Shows some basic stats for Nadeko.
`.userid`, `.uid`  |  Shows user ID.
`.channelid`, `.cid`  |  Shows current channel ID.
`.serverid`, `.sid`  |  Shows current server ID.
`.roles`  |  List all roles on this server or a single user if specified.

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
`;permrole`, `;pr`  |  Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'.
`;rolepermscopy`, `;rpc`  |  Copies BOT PERMISSIONS (not discord permissions) from one role to another. | `;rpc Some Role ~ Some other role`
`;chnlpermscopy`, `;cpc`  |  Copies BOT PERMISSIONS (not discord permissions) from one channel to another. | `;cpc Some Channel ~ Some other channel`
`;usrpermscopy`, `;upc`  |  Copies BOT PERMISSIONS (not discord permissions) from one role to another. | `;upc @SomeUser ~ @SomeOtherUser`
`;verbose`, `;v`  |  Sets whether to show when a command/module is blocked. |  ;verbose true
`;srvrperms`, `;sp`  |  Shows banned permissions for this server.
`;roleperms`, `;rp`  |  Shows banned permissions for a certain role. No argument means for everyone. |  ;rp AwesomeRole
`;chnlperms`, `;cp`  |  Shows banned permissions for a certain channel. No argument means for this channel. |  ;cp #dev
`;userperms`, `;up`  |  Shows banned permissions for a certain user. No argument means for yourself. |  ;up Kwoth
`;srvrmdl`, `;sm`  |  Sets a module's permission at the server level. |  ;sm "module name" enable
`;srvrcmd`, `;sc`  |  Sets a command's permission at the server level. |  ;sc "command name" disable
`;rolemdl`, `;rm`  |  Sets a module's permission at the role level. |  ;rm "module name" enable MyRole
`;rolecmd`, `;rc`  |  Sets a command's permission at the role level. |  ;rc "command name" disable MyRole
`;chnlmdl`, `;cm`  |  Sets a module's permission at the channel level. |  ;cm "module name" enable SomeChannel
`;chnlcmd`, `;cc`  |  Sets a command's permission at the channel level. |  ;cc "command name" enable SomeChannel
`;usrmdl`, `;um`  |  Sets a module's permission at the user level. |  ;um "module name" enable SomeUsername
`;usrcmd`, `;uc`  |  Sets a command's permission at the user level. |  ;uc "command name" enable SomeUsername
`;allsrvrmdls`, `;asm`  |  Sets permissions for all modules at the server level. |  ;asm [enable/disable]
`;allsrvrcmds`, `;asc`  |  Sets permissions for all commands from a certain module at the server level. |  ;asc "module name" [enable/disable]
`;allchnlmdls`, `;acm`  |  Sets permissions for all modules at the channel level. |  ;acm [enable/disable] SomeChannel
`;allchnlcmds`, `;acc`  |  Sets permissions for all commands from a certain module at the channel level. |  ;acc "module name" [enable/disable] SomeChannel
`;allrolemdls`, `;arm`  |  Sets permissions for all modules at the role level. |  ;arm [enable/disable] MyRole
`;allrolecmds`, `;arc`  |  Sets permissions for all commands from a certain module at the role level. |  ;arc "module name" [enable/disable] MyRole
`;ubl`  |  Blacklists a mentioned user. |  ;ubl [user_mention]
`;uubl`  |  Unblacklists a mentioned user. |  ;uubl [user_mention]
`;cbl`  |  Blacklists a mentioned channel (#general for example). |  ;cbl #some_channel
`;cubl`  |  Unblacklists a mentioned channel (#general for example). |  ;cubl #some_channel
`;sbl`  |  Blacklists a server by a name or id (#general for example). **BOT OWNER ONLY** |  ;sbl [servername/serverid]

### Conversations  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`..`  |  Adds a new quote with the specified name (single word) and message (no limit). |  .. abc My message
`...`  |  Shows a random quote with a specified name. |  .. abc
`..qdel`, `..quotedelete`  |  Deletes all quotes with the specified keyword. You have to either be bot owner or the creator of the quote to delete it. |  `..qdel abc`
`@BotName rip`  |  Shows a grave image of someone with a start year |  @NadekoBot rip @Someone 2000
`@BotName die`  |  Works only for the owner. Shuts the bot down.
`@BotName do you love me`  |  Replies with positive answer only to the bot owner.
`@BotName how are you`, `@BotName how are you?`  |  Replies positive only if bot owner is online.
`@BotName fire`  |  Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire. |  @NadekoBot fire [x]
`@BotName slm`  |  Shows the message where you were last mentioned in this channel (checks last 10k messages)
`@BotName dump`  |  Dumps all of the invites it can to dump.txt.** Owner Only.**
`@BotName ab`  |  Try to get 'abalabahaha'

### Gambling  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`$draw`  |  Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck. |  $draw [x]
`$shuffle`, `$sh`  |  Reshuffles all cards back into the deck.
`$flip`  |  Flips coin(s) - heads or tails, and shows an image. |  `$flip` or `$flip 3`
`$roll`  |  Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. |  $roll or $roll 7 or $roll 3d5
`$nroll`  |  Rolls in a given range. |  `$nroll 5` (rolls 0-5) or `$nroll 5-15`
`$raffle`  |  Prints a name and ID of a random user from the online list from the (optional) role.
`$$$`  |  Check how much NadekoFlowers a person has. (Defaults to yourself) | `$$$` or `$$$ @Someone`
`$give`  |  Give someone a certain amount of NadekoFlowers
`$award`  |  Gives someone a certain amount of flowers. **Bot Owner Only!** |  `$award 100 @person`
`$take`  |  Takes a certain amount of flowers from someone. **Bot Owner Only!**
`$leaderboard`, `$lb`  |  

### Games  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`>t`  |  Starts a game of trivia. You can add nohint to prevent hints.First player to get to 10 points wins by default. You can specify a different number. 30 seconds per question. | `>t nohint` or `>t 5 nohint`
`>tl`  |  Shows a current trivia leaderboard.
`>tq`  |  Quits current trivia after current question.
`>typestart`  |  Starts a typing contest.
`>typestop`  |  Stops a typing contest on the current channel.
`>typeadd`  |  Adds a new article to the typing contest. Owner only.
`>poll`  |  Creates a poll, only person who has manage server permission can do it. |  >poll Question?;Answer1;Answ 2;A_3
`>pollend`  |  Stops active poll on this server and prints the results in this channel.
`>pick`  |  Picks a flower planted in this channel.
`>plant`  |  Spend a flower to plant it in this channel. (If bot is restarted or crashes, flower will be lost)
`>gencurrency`, `>gc`  |  Toggles currency generation on this channel. Every posted message will have 2% chance to spawn a NadekoFlower. Optional parameter cooldown time in minutes, 5 minutes by default. Requires Manage Messages permission. |  `>gc` or `>gc 60`
`>leet`  |  Converts a text to leetspeak with 6 (1-6) severity levels |  >leet 3 Hello
`>choose`  |  Chooses a thing from a list of things |  >choose Get up;Sleep;Sleep more
`>8ball`  |  Ask the 8ball a yes/no question.
`>rps`  |  Play a game of rocket paperclip scissors with Nadeko. |  >rps scissors
`>linux`  |  Prints a customizable Linux interjection |  `>linux Spyware Windows`

### Music  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`!m next`, `!m n`, `!m skip`  |  Goes to the next song in the queue. You have to be in the same voice channel as the bot. |  `!m n`
`!m stop`, `!m s`  |  Stops the music and clears the playlist. Stays in the channel. |  `!m s`
`!m destroy`, `!m d`  |  Completely stops the music and unbinds the bot from the channel. (may cause weird behaviour) |  `!m d`
`!m pause`, `!m p`  |  Pauses or Unpauses the song. |  `!m p`
`!m queue`, `!m q`, `!m yq`  |  Queue a song using keywords or a link. Bot will join your voice channel.**You must be in a voice channel**. |  `!m q Dream Of Venice`
`!m soundcloudqueue`, `!m sq`  |  Queue a soundcloud song using keywords. Bot will join your voice channel.**You must be in a voice channel**. |  `!m sq Dream Of Venice`
`!m listqueue`, `!m lq`  |  Lists 15 currently queued songs per page. Default page is 1. |  `!m lq` or `!m lq 2`
`!m nowplaying`, `!m np`  |  Shows the song currently playing. |  `!m np`
`!m volume`, `!m vol`  |  Sets the music volume 0-100% |  `!m vol 50`
`!m defvol`, `!m dv`  |  Sets the default music volume when music playback is started (0-100). Persists through restarts. |  `!m dv 80`
`!m mute`, `!m min`  |  Sets the music volume to 0% |  `!m min`
`!m max`  |  Sets the music volume to 100%. |  `!m max`
`!m half`  |  Sets the music volume to 50%. |  `!m half`
`!m shuffle`, `!m sh`  |  Shuffles the current playlist. |  `!m sh`
`!m playlist`, `!m pl`  |  Queues up to 500 songs from a youtube playlist specified by a link, or keywords. |  `!m pl playlist link or name`
`!m soundcloudpl`, `!m scpl`  |  Queue a soundcloud playlist using a link. |  `!m scpl https://soundcloud.com/saratology/sets/symphony`
`!m localplaylst`, `!m lopl`  |  Queues all songs from a directory. **Bot Owner Only!** |  `!m lopl C:/music/classical`
`!m radio`, `!m ra`  |  Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf |  `!m ra radio link here`
`!m local`, `!m lo`  |  Queues a local file by specifying a full path. **Bot Owner Only!** |  `!m lo C:/music/mysong.mp3`
`!m move`, `!m mv`  |  Moves the bot to your voice channel. (works only if music is already playing) |  `!m mv`
`!m remove`, `!m rm`  |  Remove a song by its # in the queue, or 'all' to remove whole queue. |  `!m rm 5`
`!m movesong`, `!m ms`  |  Moves a song from one position to another. |  `!m ms` 5>3
`!m setmaxqueue`, `!m smq`  |  Sets a maximum queue size. Supply 0 or no argument to have no limit.  |  `!m smq` 50 or `!m smq`
`!m cleanup`  |  Cleans up hanging voice connections. **Bot Owner Only!** |  `!m cleanup`
`!m reptcursong`, `!m rcs`  |  Toggles repeat of current song. |  `!m rcs`
`!m rpeatplaylst`, `!m rpl`  |  Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue). |  `!m rpl`
`!m save`  |  Saves a playlist under a certain name. Name must be no longer than 20 characters and mustn't contain dashes. |  `!m save classical1`
`!m load`  |  Loads a playlist under a certain name.  |  `!m load classical-1`
`!m playlists`, `!m pls`  |  Lists all playlists. Paginated. 20 per page. Default page is 0. | `!m pls 1`
`!m deleteplaylist`, `!m delpls`  |  Deletes a saved playlist. Only if you made it or if you are the bot owner. |  `!m delpls animu-5`
`!m goto`  |  Goes to a specific time in seconds in a song.
`!m getlink`, `!m gl`  |  Shows a link to the currently playing song.
`!m autoplay`, `!m ap`  |  Toggles autoplay - When the song is finished, automatically queue a related youtube song. (Works only for youtube songs and when queue is empty)

### Searches  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~lolchamp`  |  Shows League Of Legends champion statistics. If there are spaces/apostrophes or in the name - omit them. Optional second parameter is a role. | ~lolchamp Riven or ~lolchamp Annie sup
`~lolban`  |  Shows top 6 banned champions ordered by ban rate. Ban these champions and you will be Plat 5 in no time.
`~hitbox`, `~hb`  |  Notifies this channel when a certain user starts streaming. |  ~hitbox SomeStreamer
`~twitch`, `~tw`  |  Notifies this channel when a certain user starts streaming. |  ~twitch SomeStreamer
`~beam`, `~bm`  |  Notifies this channel when a certain user starts streaming. |  ~beam SomeStreamer
`~checkhitbox`, `~chhb`  |  Checks if a certain user is streaming on the hitbox platform. |  ~chhb SomeStreamer
`~checktwitch`, `~chtw`  |  Checks if a certain user is streaming on the twitch platform. |  ~chtw SomeStreamer
`~checkbeam`, `~chbm`  |  Checks if a certain user is streaming on the beam platform. |  ~chbm SomeStreamer
`~removestream`, `~rms`  |  Removes notifications of a certain streamer on this channel. |  ~rms SomeGuy
`~liststreams`, `~ls`  |  Lists all streams you are following on this server. |  ~ls
`~convert`  |  Convert quantities from>to. Like `~convert m>km 1000`
`~convertlist`  |  List of the convertable dimensions and currencies.
`~wowjoke`  |  Get one of Kwoth's penultimate WoW jokes.
`~calculate`, `~calc`  |  Evaluate a mathematical expression. |  ~calc 1+1
`~osu`  |  Shows osu stats for a player. |  `~osu Name` or `~osu Name taiko`
`~osu b`  |  Shows information about an osu beatmap. | ~osu b https://osu.ppy.sh/s/127712
`~osu top5`  |  Displays a user's top 5 plays.  | ~osu top5 Name
`~pokemon`, `~poke`  |  Searches for a pokemon.
`~pokemonability`, `~pokeab`  |  Searches for a pokemon ability.
`~memelist`  |  Pulls a list of memes you can use with `~memegen` from http://memegen.link/templates/
`~memegen`  |  Generates a meme from memelist with top and bottom text. |  `~memegen biw "gets iced coffee" "in the winter"`
`~we`  |  Shows weather data for a specified city and a country. BOTH ARE REQUIRED. Use country abbrevations. |  ~we Moscow RF
`~yt`  |  Searches youtubes and shows the first result
`~ani`, `~anime`, `~aq`  |  Queries anilist for an anime and shows the first result.
`~imdb`  |  Queries imdb for movies or series, show first result.
`~mang`, `~manga`, `~mq`  |  Queries anilist for a manga and shows the first result.
`~randomcat`, `~meow`  |  Shows a random cat image.
`~i`  |  Pulls the first image found using a search parameter. Use ~ir for different results. |  ~i cute kitten
`~ir`  |  Pulls a random image using a search parameter. |  ~ir cute kitten
`~lmgtfy`  |  Google something for an idiot.
`~hs`  |  Searches for a Hearthstone card and shows its image. Takes a while to complete. | ~hs Ysera
`~ud`  |  Searches Urban Dictionary for a word. | ~ud Pineapple
`~#`  |  Searches Tagdef.com for a hashtag. | ~# ff
`~quote`  |  Shows a random quote.
`~catfact`  |  Shows a random catfact from <http://catfacts-api.appspot.com/api/facts>
`~yomama`, `~ym`  |  Shows a random joke from <http://api.yomomma.info/>
`~randjoke`, `~rj`  |  Shows a random joke from <http://tambal.azurewebsites.net/joke/random>
`~chucknorris`, `~cn`  |  Shows a random chucknorris joke from <http://tambal.azurewebsites.net/joke/random>
`~magicitem`, `~mi`  |  Shows a random magicitem from <https://1d4chan.org/wiki/List_of_/tg/%27s_magic_items>
`~revav`  |  Returns a google reverse image search for someone's avatar.
`~revimg`  |  Returns a google reverse image search for an image from a link.
`~safebooru`  |  Shows a random image from safebooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~safebooru yuri+kissing
`~wiki`  |  Gives you back a wikipedia link
`~clr`  |  Shows you what color corresponds to that hex. |  `~clr 00ff00`
`~videocall`  |  Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message.
`~av`, `~avatar`  |  Shows a mentioned person's avatar. |  ~av @X

### NSFW  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~hentai`  |  Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~hentai yuri+kissing
`~danbooru`  |  Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~danbooru yuri+kissing
`~gelbooru`  |  Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~gelbooru yuri+kissing
`~rule34`  |  Shows a random image from rule34.xx with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~rule34 yuri+kissing
`~e621`  |  Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags. |  ~e621 yuri kissing
`~cp`  |  We all know where this will lead you to.
`~boobs`  |  Real adult content.
`~butts`, `~ass`, `~butt`  |  Real adult content.

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
`,unclaim`, `,uncall`, `,uc`  |  Removes your claim from a certain war. Optional second argument denotes a person in whos place to unclaim |  ,uc [war_number] [optional_other_name]
`,endwar`, `,ew`  |  Ends the war with a given index. | ,ew [war_number]

### Pokegame  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`>attack`  |  Attacks a target with the given move. Use `>movelist` to see a list of moves your type can use. |  `>attack "vine whip" @someguy`
`>movelist`, `>ml`  |  Lists the moves you are able to use
`>heal`  |  Heals someone. Revives those that fainted. Costs a NadekoFlower  | >revive @someone
`>type`  |  Get the poketype of the target. |  >type @someone
`>settype`  |  Set your poketype. Costs a NadekoFlower. |  >settype fire

### Translator  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`~translate`, `~trans`  |  Translates from>to text. From the given language to the destiation language. |  ~trans en>fr Hello
`~translangs`  |  List the valid languages for translation.

### Customreactions  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`\o\`  |  Custom reaction. | \o\
`/o/`  |  Custom reaction. | /o/
`moveto`  |  Custom reaction. | moveto
`comeatmebro`  |  Custom reaction. | comeatmebro
`e`  |  Custom reaction. | e
`@BotName insult`, `<@!119777021319577610> insult`  |  Custom reaction. | %mention% insult
`@BotName praise`, `<@!119777021319577610> praise`  |  Custom reaction. | %mention% praise
`@BotName pat`, `<@!119777021319577610> pat`  |  Custom reaction. | %mention% pat
`@BotName cry`, `<@!119777021319577610> cry`  |  Custom reaction. | %mention% cry
`@BotName are you real?`, `<@!119777021319577610> are you real?`  |  Custom reaction. | %mention% are you real?
`@BotName are you there?`, `<@!119777021319577610> are you there?`  |  Custom reaction. | %mention% are you there?
`@BotName draw`, `<@!119777021319577610> draw`  |  Custom reaction. | %mention% draw
`@BotName bb`, `<@!119777021319577610> bb`  |  Custom reaction. | %mention% bb
`@BotName call`, `<@!119777021319577610> call`  |  Custom reaction. | %mention% call
`@BotName disguise`, `<@!119777021319577610> disguise`  |  Custom reaction. | %mention% disguise

### Trello  
Command and aliases |  Description |  Usage
----------------|--------------|-------
`trello bind`  |  Bind a trello bot to a single channel. You will receive notifications from your board when something is added or edited. |  bind [board_id]
`trello unbind`  |  Unbinds a bot from the channel and board.
`trello lists`, `trello list`  |  Lists all lists yo ;)
`trello cards`  |  Lists all cards from the supplied list. You can supply either a name or an index.
