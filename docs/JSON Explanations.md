###Setting up your Credentials
If you do not see `credentials.json` you will need to rename `credentials_example.json` to `credentials.json`.

**This is how the unedited credentials look:**
```json
{
  "ClientId": 123123123,
  "BotId": null,
  "Token": "",
  "OwnerIds": [
    0
  ],
  "LoLApiKey": "",
  "GoogleApiKey": "",
  "MashapeKey": "",
  "OsuApiKey": "",
  "SoundCloudClientId": "",
  "CarbonKey": "",
  "Db": null,
  "TotalShards": 1
}
```
####Required Parts
- **Token** - Required to log in. Refer to this [guide](http://discord.kongslien.net/guide.html)
- **OwnerIds** - Required for the **Owner-Only** commands. Seperate multiple Id's with a comma.
- **BotId** - Required for custom reactions to work.  
  - **Important : Bot ID and Client ID will be the same in newer bot accounts due to recent changes by Discord.** 

_BotId and the OwnerIds are **NOT** the names of the owner and the bot. If you do not know the id of your bot, keep the two random numbers in those fields and 
run the bot then do  `.uid @MyBotName` - this will give you your bot_id.
Do the same for yourself with `.uid @MyName` Put these numbers in their respective field of the credentials._

Setting up your API keys
====================
####This part is completely optional, **However it is necessary for music to work properly**
- **GoogleAPIKey** - Required for Youtube Song Search, Playlist queuing, and URL Shortener. `~i` and `~img`. 
  You can get this api Key [here](https://console.developers.google.com/apis)
- **SoundCloudClientID** - Required to queue soundloud songs from sc links.
  You will need to create a new app [here](http://soundcloud.com/you/apps). **Please note you must be logged into SoundCloud**
    - Simply click Register a new application and enter a name.
    - Copy the Client ID and click "save app" then paste the Client Id it into your `credentials.json` 
- **MashapeKey** - Required for Urban Disctionary, Hashtag search, and Hearthstone cards.
  You need to create an account on their [api marketplace](https://market.mashape.com/), after that go to `market.mashape.com/YOURNAMEHERE/applications/default-application` and press **Get the keys** in the top right corner.
    - Copy the key and paste it into `credentials.json`
- **LOLAPIKey** - Required for all League of Legends commands. 
  You can get this key [here](http://api.champion.gg/)
- **OsuAPIKey** - Required for Osu commands
  You can get this key [here](https://osu.ppy.sh/p/api) **You will need to log in and like the soundcloud it may take a few tries**
- **CarbonKey** -This key is for Carobnitex.net stats. 
  Most likely unnecessary **Needed only if your bot is listed on Carbonitex.net**
  
Additional options
==================== 
- **TotalShards** - Required if the bot will be connected to more than 2500 servers 
  Most likely unnecessary to change until your bot is added to more than 2000 servers  
[//]: # (- **Db** - Allows for advanced database configuration  )
[//]: # (  - Leave this with the `null` value for standard operation - change this to `examples` to [This is only a comment so doesn't need proper detail])
  

Config.json
===========
`config.json` is now removed with the addition of `NadekoBot.db` so if you have Nadeko 0.9x follow the [upgrading guide](http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/) to upgrade your bot.

DB files
========
Nadeko uses few db files in order to open these database files `NadekoBot\src\NadekoBot\bin\Release\netcoreapp1.0\data\NadekoBot.db` (1.0) or `data\NadekoBot.sqlite` (0.9x) you will need [DB Browser for SQLite](http://sqlitebrowser.org/).

To make changes

- go to **Browse Data** tab
- click on **Table** drop-down list
- choose the table you want to edit
- click on the cell you want to edit
- edit it on the right-hand side 
- click on **Apply** 
- click on **Write Changes**

and that will save all the changes.

![nadekodb](https://cdn.discordapp.com/attachments/251504306010849280/254067055240806400/nadekodb.gif)

[CleverBot APIs]: https://cleverbot.io/keys
