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
- For parse, you need to register on parse.com, create an app with these 3 classes `'CommandsRan', 'Requests' and 'Stats'` in order to make the logging work.

Download [this folder](http://s000.tinyupload.com/index.php?file_id=54172283263968075500) which contains images and add it next to your .exe in order to make the $draw, $flip, rip and similar functions work.

You should replace nadeko's image with the image of your bot in order to make the hide/unhide commands work as intended.

**You are all set.**
Fire up visual studio, wait for it to resolve dependencies and start NadekoBot.

Enjoy

##List of commands  
(may be incomplete) 10.12.2015

----Administration----  
`-h` [-help ] [@BotName help ] [@BotName h ] **Description:** Help command  
`.sr` [.setrole ] **Description:** Sets a role for a given user.  
**Usage:** .sr @User Guest  
`.rr` [.removerole ] **Description:** Removes a role from a given user.  
**Usage:** .rr @User Admin  
`.r` [.role ] [.cr ] **Description:** Creates a role with a given name, and color.  
`Both the user and the bot must have the sufficient permissions.  
`.b` [.ban ] **Description:** Kicks a mentioned user  
`Both the user and the bot must have the sufficient permissions.  
`.k` [.kick ] **Description:** Kicks a mentioned user.  
`Both the user and the bot must have the sufficient permissions.  
`.rvch` **Description:** Removes a voice channel with a given name.  
`Both the user and the bot must have the sufficient permissions.  
`.vch` [.cvch ] **Description:** Creates a new voice channel with a given name.  
`Both the user and the bot must have the sufficient permissions.  
`.rch` [.rtch ] **Description:** Removes a text channel with a given name.  
`Both the user and the bot must have the sufficient permissions.  
`.ch` [.tch ] **Description:** Creates a new text channel with a given name.  
`Both the user and the bot must have the sufficient permissions.  
`.uid` [.userid ] **Description:** Shows user id  
`.cid` [.channelid ] **Description:** Shows current channel id  
`.sid` [.serverid ] **Description:** Shows current server id  
`.stats` **Description:** Shows some basic stats for nadeko  

----Conversations----  
\o\ **Description:** Nadeko replies with /o/  
/o/ **Description:** Nadeko replies with \o\  
`@BotName` copyme [@BotName cm ] **Description:** Nadeko starts copying everything you say. Disable with cs  
`@BotName` cs [@BotName copystop ] **Description:** Nadeko stops copying you  
`@BotName` do you love me **Description:** Replies with positive answer only to the bot owner.  
`@BotName` die **Description:** Works only for the owner. Shuts the bot down.  
`@BotName` how are you **Description:** Replies positive only if bot owner is online.  
`@BotName` insult **Description:** Only works for owner. Insults @X person.  
**Usage:** @NadekoBot insult @X.  
`@BotName` praise **Description:** Only works for owner. Praises @X person.  
**Usage:** @NadekoBot praise @X.  
`@BotName` are you real **Description:** Useless.  
`@BotName` are you there [@BotName ! ] [@BotName ? ] **Description:** Checks if nadeko is operational.  
`@BotName` draw **Description:** Nadeko instructs you to type $draw. Gambling functions start with $  
`@BotName` uptime **Description:** Shows how long is Nadeko running for.  
`@BotName` fire **Description:** Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.  
**Usage:** @NadekoBot fire [x]  
`@BotName` rip **Description:** Shows a grave image.Optional parameter [@X] instructs her to put X's name on the grave.  
**Usage:** @NadekoBot rip [@X]  
`@BotName` j **Description:** Joins a server using a code.  
`@BotName` i [@BotName img ] **Description:** Pulls a first image using a search parameter.  
**Usage:** @NadekoBot img Multiword_search_parameter  
`@BotName` ir [@BotName imgrandom ] **Description:** Pulls a random image using a search parameter.  
**Usage:** @NadekoBot img Multiword_search_parameter  
`@BotName` save [@BotName ,s ] [@BotName -s ] **Description:** Saves something for the owner in a file.  
`@BotName` ls **Description:** Shows all saved items.  
`@BotName` cs **Description:** Deletes all saves  
`@BotName` bb **Description:** Says bye to someone. **Usage:** @NadekoBot bb @X  
`@BotName` req [@BotName ,request ] [@BotName -request ] **Description:** Requests a feature for nadeko.  
**Usage:** @NadekoBot req new_feature  
`@BotName` lr **Description:** PMs the user all current nadeko requests.  
`@BotName` dr **Description:** Deletes a request. Only owner is able to do this.  
`@BotName` rr **Description:** Resolves a request. Only owner is able to do this.  
`@BotName` clr **Description:** Clears some of nadeko's messages from the current channel.  
`@BotName` call **Description:** Useless. Writes calling @X to chat.  
**Usage:** @NadekoBot call @X   
`@BotName` hide **Description:** Hides nadeko in plain sight!11!!  
`@BotName` unhide **Description:** Unhides nadeko in plain sight!1!!1  

----Gambling----  
`$draw` **Description:** Draws a card from the deck.If you supply number [x], she draws up to 5 cards from the deck.  
**Usage:** $draw [x]  
`$flip` **Description:** Flips a coin, heads or tails, and shows an image of it.  
`$roll` **Description:** Rolls 2 dice from 0-10. If you supply a number [x] it rolls up to 30 normal dice.  
**Usage:** $roll [x]  

----Games----  
`t` [-t ] **Description:** Starts a game of trivia. Questions suck and repeat a lot atm.  
`tl` [-tl ] [tlb ] [-tlb ] **Description:** Shows a current trivia leaderboard.  
`tq` [-tq ] **Description:** Quits current trivia after current question.  

----Searches----  
`~av` **Description:** Shows a mentioned person's avatar. **Usage:** ~av @X  
`~yt` **Description:** Queries youtubes and embeds the first result  
`~ani` [~anime ] [~aq ] **Description:** Queries anilist for an anime and shows the first result.  
`~mang` [~manga ] [~mq ] **Description:** Queries anilist for a manga and shows the first result.
