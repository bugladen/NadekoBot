## Setting Up NadekoBot on Windows

#### Prerequisites 
- [Notepad++][Notepad++] (or some other decent text editor)
- Windows 8 or later

#### Guide 
- Download and run the [NadekoBot Updater][Updater]
- Press 'Install ffmpeg' button if you want music features 
*note: RESTART YOUR PC IF YOU DO*
- Press `Update` and go through the installation wizard
*note: If you're upgrading from 1.3, DO NOT select your old nadekobot folder. Install it in a separate directory and read the upgrading guide*
- When installation is finished, make sure 'open credentials.json' is checked. 
*note: Make sure to open it with Notepad++ or some other decent text editor.*

#### Creating DiscordBot application
- Go to [the Discord developer application page][DiscordApp].
- Log in with your Discord account.
- On the left side, press `New Application`.
- Fill out the `App Name` (your bot's name, in this case), put the image you want, and add an app description(optional).
- Create the application.
- Click on `Create a Bot User` and confirm that you do want to add a bot to this app.
- Keep this window open for now.
![img2](http://i.imgur.com/x3jWudH.gif)

#### Setting up credentials.json file
- In there you will see fields such as `Token`, `ClientId`, and `OwnerIDs`.
- In your [applications page][DiscordApp] (the window you were asked to keep open earlier), under the `Bot User` section, you will see `Token:click to reveal`, click to reveal the token. (Note: Make sure that you actually use a Token and not a Client Secret! It is in the **App Bot User** tab.)
- Copy your bot's token, and on the `"Token"` line of your `credentials.json`, paste your bot token **between** the quotation marks.
- Copy the `Client ID` on the page and replace the `12312123` part of the `ClientId` line with it.
- Go to a server on discord and attempt to mention yourself, but put a backslash at the start like shown below
- So the message `\@fearnlj01#3535` will appear as `<@145521851676884992>` after you send the message (to make it slightly easier, add the backslash after you type the mention out)
- The message will appear as a mention if done correctly, copy the numbers from the message you sent (`145521851676884992`) and replace the ID (By default, the ID is `105635576866156544`) on the `OwnerIds` section with your user ID shown earlier.
- Save `credentials.json`
- If done correctly, you are now the bot owner. You can add multiple owners by seperating each owner ID with a comma within the square brackets.
![img3](http://i.imgur.com/QwKMnTG.gif)

#### Inviting your bot to your server 
- [Invite Guide][Invite Guide]
- Copy your `Client ID` from your [applications page][DiscordApp].
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID_HERE**&scope=bot&permissions=66186303`.
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.
![img4](http://i.imgur.com/aFK7InR.gif)

#### Starting the bot
- Either press "Start" Button in the updater, or run the bot via it's desktop shortcut.

#### Updating NadekoBot
- Make sure the bot is closed and is not running (Run `.die` in a connected server to ensure it's not running).
- Open NadekoBot Updater
- If updates are available, you will be able to click on the Update button
- Start the bot
- You've updated and are running again, easy as that!

[Updater]: https://download.nadekobot.me/
[DiscordApp]: https://discordapp.com/developers/applications/me
[Notepad++]: https://notepad-plus-plus.org/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
