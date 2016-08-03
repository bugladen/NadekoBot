###Credentials.json and config.json
**This is how unedited credentials.json looks like:**
```json
{
  "Token": "",
  "ClientId": "116275390695079945",
  "BotId": 1231231231231,
  "OwnerIds": [
    123123123123,
    5675675679845
  ],
  "GoogleAPIKey": "",
  "SoundCloudClientID": "",
  "MashapeKey": "",
  "LOLAPIKey": "",
  "TrelloAppKey": "",
  "OsuAPIKey": "",
  "CarbonKey": ""
}
```
#### Required
- **Token** - Required to log in. See this [guide](http://discord.kongslien.net/guide.html)
- **OwnerIds** - Required for Owner-Only commands to work. Separate multiple IDs with a comma
- **BotId** - Required for custom reactions and conversation commands to work.

*BotId and OwnerIds are NOT names of the owner and the bot. If you do not know the id of your bot, put 2 random numbers in those fields, run the bot and do `.uid @MyBotName` - that will give you your bot\_id, do the same for yourself `.uid @MyName` and you will get a number to put inside brackets in OwnerIds field.*

#### Optional
- **GoogleAPIKey** - Youtube song search. Playlist queuing. URL Shortener. ~i and ~img. 
- **SoundCloudClientID** - Needed in order to queue soundcloud songs from sc links. For the Soundcloud Api key you need a Soundcloud account. You need to create a new app on http://soundcloud.com/you/apps/new and after that go here http://soundcloud.com/you/apps click on the name of your created your app and copy the Client ID. Paste it into credentials.json. 
- **MashapeKey** - Urban dictionary, hashtag search, hearthstone cards.You need to create an account on their api marketplace here https://market.mashape.com/. After that you need to go to market.mashape.com/YOURNAMEHERE/applications/default-application and press GET THE KEYS in the right top corner copy paste it into your credentials.json and you are ready to race! 
- **LOLAPIKey** - www.champion.gg api key needed for LoL commands
- **TrelloAppKey** - Needed for trello commands
- **OsuAPIKey** - needed for osu top5 and beatmap commands. 
- **CarbonKey** - carbonitex.net key if your bot is listed there in order to send stats (probably nobody needs this)

Next to your exe you must also have a data folder in which there is config.json (among other things) which will contain some user specific config, like should the Bot join servers, should DMs to bot be forwarded to you and a list of IDs of blacklisted users, channels and servers. If you do not have config.json, you can should config_example.json to config.json.
```