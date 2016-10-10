
### Administration  
Command and aliases | Description | Usage
----------------|--------------|-------
`.restart`  | `.restart` | Restarts the bot. Might not work. **Bot owner only.**
`.delmsgoncmd`  | `.delmsgoncmd` | Toggles the automatic deletion of user's successful command message to prevent chat flood. **Server Manager Only.** **Requires Administrator server permission.**
`.setrole` `.sr` | `.sr @User Guest` | Sets a role for a given user.   **Requires ManageRoles server permission.**
`.removerole` `.rr` | `.rr @User Admin` | Removes a role from a given user.   **Requires ManageRoles server permission.**
`.renamerole` `.renr` | `.renr "First role" SecondRole` | Renames a role. Roles you are renaming must be lower than bot's highest role. **Manage Roles Permissions.** **Requires ManageRoles server permission.**
`.removeallroles` `.rar` | `.rar @User` | Removes all roles from a mentioned user.   **Requires ManageRoles server permission.**
`.createrole` `.cr` | `.cr Awesome Role` | Creates a role with a given name.   **Requires ManageRoles server permission.**
`.rolecolor` `.rc` | `.rc Admin 255 200 100` or `.rc Admin ffba55` | Set a role's color to the hex or 0-255 rgb color value provided.   **Requires ManageRoles server permission.**
`.ban` `.b` | `.b "@some Guy" Your behaviour is toxic.` | Bans a user by id or name with an optional message. **Requires BanMembers server permission.**
`.softban` `.sb` | `.sb "@some Guy" Your behaviour is toxic.` | Bans and then unbans a user by id or name with an optional message.   **Requires BanMembers server permission.**
`.kick` `.k` | `.k "@some Guy" Your behaviour is toxic.` | Kicks a mentioned user.   **Requires KickMembers server permission.**
`.mute`  | `.mute @Someone` | Mutes a mentioned user in a voice channel. **Requires MuteMembers server permission.**
`.unmute`  | `.unmute "@Someguy"` or `.unmute "@Someguy" "@Someguy"` | Unmutes mentioned user or users.   **Requires MuteMembers server permission.**
`.deafen` `.deaf` | `.deaf "@Someguy"` or `.deaf "@Someguy" "@Someguy"` | Deafens mentioned user or users.   **Requires DeafenMembers server permission.**
`.undeafen` `.undef` | `.undef "@Someguy"` or `.undef "@Someguy" "@Someguy"` | Undeafens mentioned user or users.   **Requires DeafenMembers server permission.**
`.delvoichanl` `.dvch` | `.dvch VoiceChannelName` | Deletes a voice channel with a given name.   **Requires ManageChannels server permission.**
`.creatvoichanl` `.cvch` | `.cvch VoiceChannelName` | Creates a new voice channel with a given name.   **Requires ManageChannels server permission.**
`.deltxtchanl` `.dtch` | `.dtch TextChannelName` | Deletes a text channel with a given name.   **Requires ManageChannels server permission.**
`.creatxtchanl` `.ctch` | `.ctch TextChannelName` | Creates a new text channel with a given name.   **Requires ManageChannels server permission.**
`.settopic` `.st` | `.st My new topic` | Sets a topic on the current channel.   **Requires ManageChannels server permission.**
`.setchanlname` `.schn` | `.schn NewName` | Changed the name of the current channel.   **Requires ManageChannels server permission.**
`.prune` `.clr` | `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X` | `.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel.   
`.prune` `.clr` | `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X` | `.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel.   **Requires ManageMessages server permission.**
`.prune` `.clr` | `.prune` or `.prune 5` or `.prune @Someone` or `.prune @Someone X` | `.prune` removes all nadeko's messages in the last 100 messages.`.prune X` removes last X messages from the channel (up to 100)`.prune @Someone` removes all Someone's messages in the last 100 messages.`.prune @Someone X` removes last X 'Someone's' messages in the channel.   **Requires ManageMessages server permission.**
`.die`  | `@NadekoBot die` | Works only for the owner. Shuts the bot down. **Bot owner only.**
`.setname` `.newnm` | `.newnm BotName` | Give the bot a new name.   **Bot owner only.**
`.setavatar` `.setav` | `.setav http://i.imgur.com/xTG3a1I.jpg` | Sets a new avatar image for the NadekoBot. Argument is a direct link to an image.   **Bot owner only.**
`.setgame`  | `.setgame Playing with kwoth` | Sets the bots game.   **Bot owner only.**
`.send`  | `.send sid` | Send a message to someone on a different server through the bot.   **Bot owner only.**
`.announce`  | `.announce Useless spam` | Sends a message to all servers' general channel bot is connected to. **Bot owner only.**
`.savechat`  | `.savechat 150` | Saves a number of messages to a text file and sends it to you. **Bot owner only.**
`.mentionrole` `.menro` | `.menro RoleName` | Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission. **Requires MentionEveryone server permission.**
`.donators`  | `.donators` | List of lovely people who donated to keep this project alive. 
`.donadd`  | `.donadd Donate Amount` | Add a donator to the database. **Kwoth Only** **Bot owner only.**
`.autoassignrole` `.aar` | `.aar` to disable, `.aar Role Name` to enable | Automaticaly assigns a specified role to every user who joins the server.  **Requires ManageRoles server permission.**
`.scsc`  | `.scsc` | Starts an instance of cross server channel. You will get a token as a DM that other people will use to tune in to the same instance. **Bot owner only.**
`.jcsc`  | `.jcsc` | Joins current channel to an instance of cross server channel using the token.  **Requires ManageServer server permission.**
`.lcsc`  | `.lcsc` | Leaves Cross server channel instance from this channel.  **Requires ManageServer server permission.**
`.logserver`  | `.logserver` | Logs server activity in this channel. **Requires Administrator server permission.** **Bot owner only.**
`.logignore`  | `.logignore` | Toggles whether the .logserver command ignores this channel. Useful if you have hidden admin channel and public log channel. **Requires Administrator server permission.** **Bot owner only.**
`.userpresence`  | `.userpresence` | Starts logging to this channel when someone from the server goes online/offline/idle.  **Requires Administrator server permission.**
`.voicepresence`  | `.voicerpresence` | Toggles logging to this channel whenever someone joins or leaves a voice channel you are in right now.  **Requires Administrator server permission.**
`.repeatinvoke` `.repinv` | `.repinv` | Immediately shows the repeat message and restarts the timer.   **Requires ManageMessages server permission.**
`.repeat`  | `.repeat 5 Hello there` | Repeat a message every X minutes. If no parameters are specified, repeat is disabled. 
`.repeat`  | `.repeat 5 Hello there` | Repeat a message every X minutes. If no parameters are specified, repeat is disabled. 
`.migratedata`  | `.migratedata` | Migrate data from old bot configuration **Bot owner only.**
`.rotateplaying` `.ropl` | `.ropl` | Toggles rotation of playing status of the dynamic strings you specified earlier. **Bot owner only.**
`.addplaying` `.adpl` | `.adpl` | Adds a specified string to the list of playing strings to rotate. Supported placeholders: %servers%, %users%, %playing%, %queued% **Bot owner only.**
`.listplaying` `.lipl` | `.lipl` | Lists all playing statuses with their corresponding number. **Bot owner only.**
`.removeplaying` `.rmlp` `.repl` | `.rmpl` | Removes a playing string on a given number.  **Bot owner only.**
`.slowmode`  | `.slowmode` | Toggles slow mode. When ON, users will be able to send only 1 message every 5 seconds.   
`.asar`  | `.asar Gamer` | Adds a role, or list of roles separated by whitespace(use quotations for multiword roles) to the list of self-assignable roles. **Requires ManageRoles server permission.**
`.rsar`  | `.rsar` | Removes a specified role from the list of self-assignable roles. **Requires ManageRoles server permission.**
`.lsar`  | `.lsar` | Lists all self-assignable roles. 
`.togglexclsar` `.tesar` | `.tesar` | toggle whether the self-assigned roles should be exclusive **Requires ManageRoles server permission.**
`.iam`  | `.iam Gamer` | Adds a role to you that you choose. Role must be on a list of self-assignable roles. 
`.iamnot` `.iamn` | `.iamn Gamer` | Removes a role to you that you choose. Role must be on a list of self-assignable roles. 
`.leave`  | `.leave 123123123331` | Makes Nadeko leave the server. Either name or id required.   **Bot owner only.**
`.greetdel`  | `.greetdel` | Toggles automatic deletion of greet messages.  **Requires ManageServer server permission.**
`.greet`  | `.greet` | Toggles anouncements on the current channel when someone joins the server.  **Requires ManageServer server permission.**
`.greetmsg`  | `.greetmsg Welcome, %user%.` | Sets a new join announcement message which will be shown in the server's channel. Type %user% if you want to mention the new member. Using it with no message will show the current greet message.  **Requires ManageServer server permission.**
`.greetdm`  | `.greetdm` | Toggles whether the greet messages will be sent in a DM (This is separate from greet - you can have both, any or neither enabled).  **Requires ManageServer server permission.**
`.greetdmmsg`  | `.greetdmmsg Welcome to the server, %user%`. | Sets a new join announcement message which will be sent to the user who joined. Type %user% if you want to mention the new member. Using it with no message will show the current DM greet message.  **Requires ManageServer server permission.**
`.bye`  | `.bye` | Toggles anouncements on the current channel when someone leaves the server. **Requires ManageServer server permission.**
`.byemsg`  | `.byemsg %user% has left.` | Sets a new leave announcement message. Type %user% if you want to mention the new member. Using it with no message will show the current bye message.  **Requires ManageServer server permission.**
`.byedel`  | `.byedel` | Toggles automatic deletion of bye messages.  **Requires ManageServer server permission.**
`.voice+text` `.v+t` | `.voice+text` | Creates a text channel for each voice channel only users in that voice channel can see.If you are server owner, keep in mind you will see them all the time regardless.   **Requires ManageRoles server permission.** **Requires ManageChannels server permission.**
`.cleanvplust` `.cv+t` | `.cleanv+t` | Deletes all text channels ending in `-voice` for which voicechannels are not found. **Use at your own risk. Needs Manage Roles and Manage Channels Permissions.** **Requires ManageChannels server permission.** **Requires ManageRoles server permission.**

