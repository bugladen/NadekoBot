# NadekoBot
**Scroll down for a list of commands**  
Nadeko Discord chatbot written in C# using Discord.net library.  
You might want to join my discord server where i can provide help etc. https://discord.gg/0ehQwTK2RBhxEi0X

##This section will guide you through how to setup NadekoBot
#### For easy setup and no programming knowledge, you can download .exe from [releases](https://github.com/Kwoth/NadekoBot/releases) and follow the comprehensive [GUIDE](https://github.com/Kwoth/NadekoBot/blob/master/ComprehensiveGuide.md)

In your bin/debug folder (or next to your exe if you are using release version), you must have a file called 'credentials.json' in which you will store all the necessary data to make the bot know who the owner is, and your api keys.

**This is how the credentials.json should look like:**
```json
{
    "Username":"bot_email",
    "BotMention":"<@bot_id>",
    "Password":"bot_password",
    "GoogleAPIKey":"google_api_key",
    "OwnerID":123123123123,
    "TrelloAppKey": "your_trello_app_key (optional)",
    "ForwardMessages": true,
    "SoundCloudClientID": "your_soundcloud_key (optional)",
    "MashapeKey": "your_mashape_key (optional)",
}
```
##### You can omit:  
- googleAPIKey if you don't want music  
- TrelloAppKey if you don't need trello notifications
- ForwardMessages if you don't want bot PM messages to be redirected to you
```json
{
	"Username":"bot_email",
	"BotMention":"<@bot_id>",
	"Password":"bot_password",
	"OwnerID":123123123123,
}
```
- BotMention(bot\_id) and OwnerID are **NOT** names of the owner and the bot. If you do not know the id of your bot, put 2 random numbers in those fields, run the bot and do `.uid @MyBotName` - that will give you your bot\_id, do the same for yourself `.uid @MyName` and copy the numbers in their respective fields.
- For google api key, you need to enable URL shortner, Youtube video search **and custom search** in the [dev console](https://console.developers.google.com/).
- For the Soundcloud Api key you need a Soundcloud account. You need to create a new app on http://soundcloud.com/you/apps/new and after that go here http://soundcloud.com/you/apps click on the name of your created your app and copy the Client ID. Paste it into credentials.json.
- For Mashape Api Key you need to create an account on their api marketplace here https://market.mashape.com/. After that you need to go to market.mashape.com/YOURNAMEHERE/applications/default-application and press GET THE KEYS in the right top corner copy paste it into your credentials.json and you are ready to race! 
- If you want to have music, you need to download FFMPEG from this link http://ffmpeg.zeranoe.com/builds/ (static build version) and add ffmpeg/bin folder to your PATH environment variable. You do that by opening explorer -> right click 'This PC' -> properties -> advanced system settings -> In the top part, there is a PATH field, add `;` to the end and then your ffmpeg install location /bin (for example ;C:\ffmpeg-5.6.7\bin) and save. Open command prompt and type ffmpeg to see if you added it correctly. If it says "command not found" then you made a mistake somewhere. There are a lot of guides on the internet on how to add stuff to your PATH, check them out if you are stuck.
- **IF YOU HAVE BEEN USING THIS BOT BEFORE AND YOU HAVE DATA FROM PARSE THAT YOU WANT TO KEEP** you should export your parse data and extract it inside /data/parsedata in your bot's folder. Next time you start the bot, type `.parsetosql` and the bot will fill your local sqlite db with data from those .json files.

**IF BUILDING FROM SOURCE**  
You should **remove** Discord.Net projects from your solution, and use add reference to the Discord.NET DLLs in your bin/debug.  
Wait for it to resolve dependencies and start NadekoBot.

Enjoy

##List of commands   
(may be incomplete) last updated: 13.2.2016

Official repo: **github.com/Kwoth/NadekoBot/** 

### Administration  
Command and aliases | Description | Usage
----------------|--------------|-------
`.greet`  |  Enables or Disables anouncements on the current channel when someone joins the server.
`.greetmsg`  |  Sets a new announce message. Type %user% if you want to mention the new member. |  .greetmsg Welcome to the server, %user%.
`.bye`  |  Enables or Disables anouncements on the current channel when someone leaves the server.
`.byemsg`  |  Sets a new announce leave message. Type %user% if you want to mention the new member. |  .byemsg %user% has left the server.
`.byepm`  |  Toggles whether the good bye messages will be sent in a PM or in the text channel.
`.greetpm`  |  Toggles whether the greet messages will be sent in a PM or in the text channel.
`.sr`, `.setrole`  |  Sets a role for a given user. |  .sr @User Guest
`.rr`, `.removerole`  |  Removes a role from a given user. |  .rr @User Admin
`.r`, `.role`, `.cr`  |  Creates a role with a given name. |  .r Awesome Role
`.rolecolor`, `.rc`  |  Set a role's color to the hex or 0-255 color value provided. |  .color Admin 255 200 100 or .color Admin ffba55
`.roles`  |  List all roles on this server or a single user if specified.
`.modules`  |  List all bot modules
`.commands`  |  List all of the bot's commands from a certain module.
`.b`, `.ban`  |  Bans a mentioned user
`.ub`, `.unban`  |  Unbans a mentioned user
`.k`, `.kick`  |  Kicks a mentioned user.
`.mute`  |  Mutes mentioned user or users
`.unmute`  |  Unmutes mentioned user or users
`.deafen`, `.deaf`  |  Deafens mentioned user or users
`.undeafen`, `.undeaf`  |  Undeafens mentioned user or users
`.rvch`  |  Removes a voice channel with a given name.
`.vch`, `.cvch`  |  Creates a new voice channel with a given name.
`.rch`, `.rtch`  |  Removes a text channel with a given name.
`.ch`, `.tch`  |  Creates a new text channel with a given name.
`.st`, `.settopic`  |  Sets a topic on the current channel.
`.uid`, `.userid`  |  Shows user id
`.cid`, `.channelid`  |  Shows current channel id
`.sid`, `.serverid`  |  Shows current server id
`.stats`  |  Shows some basic stats for nadeko
`.leaveall`  |  Nadeko leaves all servers **OWNER ONLY**
`.prune`  |  Prunes a number of messages from the current channel. |  .prune 5
`.die`, `.graceful`  |  Works only for the owner. Shuts the bot down and notifies users about the restart.
`.clr`  |  Clears some of nadeko's messages from the current channel.
`.newname`, `.setname`  |  Give the bot a new name.
`.newavatar`, `.setavatar`  |  Sets a new avatar image for the NadekoBot.
`.setgame`  |  Sets the bots game.
`.checkmyperms`  |  Checks your userspecific permissions on this channel.
`.commsuser`  |  Sets a user for through-bot communication. Only works if server is set. Resets commschannel.**Owner only**.
`.commsserver`  |  Sets a server for through-bot communication.**Owner only**.
`.commschannel`  |  Sets a channel for through-bot communication. Only works if server is set. Resets commsuser.**Owner only**.
`.send`  |  Send a message to someone on a different server through the bot.**Owner only.**
  |  .send Message text multi word!
`.menrole`, `.mentionrole`  |  Mentions every person from the provided role or roles (separated by a ',') on this server. Requires you to have mention everyone permission.
`.parsetosql`  |  Loads exported parsedata from /data/parsedata/ into sqlite database.
`.unstuck`  |  Clears the message queue. **OWNER ONLY**
`.donators`  |  List of lovely people who donated to keep this project alive.
`.adddon`, `.donadd`  |  Add a donator to the database.

### Help  
Command and aliases | Description | Usage
----------------|--------------|-------
`-h`, `-help`, `@BotName help`, `@BotName h`, `~h`  |  Help command
`-hgit`  |  Help command stylized for github readme
`-readme`, `-guide`  |  Sends a readme and a guide links to the channel.
`-donate`, `~donate`  |  Instructions for helping the project!

### Permissions  
Command and aliases | Description | Usage
----------------|--------------|-------
`;permrole`, `;pr`  |  Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'.
`;verbose`, `;v`  |  Sets whether to show when a command/module is blocked. |  ;verbose true
`;serverperms`, `;sp`  |  Shows banned permissions for this server.
`;roleperms`, `;rp`  |  Shows banned permissions for a certain role. No argument means for everyone. |  ;rp AwesomeRole
`;channelperms`, `;cp`  |  Shows banned permissions for a certain channel. No argument means for this channel. |  ;cp #dev
`;userperms`, `;up`  |  Shows banned permissions for a certain user. No argument means for yourself. |  ;up Kwoth
`;sm`, `;servermodule`  |  Sets a module's permission at the server level. |  ;sm <module_name> enable
`;sc`, `;servercommand`  |  Sets a command's permission at the server level. |  ;sc <command_name> disable
`;rm`, `;rolemodule`  |  Sets a module's permission at the role level. |  ;rm <module_name> enable <role_name>
`;rc`, `;rolecommand`  |  Sets a command's permission at the role level. |  ;rc <command_name> disable <role_name>
`;cm`, `;channelmodule`  |  Sets a module's permission at the channel level. |  ;cm <module_name> enable <channel_name>
`;cc`, `;channelcommand`  |  Sets a command's permission at the channel level. |  ;cm enable <channel_name>
`;um`, `;usermodule`  |  Sets a module's permission at the user level. |  ;um <module_name> enable <user_name>
`;uc`, `;usercommand`  |  Sets a command's permission at the user level. |  ;uc <module_command> enable <user_name>
`;asm`, `;allservermodules`  |  Sets permissions for all modules at the server level. |  ;asm <enable/disable>
`;asc`, `;allservercommands`  |  Sets permissions for all commands from a certain module at the server level. |  ;asc <module_name> <enable/disable>
`;acm`, `;allchannelmodules`  |  Sets permissions for all modules at the channel level. |  ;acm <enable/disable> <channel_name>
`;acc`, `;allchannelcommands`  |  Sets permissions for all commands from a certain module at the channel level. |  ;acc <module_name> <enable/disable> <channel_name>
`;arm`, `;allrolemodules`  |  Sets permissions for all modules at the role level. |  ;arm <enable/disable> <role_name>
`;arc`, `;allrolecommands`  |  Sets permissions for all commands from a certain module at the role level. |  ;arc <module_name> <enable/disable> <channel_name>

### Conversations  
Command and aliases | Description | Usage
----------------|--------------|-------
`\o\`  |  Nadeko replies with /o/
`/o/`  |  Nadeko replies with \o\
`@BotName copyme`, `@BotName cm`  |  Nadeko starts copying everything you say. Disable with cs
`@BotName cs`, `@BotName copystop`  |  Nadeko stops copying you
`@BotName req`, `@BotName request`  |  Requests a feature for nadeko. |  @NadekoBot req new_feature
`@BotName lr`  |  PMs the user all current nadeko requests.
`@BotName dr`  |  Deletes a request. Only owner is able to do this.
`@BotName rr`  |  Resolves a request. Only owner is able to do this.
`@BotName uptime`  |  Shows how long is Nadeko running for.
`@BotName die`  |  Works only for the owner. Shuts the bot down.
`@BotName randserver`  |  Generates an invite to a random server and prints some stats.
`@BotName do you love me`  |  Replies with positive answer only to the bot owner.
`@BotName how are you`  |  Replies positive only if bot owner is online.
`@BotName insult`  |  Only works for owner. Insults @X person. |  @NadekoBot insult @X.
`@BotName praise`  |  Only works for owner. Praises @X person. |  @NadekoBot praise @X.
`@BotName pat`  |  Pat someone ^_^
`@BotName cry`  |  Tell Nadeko to cry. You are a heartless monster if you use this command.
`@BotName are you real`  |  Useless.
`@BotName are you there`, `@BotName !`, `@BotName ?`  |  Checks if nadeko is operational.
`@BotName draw`  |  Nadeko instructs you to type $draw. Gambling functions start with $
`@BotName fire`  |  Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire. |  @NadekoBot fire [x]
`@BotName rip`  |  Shows a grave image of someone with a start year |  @NadekoBot rip @Someone 2000
`@BotName j`  |  Joins a server using a code.
`@BotName slm`  |  Shows the message where you were last mentioned in this channel (checks last 10k messages)
`@BotName bb`  |  Says bye to someone.  |  @NadekoBot bb @X
`@BotName call`  |  Useless. Writes calling @X to chat. |  @NadekoBot call @X 
`@BotName hide`  |  Hides nadeko in plain sight!11!!
`@BotName unhide`  |  Unhides nadeko in plain sight!1!!1
`@BotName dump`  |  Dumps all of the invites it can to dump.txt.** Owner Only.**
`@BotName ab`  |  Try to get 'abalabahaha'
`@BotName av`, `@BotName avatar`  |  Shows a mentioned person's avatar.  |  ~av @X

### Gambling  
Command and aliases | Description | Usage
----------------|--------------|-------
`$draw`  |  Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck. |  $draw [x]
`$shuffle`, `$reshuffle`  |  Reshuffles all cards back into the deck.
`$flip`  |  Flips coin(s) - heads or tails, and shows an image. |  `$flip` or `$flip 3`
`$roll`  |  Rolls 2 dice from 0-10. If you supply a number [x] it rolls up to 30 normal dice. |  $roll [x]
`$nroll`  |  Rolls in a given range. |  `$nroll 5` (rolls 0-5) or `$nroll 5-15`
`$raffle`  |  Prints a name and ID of a random user from the online list from the (optional) role.
`$$$`  |  Check how many NadekoFlowers you have.

### Games  
Command and aliases | Description | Usage
----------------|--------------|-------
`t`, `-t`  |  Starts a game of trivia.
`tl`, `-tl`, `tlb`, `-tlb`  |  Shows a current trivia leaderboard.
`tq`, `-tq`  |  Quits current trivia after current question.
`typestart`  |  Starts a typing contest.
`typestop`  |  Stops a typing contest on the current channel.
`typeadd`  |  Adds a new article to the typing contest. Owner only.
`>poll`  |  Creates a poll, only person who has manage server permission can do it. |  >poll Question?;Answer1;Answ 2;A_3
`>pollend`  |  Stops active poll on this server and prints the results in this channel.
`,startwar`, `,sw`  |  Starts a new war by specifying a size (>10 and multiple of 5) and enemy clan name. War ends in 23 hours. You need manage channels permission to use this. | ,sw 15 The Enemy Clan
`,listwar`, `,lw`  |  Shows the active war claims by a number. Shows all wars in a short way if no number is specified. |  ,lw [war_number] or ,lw
`,claim`, `,call`, `,c`  |  Claims a certain base from a certain war. |  ,call [war_number] [base_number]
`,cf`, `,claimfinish`  |  Finish your claim if you destroyed a base. |  ,cf [war_number]
`,unclaim`, `,uncall`, `,uc`  |  Removes your claim from a certain war. |  ,uc [war_number] [base_number]
`,endwar`, `,ew`  |  Ends the war with a given index. | ,ew [war_number]
`>choose`  |  Chooses a thing from a list of things |  >choose Get up;Sleep;Sleep more
`>8ball`  |  Ask the 8ball a yes/no question.
`>`  |  Attack a person. Supported attacks: 'splash', 'strike', 'burn', 'surge'. |  > strike @User
`poketype`  |  Gets the users element type. Use this to do more damage with strike

### Music  
Command and aliases | Description | Usage
----------------|--------------|-------
`!m n`, `!m next`  |  Goes to the next song in the queue.
`!m s`, `!m stop`  |  Completely stops the music and unbinds the bot from the channel and cleanes up files.
`!m p`, `!m pause`  |  Pauses or Unpauses the song
`!m q`, `!m yq`  |  Queue a song using keywords or link. Bot will join your voice channel. **You must be in a voice channel**. |  `!m q Dream Of Venice`
`!m lq`, `!m ls`, `!m lp`  |  Lists up to 10 currently queued songs.
`!m np`, `!m playing`  |  Shows the song currently playing.
`!m vol`  |  Sets the music volume 0-150%
`!m dv`, `!m defvol`  |  Sets the default music volume when music playback is started (0-100). Does not persist through restarts. |  !m dv 80
`!m min`, `!m mute`  |  Sets the music volume to 0%
`!m max`  |  Sets the music volume to 100% (real max is actually 150%).
`!m half`  |  Sets the music volume to 50%.
`!m sh`  |  Shuffles the current playlist.
`!m setgame`  |  Sets the game of the bot to the number of songs playing.**Owner only**
`!m pl`  |  Queues up to 25 songs from a youtube playlist specified by a link, or keywords.
`!m radio`, `!m ra`  |  Queues a direct radio stream from a link.
`!m mv`  |  Moves the bot to your voice channel. (works only if music is already playing)
`!m rm`  |  Removes a song by a # from the queue
`!m debug`  |  Writes some music data to console. **BOT OWNER ONLY**

### Searches  
Command and aliases | Description | Usage
----------------|--------------|-------
`~yt`  |  Searches youtubes and shows the first result
`~ani`, `~anime`, `~aq`  |  Queries anilist for an anime and shows the first result.
`~mang`, `~manga`, `~mq`  |  Queries anilist for a manga and shows the first result.
`~randomcat`  |  Shows a random cat image.
`~i`  |  Pulls a first image using a search parameter. Use ~ir for different results. |  ~i cute kitten
`~ir`  |  Pulls a random image using a search parameter. |  ~ir cute kitten
`lmgtfy`, `~lmgtfy`  |  Google something for an idiot.
`~hs`  |  Searches for a Hearthstone card and shows its image. Takes a while to complete. | ~hs Ysera
`~osu`  |  Shows osu stats for a player | ~osu Name
`~ud`  |  Searches Urban Dictionary for a word | ~ud Pineapple

### NSFW  
Command and aliases | Description | Usage
----------------|--------------|-------
`~hentai`  |  Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~hentai yuri
`~danbooru`  |  Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~danbooru yuri+kissing
`~gelbooru`  |  Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) |  ~gelbooru yuri+kissing
`~e621`  |  Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags. |  ~e621 yuri kissing
`~cp`  |  We all know where this will lead you to.
`~boobs`  |  Real adult content.

