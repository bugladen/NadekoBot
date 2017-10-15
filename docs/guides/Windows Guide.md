## Setting Up NadekoBot on Windows

#### Prerequisites 
- [Notepad++][Notepad++] (or some other decent text editor)
- Windows 8 or later
- [Create Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 

#### Guide 
- Download and run the [NadekoBot Updater.][Updater]
- Press **`Install Redis`** then  
- Press **`Install ffmpeg`** and **`Install youtube-dl`** if you want music features.  
***NOTE:** RESTART YOUR PC IF YOU DO.*
- Press **`Update`** and go through the installation wizard.			
***NOTE:** If you're upgrading from 1.3, DO NOT select your old nadekobot folder. Install it in a separate directory and read the [upgrading guide](http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/).*
- When installation is finished, make sure **`Open credentials.json`** is checked. 			
***NOTE:** Make sure to open it with Notepad++ or some other decent text editor.*
- [Set up credentials.json](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file) file.

#### Starting the bot
- Either press **`Start`** Button in the updater, or run the bot via it's desktop shortcut.

#### Updating NadekoBot
- Make sure the bot is closed and is not running 			
(Run `.die` in a connected server to ensure it's not running).
- Open NadekoBot Updater
- If updates are available, you will be able to click on the Update button
- Start the bot
- You've updated and are running again, easy as that!

[Updater]: https://download.nadekobot.me/
[DiscordApp]: https://discordapp.com/developers/applications/me
[Notepad++]: https://notepad-plus-plus.org/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[.NET Core SDK]: https://www.microsoft.com/net/core#windowscmd