### Permissions  
Command and aliases | Description | Usage
----------------|--------------|-------
`;verbose` `;v` | `;verbose true` | Sets whether to show when a command/module is blocked. 
`;permrole` `;pr` | `;pr role` | Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'. 
`;listperms` `;lp` | `;lp` or `;lp 3` | Lists whole permission chain with their indexes. You can specify optional page number if there are a lot of permissions 
`;removeperm` `;rp` | `;rp 1` | Removes a permission from a given position 
`;moveperm` `;mp` | `;mp 2 4` | Moves permission from one position to another. 
`;srvrcmd` `;sc` | `;sc "command name" disable` | Sets a command's permission at the server level. 
`;srvrmdl` `;sm` | `;sm "module name" enable` | Sets a module's permission at the server level. 
`;usrcmd` `;uc` | `;uc "command name" enable SomeUsername` | Sets a command's permission at the user level. 
`;usrmdl` `;um` | `;um "module name" enable SomeUsername` | Sets a module's permission at the user level. 
`;rolecmd` `;rc` | `;rc "command name" disable MyRole` | Sets a command's permission at the role level. 
`;rolemdl` `;rm` | `;rm "module name" enable MyRole` | Sets a module's permission at the role level. 
`;chnlcmd` `;cc` | `;cc "command name" enable SomeChannel` | Sets a command's permission at the channel level. 
`;chnlmdl` `;cm` | `;cm "module name" enable SomeChannel` | Sets a module's permission at the channel level. 
`;allchnlmdls` `;acm` | `;acm enable #SomeChannel` | Enable or disable all modules in a specified channel. 
`;allrolemdls` `;arm` | `;arm [enable/disable] MyRole` | Enable or disable all modules for a specific role. 
`;allusrmdls` `;aum` | `;aum enable @someone` | Enable or disable all modules for a specific user. 
`;allsrvrmdls` `;asm` | `;asm [enable/disable]` | Enable or disable all modules for your server. 
`;ubl`  | `;ubl add @SomeUser` or `;ubl rem 12312312313` | Either [add]s or [rem]oves a user specified by a mention or ID from a blacklist. **Bot owner only.**
`;ubl`  | `;ubl add @SomeUser` or `;ubl rem 12312312313` | Either [add]s or [rem]oves a user specified by a mention or ID from a blacklist. **Bot owner only.**
`;cbl`  | `;cbl rem 12312312312` | Either [add]s or [rem]oves a channel specified by an ID from a blacklist. **Bot owner only.**
`;sbl`  | `;sbl add 12312321312` or `;sbl rem SomeTrashServer` | Either [add]s or [rem]oves a server specified by a Name or ID from a blacklist. **Bot owner only.**
`;sbl`  | `;sbl add 12312321312` or `;sbl rem SomeTrashServer` | Either [add]s or [rem]oves a server specified by a Name or ID from a blacklist. **Bot owner only.**
`;cmdcooldown` `;cmdcd` | `;cmdcd "some cmd" 5` | Sets a cooldown per user for a command. Set 0 to clear. 
`;allcmdcooldowns` `;acmdcds` | `;acmdcds` | Shows a list of all commands and their respective cooldowns. 
`;srvrfilterinv` `;sfi` | `;sfi disable` | Enables or disables automatic deleting of invites on the server. 
`;chnlfilterinv` `;cfi` | `;cfi enable #general-chat` | Enables or disables automatic deleting of invites on the channel.If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once. 
`;srvrfilterwords` `;sfw` | `;sfw disable` | Enables or disables automatic deleting of messages containing forbidden words on the server. 
`;chnlfilterwords` `;cfw` | `;cfw enable #general-chat` | Enables or disables automatic deleting of messages containing banned words on the channel.If no channel supplied, it will default to current one. Use ALL to apply to all existing channels at once. 
`;fw`  | `;fw poop` | Adds or removes (if it exists) a word from the list of filtered words 
`;lstfilterwords` `;lfw` | `;lfw` | Shows a list of filtered words 

