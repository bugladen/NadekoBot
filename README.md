# NadekoBot

Nadeko Discord chatbot I made in c#, using Discord.net library.  
You can also create a very basic web-ui with the data that is in the Parse DB. [example](http://www.nadekobot.tk)

##This section will guide you through how to setup NadekoBot
After you have cloned this repo, move the data from the DLLs folder to the bin/debug. Those are part of the libraries you will need for your project. The other part should resolve after you start the project for the first time. All the references are shown [in this image.](http://icecream.me/uploads/72738d3b2797e46767e10820998ad5b3.png)

In your bin/debug folder (or next to your exe), you must have a file called 'credentials.json' in which you will store all the necessary data to make the bot know who the owner is, where to store data, etc.

**This is how the credentials.json should look like:**
```json
{
	"Username":"bot_email",
	"BotMention":"<@bot_id>",
	"Password":"bot_password",
	"GoogleAPIKey":"google_api_key",
	"OwnerID":123123123123,
	"Crawl":false,
	"ParseID":"parse_app_id",
	"ParseKey":"parse_api_key",
}
```
- Keep the crawl on false.
- For google api key, you need to enable URL shortner and Youtube video search in the dev console.
- For **ParseID** and **ParseKey**, you need to register on http://www.parse.com, get those values and create an app with these 3 classes: `'CommandsRan', 'Requests' and 'Stats'` in order to make the logging work.
- If you have **Windows7**, you need to install [Parse.Api](https://www.nuget.org/packages/Parse.Api/) instead of the library that comes with this project

Download [this folder](http://s000.tinyupload.com/index.php?file_id=54172283263968075500) which contains images and add it next to your .exe in order to make the $draw, $flip, rip and similar functions work.

You should replace nadeko's image with the image of your bot in order to make the hide/unhide commands work as intended.

**You are all set.**
Fire up visual studio, wait for it to resolve dependencies and start NadekoBot.

Enjoy

##List of commands  
(may be incomplete) 10.12.2015


Official repo: **github.com/Kwoth/NadekoBot/** 

### Administration  
Command [alias] | Description | Usage
----------------|--------------|-------
`-h` [-help ] [@BotName help ] [@BotName h ]  |  Help command
`-hgit`  |  Help command stylized for github readme
`.sr` [.setrole ]  |  Sets a role for a given user. |  .sr @User Guest
`.rr` [.removerole ]  |  Removes a role from a given user. |  .rr @User Admin
`.r` [.role ] [.cr ]  |  Creates a role with a given name, and color.
`.b` [.ban ]  |  Kicks a mentioned user
`.k` [.kick ]  |  Kicks a mentioned user.
`.rvch`  |  Removes a voice channel with a given name.
`.vch` [.cvch ]  |  Creates a new voice channel with a given name.
`.rch` [.rtch ]  |  Removes a text channel with a given name.
`.ch` [.tch ]  |  Creates a new text channel with a given name.
`.uid` [.userid ]  |  Shows user id
`.cid` [.channelid ]  |  Shows current channel id
`.sid` [.serverid ]  |  Shows current server id
`.stats`  |  Shows some basic stats for nadeko

### Conversations  
Command [alias] | Description | Usage
----------------|--------------|-------
`\o\`  |  Nadeko replies with /o/
`/o/`  |  Nadeko replies with \o\
`@BotName copyme` [@BotName cm ]  |  Nadeko starts copying everything you say. Disable with cs
`@BotName cs` [@BotName copystop ]  |  Nadeko stops copying you
`@BotName do you love me`  |  Replies with positive answer only to the bot owner.
`@BotName die`  |  Works only for the owner. Shuts the bot down.
`@BotName how are you`  |  Replies positive only if bot owner is online.
`@BotName insult`  |  Only works for owner. Insults @X person. |  @NadekoBot insult @X.
`@BotName praise`  |  Only works for owner. Praises @X person. |  @NadekoBot praise @X.
`@BotName are you real`  |  Useless.
`@BotName are you there` [@BotName ! ] [@BotName ? ]  |  Checks if nadeko is operational.
`@BotName draw`  |  Nadeko instructs you to type $draw. Gambling functions start with $
`@BotName uptime`  |  Shows how long is Nadeko running for.
`@BotName fire`  |  Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire. |  @NadekoBot fire [x]
`@BotName rip`  |  Shows a grave image.Optional parameter [@X] instructs her to put X's name on the grave. |  @NadekoBot rip [@X]
`@BotName j`  |  Joins a server using a code. Obsolete, since nadeko will autojoin any valid code in chat.
`@BotName i` [@BotName img ]  |  Pulls a first image using a search parameter. |  @NadekoBot img Multiword_search_parameter
`@BotName ir` [@BotName imgrandom ]  |  Pulls a random image using a search parameter. |  @NadekoBot img Multiword_search_parameter
`@BotName save` [@BotName ,s ] [@BotName -s ]  |  Saves something for the owner in a file.
`@BotName ls`  |  Shows all saved items.
`@BotName slm`  |  Shows the message where you were last mentioned in this channel (checks last 10k messages)
`@BotName cs`  |  Deletes all saves
`@BotName bb`  |  Says bye to someone.  |  @NadekoBot bb @X
`@BotName req` [@BotName ,request ] [@BotName -request ]  |  Requests a feature for nadeko. |  @NadekoBot req new_feature
`@BotName lr`  |  PMs the user all current nadeko requests.
`@BotName dr`  |  Deletes a request. Only owner is able to do this.
`@BotName rr`  |  Resolves a request. Only owner is able to do this.
`@BotName clr`  |  Clears some of nadeko's messages from the current channel.
`@BotName call`  |  Useless. Writes calling @X to chat. |  @NadekoBot call @X 
`@BotName hide`  |  Hides nadeko in plain sight!11!!
`@BotName unhide`  |  Unhides nadeko in plain sight!1!!1
`@BotName dump`  |  Dumps all of the invites it can to dump.txt
`@BotName randserver`  |  Generates an invite to a random server and prints some stats.
`@BotName av` [@BotName avatar ]  |  Shows a mentioned person's avatar.  |  ~av @X

### Gambling  
Command [alias] | Description | Usage
----------------|--------------|-------
`$draw`  |  Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck. |  $draw [x]
`$flip`  |  Flips a coin, heads or tails, and shows an image of it.
`$roll`  |  Rolls 2 dice from 0-10. If you supply a number [x] it rolls up to 30 normal dice. |  $roll [x]

### Games  
Command [alias] | Description | Usage
----------------|--------------|-------
`t` [-t ]  |  Starts a game of trivia.
`tl` [-tl ] [tlb ] [-tlb ]  |  Shows a current trivia leaderboard.
`tq` [-tq ]  |  Quits current trivia after current question.
`>`  |  Attack a person. Supported attacks: 'splash', 'strike', 'burn', 'surge'. |  > strike @User
`poketype`  |  Gets the users element type. Use this to do more damage with strike

### Music  
Command [alias] | Description | Usage
----------------|--------------|-------
`!m n` [!m next ]  |  Goes to the next song in the queue.
`!m s` [!m stop ]  |  Completely stops the music and unbinds the bot from the channel.
`!m p` [!m pause ]  |  Pauses the song
`!m testq`  |  Queue a song using a multi/single word name. |  `!m q Dream Of Venice`
`!m q` [!m yq ]  |  Queue a song using a multi/single word name. |  `!m q Dream Of Venice`
`!m lq` [!m ls ] [!m lp ]  |  Lists up to 10 currently queued songs.
`!m sh`  |  Shuffles the current playlist.
`!m radio` [!m music ]  |  Binds to a voice and text channel in order to play music.

### Searches  
Command [alias] | Description | Usage
----------------|--------------|-------
`~yt`  |  Queries youtubes and embeds the first result
`~ani` [~anime ] [~aq ]  |  Queries anilist for an anime and shows the first result.
`~mang` [~manga ] [~mq ]  |  Queries anilist for a manga and shows the first result.
