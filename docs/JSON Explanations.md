###Setting up your Credentials
If you do not see `credentials.json` you will need to rename `credentials_example.json` to `credentials.json`.

**This is how the unedited credentials look:**
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
  "LOLAPIKEY": "",
  "TrelloAPPKey": "",
  "OsuAPIKey": "",
  "CarbonKey": "",
}
```
####Required Parts
+ **Token** - Required to log in. Refer to this [guide](http://discord.kongslien.net/guide.html)
+ **OwnerIds** - Required for the **Owner-Only** commands. Seperate multiple Id's with a comma.
+ **BotId** - Required for custom reactions and conversation commands to work.  
  + **Important : Bot ID and Client ID are the same in newer bot accounts due to recent Discord API changes.** 

_BotId and the OwnerIds are **NOT** the names of the owner and the bot. If you do not know the id of your bot, keep the two random numbers in those fields and 
run the bot then do  `.uid @MyBotName` - this will give you your bot_id.
Do the same for yourself with `.uid @MyName` Put these numbers in their respective field of the credentials._

Setting up your API keys
====================
####This part is completely optional, **However it is necessary for music to work properly**
+ **GoogleAPIKey** - Required for Youtube Song Search, Playlist queuing, and URL Shortener. `~i` and `~img`. 
  + You can get this api Key [here](https://console.developers.google.com/apis)
+ **SoundCloudClientID** - Required to queue soundloud songs from sc links.
  + You will need to create a new app [here](http://soundcloud.com/you/apps). **Please note you must be logged into SoundCloud**
    + You should come to a page that looks like this ![Imgur](http://i.imgur.com/RAZ2HDM.png)
    + Simply click Register a new application and enter a name.
    + After naming your app you will be brought to this page: ![Imgur](http://i.imgur.com/GH1gjKK.png) Copy the Client ID and click "save app" then paste the Client Id it into your `credentials.json` 
+ **MashapeKey** - Required for Urban Disctionary, Hashtag search, and Hearthstone cards.
  + You need to create an account on their [api marketplace](https://market.mashape.com/), after that go to `market.mashape.com/YOURNAMEHERE/applications/default-application` and press **Get the keys** in the top right corner.
    + Copy the key and paste it into `credentials.json`
+ **LOLAPIKey** - Required for all League of Legends commands. 
  + You can get this key [here](http://api.champion.gg/)
+ **TrelloAppKey** - Required for the trello commands.
  + You can get this key [here](https://trello.com/app-key) **Be sure you are logged into Trello first**
+ **OsuAPIKey** - Required for Osu commands
  + You can get this key [here](https://osu.ppy.sh/p/api) **You will need to log in and like the soundcloud it may take a few tries**
+ **CarbonKey** -This key is for Carobnitex.net stats. 
  + Most likely unnecessary **Needed only if your bot is listed on Carbonitex.net**

Config.json
===========
In the folder where `NadekoBot.exe` is located you should also see a `Data` folder. In this folder you will find `config.json` among other files.
`config.json` contains user specific commands, such as: if DM's sent to the bot are forwarded to you, Blacklisted Ids, Servers, and channels...etc.

**If you do not see** `config.json` **you need to rename** `config_example.json` **to** `config.json`