### Utility  
Command and aliases | Description | Usage
----------------|--------------|-------
`.whosplaying`  | `.whoplays Overwatch` | Shows a list of users who are playing the specified game. 
`.inrole`  | `.inrole Role` | Lists every person from the provided role or roles (separated by a ',') on this server. If the list is too long for 1 message, you must have Manage Messages permission. 
`.checkmyperms`  | `.checkmyperms` | Checks your userspecific permissions on this channel. 
`.userid` `.uid` | `.uid` or `.uid "@SomeGuy"` | Shows user ID. 
`.channelid` `.cid` | `.cid` | Shows current channel ID. 
`.serverid` `.sid` | `.sid` | Shows current server ID. 
`.roles`  | `.roles` | List all roles on this server or a single user if specified. 
`.channeltopic` `.ct` | `.ct` | Sends current channel's topic as a message. 
`.stats`  | `.stats` | Shows some basic stats for Nadeko. 
`.showemojis` `.se` | `.se A message full of SPECIALemojis` | Shows a name and a link to every SPECIAL emoji in the message. 
`.serverinfo` `.sinfo` | `.sinfo Some Server` | Shows info about the server the bot is on. If no channel is supplied, it defaults to current one. 
`.channelinfo` `.cinfo` | `.cinfo #some-channel` | Shows info about the channel. If no channel is supplied, it defaults to current one. 
`.userinfo` `.uinfo` | `.uinfo @SomeUser` | Shows info about the user. If no user is supplied, it defaults a user running the command. 
`...`  | `... abc` | Shows a random quote with a specified name. 
`..`  | `.. sayhi Hi` | Adds a new quote with the specified name and message. 
`.deletequote` `.delq` | `.delq abc` | Deletes all quotes with the specified keyword. You have to either be bot owner or the creator of the quote to delete it. 
`.delallq` `.daq` | `.delallq kek` | Deletes all quotes on a specified keyword. 
`.remind`  | `.remind me 1d5h Do something` or `.remind #general Start now!` | Sends a message to you or a channel after certain amount of time. First argument is me/here/'channelname'. Second argument is time in a descending order (mo>w>d>h>m) example: 1w5d3h10m. Third argument is a (multiword)message. 
`.remindtemplate`  | `.remindtemplate %user%, you gotta do %message%!` | Sets message for when the remind is triggered.  Available placeholders are %user% - user who ran the command, %message% - Message specified in the remind, %target% - target channel of the remind.   **Bot owner only.**

