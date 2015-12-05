# NadekoBot

Nadeko Discord chatbot I made in c#, using Discord.net library.

##This section will guide you through how to setup NadekoBot
After you have cloned this repo, move the data from the DLLs folder to the bin/debug. Those are part of the libraries you will need for your project. The other part should resolve after you start the project for the first time. All the references are shown [in this image.](http://icecream.me/uploads/72738d3b2797e46767e10820998ad5b3.png)

In your bin/debug folder (or next to your exe), you must have a file called 'credentials.json' in which you will store all the necessary data to make the bot know who the owner is, where to store data, etc.

**This is how the credentials.json should look like:**
```json
{
	"Username":"bot_email",
	"BotMention":"@BotName",
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
