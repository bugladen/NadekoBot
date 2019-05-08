## Setting up your Credentials

This document aims to guide you through the process of creating a Discord account for your bot (the Discord Bot application), inviting that account into your Discord server and setting up the credentials necessary for the bot installed on your computer to be able to log into that account.

---

#### Creating the Discord Bot application

![img2](https://i.imgur.com/Vxxeh2n.gif)

- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- Create an application.
- On the **General Information** tab, fill out the `Name` field (it's your app's name)
- Upload an image if you want and add an app description. **(Optional)**
- Go to the **Bot** tab on the left sidebar.
- Click on the `Add a Bot` button and confirm that you do want to add a bot to this app.

#### Inviting your bot to your server

![img4](https://i.imgur.com/6beUSa5.gif)

- On the **General Information** tab, copy your `Client ID` from your [applications page][DiscordApp].
- Replace the **`12345678`** in this link:
  `https://discordapp.com/oauth2/authorize?client_id=`**`12345678`**`&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this:
  `https://discordapp.com/oauth2/authorize?client_id=`**`YOUR_CLIENT_ID_HERE`**`&scope=bot&permissions=66186303`
- Access that newly created link, pick your Discord server, click `Authorize` and confirm with the captcha at the end.
- The bot should have been added to your server.

#### Setting up credentials.json file

- **For Windows (Updater)**: the `credentials.json` file is located in the `C:\Program Files\NadekoBot\system` folder.
    - Note: there is a shortcut as well in `C:\Program Files\NadekoBot`, for easier access.
- **For Windows (Source), Linux and OSX**: the `credentials.json` file is located in the `NadekoBot/src/NadekoBot` folder.

##### Getting Client ID:

- On the **General Information** tab of your [applications page][DiscordApp], copy your `Client ID`.
- Open your `credentials.json` file and replace the `12312123` part of the **`"ClientId"`** line with it.
    - Be careful to not delete or move commas or quotation marks, this will break the file's syntax, making Nadeko unable to launch correctly.

It should look like this:

```json
"ClientId": 179372110000358912,
```

---

##### Getting the Bot's Token:

- On the **Bot** tab of your [applications page][DiscordApp], copy your `Token`.
    - *Note: Your bot Token **is not** the Client Secret! We won't need the Client Secret for anything.*
- Paste your bot token **between** the quotation marks on the **`"Token"`** line of your `credentials.json`.

It should look like this:

```json
"Token": "MTc5MzcyXXX2MDI1ODY3MjY0.ChKs4g.I8J_R9XX0t-QY-0PzXXXiN0-7vo",
```

##### Getting Owner ID*(s)*:

- Go to your Discord server and attempt to mention yourself, but put a backslash at the start:
  *(to make it slightly easier, add the backslash after you type the mention out)*
- For example, the message `\@fearnlj01#3535` will appear as `<@145521851676884992>` after you send the message.
- The message will appear as a mention if done correctly. Copy the numbers from it **`145521851676884992`** and replace the 0 on the `OwnerIds` section with your user ID.
- Save the `credentials.json` file.
- If done correctly, you should now be the bot owner. You can add multiple owners by seperating each owner ID with a comma within the square brackets.

For single owner, it should look like this:

```json
    "OwnerIds": [
        105635576866156544
    ],
```

For multiple owners, it should look like this (pay attention to the commas, the last ID should **never** have a comma next to it):

```json
    "OwnerIds": [
        105635123466156544,
        145521851676884992,
        341420590009417729
    ],
```

---

## Setting up your API keys

This part is completely optional, **however it's necessary for music and a few other features to work properly**.

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

It should look like this:

```json
"GoogleApiKey": "AIzaSyDSci1sdlWQOWNVj1vlXxxxxxbk0oWMEzM",
```

- **MashapeKey**
    - Required for Urban Disctionary, Hashtag search, and Hearthstone cards.
    - You need to create an account on their [api marketplace](https://market.mashape.com/), after that go to `market.mashape.com/YOURNAMEHERE/applications/default-application` and press **Get the keys** in the top right corner.
    - Copy the key and paste it into `credentials.json`
- **LoLApiKey**
    - Required for all League of Legends commands.
    - You can get this key [here](http://api.champion.gg/).
- **OsuApiKey**
    - Required for Osu commands
    - You can get this key [here](https://osu.ppy.sh/p/api).
- **CleverbotApiKey**
    - Required if you want to use Cleverobot. It's currently a paid service.
    - You can get this key [here](http://www.cleverbot.com/api/).
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
    - Required if the bot will be connected to more than 2500 servers.
    - Most likely unnecessary to change until your bot is added to more than 2500 servers.
- **RedisOptions**
    - Required if the Redis instance is not on localhost or on non-default port.
    - You can find all available options [here](https://stackexchange.github.io/StackExchange.Redis/Configuration.html).
- **RestartCommand**
    - Required if you want to be able to use the `.restart` command
    - If you're using the CLI installer or Linux/OSX, it's easier and more reliable setup Nadeko with auto-restart and just use `.die`

For Windows (Updater), add this to your `credentials.json`

```json
"RestartCommand": {
    "Cmd": "NadekoBot.exe"
},
```

For Windows (Source), Linux or OSX, add this to your `credentials.json`

```json
"RestartCommand": {
    "Cmd": "dotnet",
    "Args": "run -c Release"
},
```

---

#### End Result

**This is an example of how the `credentials.json` looks like with multiple owners, the restart command (optional) and all the API keys (also optional):**

```json
{
  "ClientId": 179372110000358912,
  "Token": "MTc5MzcyXXX2MDI1ODY3MjY0.ChKs4g.I8J_R9XX0t-QY-0PzXXXiN0-7vo",
  "OwnerIds": [
        105635123466156544,
        145521851676884992,
        341420590009417729
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
  "RestartCommand": {
    "Cmd": "NadekoBot.exe"
	},
  "ShardRunCommand": "",
  "ShardRunArguments": "",
  "ShardRunPort": null,
  "TwitchClientId": null,
  "RedisOptions": null
}
```

---

## Database

Nadeko saves all settings and data in the database file `NadekoBot.db`, located in:

- Windows (Updater): `C:\Program Files\NadekoBot\system\data`
- Windows (Source), Linux and OSX: `NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.1/data/NadekoBot.db`

In order to open it you will need [SQLite Browser](http://sqlitebrowser.org/).

*NOTE: You don't have to worry if you don't have the `NadekoBot.db` file, it gets automatically created once you successfully run the bot for the first time.*

**To make changes:**

- Shut your bot down.
- Copy the `NadekoBot.db` file to someplace safe. (Back up)
- Open it with SQLite Browser.
- Go to the **Browse Data** tab.
- Click on the **Table** drop-down list.
- Choose the table you want to edit.
- Click on the cell you want to edit.
- Edit it on the right-hand side.
- Click on **Apply**.
- Click on **Write Changes**.

![nadekodb](https://cdn.discordapp.com/attachments/251504306010849280/254067055240806400/nadekodb.gif)

---

## Sharding your bot

- **ShardRunCommand**
    - Command with which to run shards 1+
    - Required if you're sharding your bot on windows using .exe, or in a custom way.
    - This internally defaults to `dotnet`
    - For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to something like this: `C:\Program Files\NadekoBot\system\NadekoBot.exe`
- **ShardRunArguments**
    - Arguments to the shard run command
    - Required if you're sharding your bot on windows using .exe, or in a custom way.
    - This internally defaults to `run -c Release --no-build -- {0} {1} {2}` which will be enough to run linux and other 'from source' setups
    - {0} will be replaced by the `shard ID` of the shard being ran, {1} by the shard 0's process id, and {2} by the port shard communication is happening on
    - If shard0 (main window) is closed, all other shards will close too
    - For example, if you want to shard your NadekoBot which you installed using windows installer, you would want to set it to `{0} {1} {2}`
- **ShardRunPort**
    - Bot uses a random UDP port in [5000, 6000] range for communication between shards

[Google Console]: https://console.developers.google.com
[DiscordApp]: https://discordapp.com/developers/applications/me
[Invite Guide]: https://tukimoop.pw/s/guide.html