### Searches  
Command and aliases | Description | Usage
----------------|--------------|-------
`~weather` `~we` | `~we Moscow RF` | Shows weather data for a specified city and a country. BOTH ARE REQUIRED. Use country abbrevations. 
`~youtube` `~yt` | `~yt query` | Searches youtubes and shows the first result 
`~imdb`  | `~imdb Batman vs Superman` | Queries imdb for movies or series, show first result. 
`~randomcat` `~meow` | `~meow` | Shows a random cat image. 
`~randomdog` `~woof` | `~woof` | Shows a random dog image. 
`~img` `~i` | `~i cute kitten` | Pulls the first image found using a search parameter. Use ~ir for different results. 
`~ir`  | `~ir cute kitten` | Pulls a random image using a search parameter. 
`~lmgtfy`  | `~lmgtfy query` | Google something for an idiot. 
`~google` `~g` | `~google query` | Get a google search link for some terms. 
`~hearthstone` `~hs` | `~hs Ysera` | Searches for a Hearthstone card and shows its image. Takes a while to complete. 
`~urbandict` `~ud` | `~ud Pineapple` | Searches Urban Dictionary for a word. 
`~#`  | `~# ff` | Searches Tagdef.com for a hashtag. 
`~catfact`  | `~catfact` | Shows a random catfact from <http://catfacts-api.appspot.com/api/facts> 
`~revav`  | `~revav "@SomeGuy"` | Returns a google reverse image search for someone's avatar. 
`~revimg`  | `~revimg Image link` | Returns a google reverse image search for an image from a link. 
`~safebooru`  | `~safebooru yuri+kissing` | Shows a random image from safebooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) 
`~wikipedia` `~wiki` | `~wiki query` | Gives you back a wikipedia link 
`~color` `~clr` | `~clr 00ff00` | Shows you what color corresponds to that hex. 
`~videocall`  | `~videocall "@SomeGuy"` | Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message. 
`~av` `~avatar` | `~av "@SomeGuy"` | Shows a mentioned person's avatar. 
`~calculate` `~calc` | `~calc 1+1` | Evaluate a mathematical expression. 
`~calcops`  | `~calcops` | Shows all available operations in .calc command 
`~lolban`  | `~lolban` | Shows top banned champions ordered by ban rate. 
`~memelist`  | `~memelist` | Pulls a list of memes you can use with `~memegen` from http://memegen.link/templates/ 
`~memegen`  | `~memegen biw "gets iced coffee" "in the winter"` | Generates a meme from memelist with top and bottom text. 
`~anime` `~ani` `~aq` | `~ani aquarion evol` | Queries anilist for an anime and shows the first result. 
`~manga` `~mang` `~mq` | `~mq Shingeki no kyojin` | Queries anilist for a manga and shows the first result. 
`~yomama` `~ym` | `~ym` | Shows a random joke from <http://api.yomomma.info/> 
`~randjoke` `~rj` | `~rj` | Shows a random joke from <http://tambal.azurewebsites.net/joke/random> 
`~chucknorris` `~cn` | `~cn` | Shows a random chucknorris joke from <http://tambal.azurewebsites.net/joke/random> 
`~wowjoke`  | `~wowjoke` | Get one of Kwoth's penultimate WoW jokes. 
`~magicitem` `~mi` | `~mi` | Shows a random magicitem from <https://1d4chan.org/wiki/List_of_/tg/%27s_magic_items> 
`~osu`  | `~osu Name` or `~osu Name taiko` | Shows osu stats for a player. 
`~osub`  | `~osub https://osu.ppy.sh/s/127712` | Shows information about an osu beatmap. 
`~osu5`  | `~osu5 Name` | Displays a user's top 5 plays. 
`~pokemon` `~poke` | `~poke Sylveon` | Searches for a pokemon. 
`~pokemonability` `~pokeab` | `~pokeab "water gun"` | Searches for a pokemon ability. 
`~hitbox` `~hb` | `~hitbox SomeStreamer` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.**
`~twitch` `~tw` | `~twitch SomeStreamer` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.**
`~beam` `~bm` | `~beam SomeStreamer` | Notifies this channel when a certain user starts streaming. **Requires ManageMessages server permission.**
`~liststreams` `~ls` | `~ls` | Lists all streams you are following on this server. 
`~removestream` `~rms` | `~rms SomeGuy` | Removes notifications of a certain streamer on this channel. 
`~checkstream` `~cs` | `~cs twitch MyFavStreamer` | Checks if a user is online on a certain streaming platform. 
`~convertlist`  | `~convertlist` | List of the convertable dimensions and currencies. 
`~convert`  | `~convert m>km 1000` | Convert quantities from>to. 

