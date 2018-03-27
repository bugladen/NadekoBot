## Setting up your Credentials
If you do not see `credentials.json` you will need to rename `credentials_example.json` to `credentials.json`.

**This is how the `credentials.json` looks with all the APIs:**
```json
{
  "ClientId": 179372110000358912,
  "Token": "MTc5MzcyXXX2MDI1ODY3MjY0.ChKs4g.I8J_R9XX0t-QY-0PzXXXiN0-7vo",
  "OwnerIds": [
    105635123466156544,
	105635123566156544,
	105635123666156544
  ],
  "LoLApiKey": "6e99ecf36f0000095b0a3ccfe35df45f",
  "GoogleApiKey": "AIzaSyDSci1sdlWQOWNVj1vlXxxxxxbk0oWMEzM",
  "MashapeKey": "4UrKpcWXc2mshS8RKi00000y8Kf5p1Q8kI6jsn32bmd8oVWiY7",
  "OsuApiKey": "4c8c8fdff8e1234581725db27fd140a7d93320d6",
  "CleverbotApiKey": "",
  "Db": null,
  "TotalShards": 1,
  "PatreonAccessToken": "",
  "PatreonCampaignId": "334038",
  "RestartCommand": null,
  "ShardRunCommand": "",
  "ShardRunArguments": "",
  "ShardRunPort": null,
  "TwitchClientId": null,
}
```
-----
#### Creating Discord Bot application
![img2](http://i.imgur.com/x3jWudH.gif)

- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- On the left side, press `New Application`.
- Fill out the `App Name` (Your bot's name, in this case)
- Put the image if you want, and add an app description. **(Optional)**
- Create the application.
- Click on `Create a Bot User` and confirm that you do want to add a bot to this app.
- **Keep this window open for now.**


#### Inviting your bot to your server 
![img4](http://i.imgur.com/aFK7InR.gif)

- [Invite Guide][Invite Guide]
- Copy your `Client ID` from your [applications page][DiscordApp].
- Replace the **`12345678`** in this link: 			
`https://discordapp.com/oauth2/authorize?client_id=`**`12345678`**`&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: 			
`https://discordapp.com/oauth2/authorize?client_id=`**`YOUR_CLIENT_ID_HERE`**`&scope=bot&permissions=66186303`
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.


#### Setting up credentials.json file
**For Windows** you can find `credentials.json` file in `NadekoBot\system` folder. 			
e.g. `C:\Program Files\NadekoBot\system`			
**For Linux** you can find `credentials.json` in `NadekoBot/src/NadekoBot` folder. 			
e.g. `/root/NadekoBot/src/NadekoBot`			

![img3](http://i.imgur.com/QwKMnTG.gif)

##### Getting Bot's Token:
- In your [Discord applications page][DiscordApp], under the **`APP BOT USER`** section, you will see `Token:click to reveal`, click to reveal the token. 		
*Note: Make sure that you actually use a Token and not a Client Secret!* It is in the **App Bot User** section.
- Copy your bot's token, and on the **`"Token"`** line of your `credentials.json`, paste your bot token **between** the quotation marks.
```
It should look like:
```
```json
"Token": "MTc5MzcyXXX2MDI1ODY3MjY0.ChKs4g.I8J_R9XX0t-QY-0PzXXXiN0-7vo",
```
##### Getting Client ID:
- Copy the `Client ID` on the page and replace the `12312123` part of the **`"ClientId"`** line with it.
```
It should look like:
```
```json
"ClientId": 179372110000358912,
```
-----
##### Getting Owner ID*(s)*:		

- Go to your Discord server and attempt to mention yourself, but put a backslash at the start like shown below: 			
*(to make it slightly easier, add the backslash after you type the mention out)*
- So the message `\@fearnlj01#3535` will appear as `<@145521851676884992>` after you send the message. 
- The message will appear as a mention if done correctly, copy the numbers from the message **`145521851676884992`** and replace the ID (By default, the ID is `105635576866156544`) on the `OwnerIds` section with your user ID shown earlier.
- Save `credentials.json`
- If done correctly, you are now the bot owner. You can add multiple owners by seperating each owner ID with a comma within the square brackets.


```
For single owner it should look like:
```

	
```json
	"OwnerIds": [
		105635576866156544
	],
``` 		

```
For multiple owners it should look like:
```
	
```json
	"OwnerIds": [
		105635123466156544,
		105635123566156544,
		105635123666156544
	],
``` 
-----

## Setting up your API keys

This part is completely optional, **However it is necessary for music and few other features to work properly**			

- **GoogleAPIKey** 
	- Required for Youtube Song Search, Playlist queuing, URL Shortener and lot more.
	- Follow these steps on how to setup Google API keys:
		- Go to [Google Console][Google Console] and log in.
		- Create a new project (name does not matter). 
		- Once the project is created, go into **`Library`**
		- Under the **`Other Popular APIs`** section, enable `URL Shortener API` and `Custom Search API` 
		- Under the **`YouTube APIs`** section, enable `YouTube Data API`
		- Under the **`Google Maps APIs`** section, enable `Google Maps Geocoding API` and `Google Maps Time Zone API`
		- On the left tab, access **`Credentials`**, 
			- Click `Create Credentials` button, 
			- Click on `API Key` 
			- A new window will appear with your `Google API key` 			
			*NOTE: You don't really need to click on `RESTRICT KEY`, just click on `CLOSE` when you are done.*			
			- Copy the key. 
			
		- Open up **`credentials.json`** and look for **`"GoogleAPIKey"`**, paste your API key inbetween the quotation marks.
```
It should look like:
```

```json
"GoogleApiKey": "AIzaSyDSci1sdlWQOWNVj1vlXxxxxxbk0oWMEzM",
```
- **MashapeKey** 
	- Required for Urban Disctionary, Hashtag search, and Hearthstone cards.
	- You need to create an account on their [api marketplace](https://market.mashape.com/), after that go to `market.mashape.com/YOURNAMEHERE/applications/default-application` and press **Get the keys** in the top right corner.
    - Copy the key and paste it into `credentials.json`
- **LoLApiKey** 
	- Required for all League of Legends commands. 
    - You can get this key [here.](http://api.champion.gg/)
- **OsuApiKey** 
	- Required for Osu commands
	- You can get this key [here.](https://osu.ppy.sh/p/api) 
- **CleverbotApiKey**
	- Required if you want to use official cleverobot, instead of program-o
	- you can get this key [here.](http://www.cleverbot.com/api/)
- **PatreonAccessToken**
	- For Patreon creators only.
- **PatreonCampaignId**
	- For Patreon creators only. Id of your campaign.
- **TwitchClientId**
	- Optional. In order to avoid ratelimits that may happen if you use .twitch/.stadd function extensively.
	- [How to get it](https://blog.twitch.tv/client-id-required-for-kraken-api-calls-afbb8e95f843)
		- Go to [connections page](https://www.twitch.tv/settings/connections) on twitch and register you applicaiton.
		- Once registered, find your application under Other Connections on the Connections page. Click Edit
		- You will see your Client ID on the edit page.

##### Additional Settings

- **TotalShards** 
	- Required if the bot will be connected to more than 1500 servers. 
	- Most likely unnecessary to change until your bot is added to more than 1500 servers.
- **RestartCommand**
	- Required if you want to be able to use `.restart` command
	- It requires command, and arguments to the command which to execute right before bot stops
	- If you're using linux, it's easier, and more reliable to use auto restart option, and just use `.die`

For linux, or from the source, this is usually
```json
"RestartCommand": {
	"Cmd": "dotnet",
	"Args": "run -c Release"
},
```

For windows (regular installation, or from the updater), this is usually

```json
"RestartCommand": {
	"Cmd": "NadekoBot.exe"
},
```
-----

## DB files

Nadeko saves all the settings and infomations in `NadekoBot.db` file here:		
`NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data/NadekoBot.db` (macOS and Linux) 		
`NadekoBot\system\data` (Windows)  

in order to open the database file you will need [DB Browser for SQLite](http://sqlitebrowser.org/).

*NOTE: You don't have to worry if you don't have `NadekoBot.db` file, it gets auto created once you run the bot successfully.*		

**To make changes:**

- Copy the `NadekoBot.db` to someplace safe. (Back up)
- Open `NadekoBot.db`
- go to **Browse Data** tab
- click on **Table** drop-down list
- choose the table you want to edit
- click on the cell you want to edit
- edit it on the right-hand side 
- click on **Apply** 
- click on **Write Changes**

![nadekodb](https://cdn.discordapp.com/attachments/251504306010849280/254067055240806400/nadekodb.gif)

and that will save all the changes.

-----

## Sharding your bot

- **ShardRunCommand**
	- Command with which to run shards 1+
	- Required if you're sharding your bot on windows using .exe, or in a custom way.
	- This internally defaults to `dotnet`
	- For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to something like this: `C:\Program Files\NadekoBot\system\NadekoBot.exe`
- **ShardRunArguments**
	- Arguments to the shard run command
	- Required if you're sharding your bot on windows using .exe, or in a custom way. 
	- This internally defaults to `run -c Release -- {0} {1} {2}` which will be enough to run linux and other 'from source' setups
	- {0} will be replaced by the `shard ID` of the shard being ran, {1} by the shard 0's process id, and {2} by the port shard communication is happening on
	- If shard0 (main window) is closed, all other shards will close too
	- For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to `{0} {1} {2}`
- **ShardRunPort**
	- Bot uses a random UDP port in [5000, 6000) range for communication between shards

[Google Console]: https://console.developers.google.com
[DiscordApp]: https://discordapp.com/developers/applications/me
[Invite Guide]: https://tukimoop.pw/s/guide.html
