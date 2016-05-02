![img](https://ci.appveyor.com/api/projects/status/gmu6b3ltc80hr3k9?svg=true)
# NadekoBot

### [Click here to invite nadeko to your server](https://discordapp.com/oauth2/authorize?client_id=170254782546575360&scope=bot&permissions=66186303)
[**click here for a list of commands**](https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md)  
Nadeko Discord chatbot written in C# using Discord.net library.  
You might want to join my discord server where i can provide help etc. https://discord.gg/0ehQwTK2RBjAxzEY

##This section will guide you through how to setup NadekoBot
#### For easy setup and no programming knowledge, you can use [UPDATER](https://github.com/Kwoth/NadekoUpdater/releases/latest) or download release from [releases](https://github.com/Kwoth/NadekoBot/releases) and follow the comprehensive [GUIDE](https://github.com/Kwoth/NadekoBot/blob/master/ComprehensiveGuide.md)

In your bin/debug folder (or next to your exe if you are using release version), you must have a file called 'credentials.json' in which you will store all the necessary data to make the bot know who the owner is, and your api keys.

When you clone the project, make sure to run `git submodule init` and `git submodule update` to get the correct discord.net version

**This is how the credentials.json should look like:**
```json
{
    "BotId": 123123123123,
    "Token":"Bot.Token",
    "GoogleAPIKey":"google_api_key",
    "OwnerIds":[123123123123, 123123123123],
    "TrelloAppKey": "your_trello_app_key (optional)",
    "SoundCloudClientID": "your_soundcloud_key (optional)",
    "MashapeKey": "your_mashape_key (optional)",
    "LoLAPIKey":"your_champion.gg_apikey (optional)',
}
```
##### You can omit:  
- googleAPIKey if you don't want music  
- TrelloAppKey if you don't need trello notifications
- ForwardMessages if you don't want bot PM messages to be redirected to you
```json
{
	"Username":"bot_email",
	"BotId": 12312312312313,
	"Password":"bot_password",
	"OwnerIds":[123123123123, 1231231232],
}
```

Next to your exe you must also have a data folder in which there is config.json (among other things) which will contain some user specific config, like should bot join servers, should pms to bot be forwarded to you and list of ids of blacklisted users, channels and servers.

##### data/config.json example
```json
{
  "DontJoinServers": false,
  "ForwardMessages": true,
  "ServerBlacklist": [],
  "ChannelBlacklist": [],
  "UserBlacklist": []
}
```
- http://discord.kongslien.net/guide.html <- to make a bot account and get the `Token`
- BotId and OwnerIds are **NOT** names of the owner and the bot. If you do not know the id of your bot, put 2 random numbers in those fields, run the bot and do `.uid @MyBotName` - that will give you your bot\_id, do the same for yourself `.uid @MyName` and copy the numbers in their respective fields.
- For google api key, you need to enable URL shortner, Youtube video search **and custom search** in the [dev console](https://console.developers.google.com/).
- For the Soundcloud Api key you need a Soundcloud account. You need to create a new app on http://soundcloud.com/you/apps/new and after that go here http://soundcloud.com/you/apps click on the name of your created your app and copy the Client ID. Paste it into credentials.json.
- For Mashape Api Key you need to create an account on their api marketplace here https://market.mashape.com/. After that you need to go to market.mashape.com/YOURNAMEHERE/applications/default-application and press GET THE KEYS in the right top corner copy paste it into your credentials.json and you are ready to race! 
- If you want to have music, you need to download FFMPEG from this link http://ffmpeg.zeranoe.com/builds/ (static build version) and add ffmpeg/bin folder to your PATH environment variable. You do that by opening explorer -> right click 'This PC' -> properties -> advanced system settings -> In the top part, there is a PATH field, add `;` to the end and then your ffmpeg install location /bin (for example ;C:\ffmpeg-5.6.7\bin) and save. Open command prompt and type ffmpeg to see if you added it correctly. If it says "command not found" then you made a mistake somewhere. There are a lot of guides on the internet on how to add stuff to your PATH, check them out if you are stuck.
- **IF YOU HAVE BEEN USING THIS BOT BEFORE AND YOU HAVE DATA FROM PARSE THAT YOU WANT TO KEEP** you should export your parse data and extract it inside /data/parsedata in your bot's folder. Next time you start the bot, type `.parsetosql` and the bot will fill your local sqlite db with data from those .json files.

**Nothing was buffered music error?** make sure to follow the guide on google api key and ffmpeg [here](https://www.youtube.com/watch?v=x7v02MXNLeI)
  
Enjoy

##List of commands   

[**click here for a list of commands**](https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md)