### Help  
Command and aliases | Description | Usage
----------------|--------------|-------
`-modules` `-mdls` | `-modules` or `.modules` | List all bot modules. 
`-commands` `-cmds` | `-commands` or `.commands` | List all of the bot's commands from a certain module. 
`-h` `-help` | `-h !m q` or just `-h` | Either shows a help for a single command, or PMs you help link if no arguments are specified. 
`-hgit`  | `-hgit` | Generates the commandlist.md file. **Bot owner only.**
`-readme` `-guide` | `-readme` or `-guide` | Sends a readme and a guide links to the channel. 
`-donate`  | `-donate` or `~donate` | Instructions for helping the project! 

### NSFW  
Command and aliases | Description | Usage
----------------|--------------|-------
`~hentai`  | `~hentai yuri+kissing` | Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) 
`~danbooru`  | `~danbooru yuri+kissing` | Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) 
`~gelbooru`  | `~gelbooru yuri+kissing` | Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) 
`~rule34`  | `~rule34 yuri+kissing` | Shows a random image from rule34.xx with a given tag. Tag is optional but preffered. (multiple tags are appended with +) 
`~e621`  | `~e621 yuri kissing` | Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags. 
`~cp`  | `~cp` | We all know where this will lead you to. 
`~boobs`  | `~boobs` | Real adult content. 
`~butts` `~ass` `~butt` | `~butts` or `~ass` | Real adult content. 

### Gambling  
Command and aliases | Description | Usage
----------------|--------------|-------
`$raffle`  | `$raffle` or `$raffle RoleName` | Prints a name and ID of a random user from the online list from the (optional) role. 
`$cash` `$$$` | `$$$` or `$$$ @SomeGuy` | Check how much NadekoFlowers a person has. (Defaults to yourself) 
`$cash` `$$$` | `$$$` or `$$$ @SomeGuy` | Check how much NadekoFlowers a person has. (Defaults to yourself) 
`$give`  | `$give 1 "@SomeGuy"` | Give someone a certain amount of NadekoFlowers 
`$award`  | `$award 100 @person` | Gives someone a certain amount of flowers.   **Bot owner only.**
`$award`  | `$award 100 @person` | Gives someone a certain amount of flowers.   **Bot owner only.**
`$take`  | `$take 1 "@someguy"` | Takes a certain amount of flowers from someone.   **Bot owner only.**
`$take`  | `$take 1 "@someguy"` | Takes a certain amount of flowers from someone.   **Bot owner only.**
`$betroll` `$br` | `$br 5` | Bets a certain amount of NadekoFlowers and rolls a dice. Rolling over 66 yields x2 flowers, over 90 - x3 and 100 x10. 
`$leaderboard` `$lb` | `$lb` | Displays bot currency leaderboard 
`$race`  | `$race` | Starts a new animal race. 
`$joinrace` `$jr` | `$jr` or `$jr 5` | Joins a new race. You can specify an amount of flowers for betting (optional). You will get YourBet*(participants-1) back if you win. 
`$roll`  | `$roll` or `$roll 7` or `$roll 3d5` | Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. 
`$roll`  | `$roll` or `$roll 7` or `$roll 3d5` | Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. 
`$roll`  | `$roll` or `$roll 7` or `$roll 3d5` | Rolls 0-100. If you supply a number [x] it rolls up to 30 normal dice. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. 
`$rolluo`  | `$rolluo` or `$rolluo 7` or `$rolluo 3d5` | Rolls X normal dice (up to 30) unordered. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. 
`$rolluo`  | `$rolluo` or `$rolluo 7` or `$rolluo 3d5` | Rolls X normal dice (up to 30) unordered. If you split 2 numbers with letter d (xdy) it will roll x dice from 1 to y. 
`$nroll`  | `$nroll 5` (rolls 0-5) or `$nroll 5-15` | Rolls in a given range. 
`$draw`  | `$draw` or `$draw 5` | Draws a card from the deck.If you supply number X, she draws up to 5 cards from the deck. 
`$shuffle` `$sh` | `$sh` | Reshuffles all cards back into the deck. 
`$flip`  | `$flip` or `$flip 3` | Flips coin(s) - heads or tails, and shows an image. 
`$betflip` `$bf` | `$bf 5 heads` or `$bf 3 t` | Bet to guess will the result be heads or tails. Guessing award you double flowers you've bet. 

### Games  
Command and aliases | Description | Usage
----------------|--------------|-------
`>choose`  | `>choose Get up;Sleep;Sleep more` | Chooses a thing from a list of things 
`>8ball`  | `>8ball should i do something` | Ask the 8ball a yes/no question. 
`>rps`  | `>rps scissors` | Play a game of rocket paperclip scissors with Nadeko. 
`>linux`  | `>linux Spyware Windows` | Prints a customizable Linux interjection 
`>leet`  | `>leet 3 Hello` | Converts a text to leetspeak with 6 (1-6) severity levels 
`>poll`  | `>poll Question?;Answer1;Answ 2;A_3` | Creates a poll, only person who has manage server permission can do it. 
`>pollend`  | `>pollend` | Stops active poll on this server and prints the results in this channel. 
`>pick`  | `>pick` | Picks a flower planted in this channel. 
`>plant`  | `>plant` | Spend a flower to plant it in this channel. (If bot is restarted or crashes, flower will be lost) 
`>gencurrency` `>gc` | `>gc` | Toggles currency generation on this channel. Every posted message will have 2% chance to spawn a NadekoFlower. Requires Manage Messages permission. **Requires ManageMessages server permission.**
`>typestart`  | `>typestart` | Starts a typing contest. 
`>typestop`  | `>typestop` | Stops a typing contest on the current channel. 
`>typeadd`  | `>typeadd wordswords` | Adds a new article to the typing contest. **Bot owner only.**
`>trivia` `>t` | `>t nohint` or `>t 5 nohint` | Starts a game of trivia. You can add nohint to prevent hints.First player to get to 10 points wins by default. You can specify a different number. 30 seconds per question. 
`>tl`  | `>tl` | Shows a current trivia leaderboard. 
`>tq`  | `>tq` | Quits current trivia after current question. 

### ClashOfClans  
Command and aliases | Description | Usage
----------------|--------------|-------
`,createwar` `,cw` | `,cw 15 The Enemy Clan` | Creates a new war by specifying a size (>10 and multiple of 5) and enemy clan name. 
`,startwar` `,sw` | `,sw 15` | Starts a war with a given number. 
`,listwar` `,lw` | `,lw [war_number] or ,lw` | Shows the active war claims by a number. Shows all wars in a short way if no number is specified. 
`,claim` `,call` `,c` | `,call [war_number] [base_number] [optional_other_name]` | Claims a certain base from a certain war. You can supply a name in the third optional argument to claim in someone else's place. 
`,claimfinish1` `,cf1` | `,cf [war_number] [optional_other_name]` | Finish your claim with 1 stars if you destroyed a base. Optional second argument finishes for someone else. 
`,claimfinish2` `,cf2` | `,cf [war_number] [optional_other_name]` | Finish your claim with 2 stars if you destroyed a base. Optional second argument finishes for someone else. 
`,claimfinish` `,cf` `,cf3` `,claimfinish3` | `,cf [war_number] [optional_other_name]` | Finish your claim with 3 stars if you destroyed a base. Optional second argument finishes for someone else. 
`,endwar` `,ew` | `,ew [war_number]` | Ends the war with a given index. 
`,unclaim` `,ucall` `,uc` | `,uc [war_number] [optional_other_name]` | Removes your claim from a certain war. Optional second argument denotes a person in whose place to unclaim 

### CustomReactions  
Command and aliases | Description | Usage
----------------|--------------|-------
`.addcustreact` `.acr` | `.acr "hello" Hi there %user%` | Add a custom reaction with a trigger and a response. Running this command in server requires Administration permission. Running this command in DM is Bot Owner only and adds a new global custom reaction. Guide here: <https://github.com/Kwoth/NadekoBot/wiki/Custom-Reactions> 
`.listcustreact` `.lcr` | `.lcr 1` | Lists global or server custom reactions (15 commands per page). Running the command in DM will list global custom reactions, while running it in server will list that server's custom reactions. 
`.delcustreact` `.dcr` | `.dcr 5` | Deletes a custom reaction on a specific index. If ran in DM, it is bot owner only and deletes a global custom reaction. If ran in a server, it requires Administration priviledges and removes server custom reaction. 

### Translator  
Command and aliases | Description | Usage
----------------|--------------|-------
`~translate` `~trans` | `~trans en>fr Hello` | Translates from>to text. From the given language to the destiation language. 
`~translangs`  | `~translangs` | List the valid languages for translation. 

### Pokemon  
Command and aliases | Description | Usage
----------------|--------------|-------
`>poke_cmd`  | poke_usage | poke_desc 

### Music  
Command and aliases | Description | Usage
----------------|--------------|-------
`!!next` `!!n` | `!!n` | Goes to the next song in the queue. You have to be in the same voice channel as the bot. 
`!!stop` `!!s` | `!!s` | Stops the music and clears the playlist. Stays in the channel. 
`!!destroy` `!!d` | `!!d` | Completely stops the music and unbinds the bot from the channel. (may cause weird behaviour) 
`!!pause` `!!p` | `!!p` | Pauses or Unpauses the song. 
`!!queue` `!!q` `!!yq` | `!!q Dream Of Venice` | Queue a song using keywords or a link. Bot will join your voice channel.**You must be in a voice channel**. 
`!!soundcloudqueue` `!!sq` | `!!sq Dream Of Venice` | Queue a soundcloud song using keywords. Bot will join your voice channel.**You must be in a voice channel**. 
`!!listqueue` `!!lq` | `!!lq` or `!!lq 2` | Lists 15 currently queued songs per page. Default page is 1. 
`!!nowplaying` `!!np` | `!!np` | Shows the song currently playing. 
`!!volume` `!!vol` | `!!vol 50` | Sets the music volume 0-100% 
`!!defvol` `!!dv` | `!!dv 80` | Sets the default music volume when music playback is started (0-100). Persists through restarts. 
`!!shuffle` `!!sh` | `!!sh` | Shuffles the current playlist. 
`!!playlist` `!!pl` | `!!pl playlist link or name` | Queues up to 500 songs from a youtube playlist specified by a link, or keywords. 
`!!soundcloudpl` `!!scpl` | `!!scpl soundcloudseturl` | Queue a soundcloud playlist using a link. 
`!!localplaylst` `!!lopl` | `!!lopl C:/music/classical` | Queues all songs from a directory. **Bot owner only.**
`!!radio` `!!ra` | `!!ra radio link here` | Queues a radio stream from a link. It can be a direct mp3 radio stream, .m3u, .pls .asx or .xspf (Usage Video: <https://streamable.com/al54>) 
`!!local` `!!lo` | `!!lo C:/music/mysong.mp3` | Queues a local file by specifying a full path. **Bot owner only.**
`!!move` `!!mv` | `!!mv` | Moves the bot to your voice channel. (works only if music is already playing) 
`!!remove` `!!rm` | `!!rm 5` | Remove a song by its # in the queue, or 'all' to remove whole queue. 
`!!remove` `!!rm` | `!!rm 5` | Remove a song by its # in the queue, or 'all' to remove whole queue. 
`!!movesong` `!!ms` | `!! ms 5>3` | Moves a song from one position to another. 
`!!setmaxqueue` `!!smq` | `!!smq 50` or `!!smq` | Sets a maximum queue size. Supply 0 or no argument to have no limit. 
`!!reptcursong` `!!rcs` | `!!rcs` | Toggles repeat of current song. 
`!!rpeatplaylst` `!!rpl` | `!!rpl` | Toggles repeat of all songs in the queue (every song that finishes is added to the end of the queue). 
`!!save`  | `!!save classical1` | Saves a playlist under a certain name. Name must be no longer than 20 characters and mustn't contain dashes. 
`!!load`  | `!!load classical-1` | Loads a playlist under a certain name. 
`!!playlists` `!!pls` | `!!pls 1` | Lists all playlists. Paginated. 20 per page. Default page is 0. 
`!!deleteplaylist` `!!delpls` | `!!delpls animu-5` | Deletes a saved playlist. Only if you made it or if you are the bot owner. 
`!!goto`  | `!!goto 30` | Goes to a specific time in seconds in a song. 
`!!getlink` `!!gl` | `!!gl` | Shows a link to the song in the queue by index, or the currently playing song by default. 
`!!autoplay` `!!ap` | `!!ap` | Toggles autoplay - When the song is finished, automatically queue a related youtube song. (Works only for youtube songs and when queue is empty) 
