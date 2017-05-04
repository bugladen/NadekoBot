________________________________________________________________________________
*Thanks to @Flatbread and @Mirai for making this guide*
________________________________________________________________________________

## Setting Up NadekoBot on Windows

#### Prerequisites 
- 1) [.NET Core SDK][.NET Core SDK]
- 2) [Git][Git]
- 3) Google Account
- 4) Soundcloud Account (if you want soundcloud support)
- 5) [7zip][7zip] (or whatever you are using, WinRar)
- 6) [Notepad++][Notepad++]
- 7) Windows 8 or later

#### Guide 
- Make sure you have installed both [Git][Git] and the [.NET Core SDK][.NET Core SDK].
- Create a **new folder** anywhere you like and name it `Nadeko`.
- Next, [Right-Click on this link](https://github.com/Kwoth/NadekoBotInstallerWin/raw/master/NadekoInstaller.bat) and select **Save link as** and save the file `NadekoInstaller.bat` inside the `Nadeko` folder that we created earlier. (Please **DO NOT** rename the file `NadekoInstaller.bat`.)
- Once that's done, right-click on `NadekoInstaller.bat` to run it as Administrator.
- From the options, 
	- Choose `1` to get the **most recent build**.
	- Choose `2` to get the **stable build**.
- Wait a while for the file to finish installing, it'll display it's progress in the command prompt.
- You should now have a new folder named `NadekoBot` inside the `Nadeko` folder we previously created.
- Once Installation is completed, press any key to close the command prompt.
![img1](http://i.imgur.com/O1dY9eW.gif)

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
- In our `NadekoBot` folder you should see a `src` folder, then *another* `NadekoBot` folder, in this final folder, you should see a `.json` file named `credentials.json`. (Note: If you do not see a `.json` after `credentials.json`, do not add the `.json`. You most likely have **"Hide file extensions"** enabled.)
- If you mess up the setup of `credentials.json`, rename `credentials_example.json` to `credentials.json`.
- Open the file with [Notepad++][Notepad++].
- In there you will see fields such as `Token`, `ClientId`, `BotId` and `OwnerIDs`.
- In your [applications page][DiscordApp] (the window you were asked to keep open earlier), under the `Bot User` section, you will see `Token:click to reveal`, click to reveal the token. (Note: Make sure that you actually use a Token and not a Client Secret! It is in the **App Bot User** tab.)
- Copy your bot's token, and on the `"Token"` line of your `credentials.json`, paste your bot token between the quotation marks.
- Copy the `Client ID` on the page and replace the `12312123` part of the `ClientId` line with it.
- Again, copy the same `Client ID` and replace the `null` part of the `BotId` line with it.
- Go to a server on discord and attempt to mention yourself, but put a backslash at the start like shown below
- So the message `\@fearnlj01#3535` will appears as `<@145521851676884992>` after you send the message (to make it slightly easier, add the backslash after you type the mention out)
- The message will appear as a mention if done correctly, copy the numbers from the message you sent (`145521851676884992`) and replace the ID (By default, the ID is `105635576866156544`) on the `OwnerIds` section with your user ID shown earlier.
- Save `credentials.json` (make sure you aren't saving it as `credentials.json.txt`)
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
- Go to the `Nadeko` folder that we have created earlier, and run the `NadekoInstaller.bat` file as Administrator.
- From the options,
	- Choose `3` to **run the bot normally**.	
	(with normal-run the bot will shutdown and will stay offline if it disconnects by the use of `.die` command until you manually run it again. Useful if you want to test the bot.)
	- Choose `4` to **run the bot with auto restart**.	
	(with auto restart the bot will restart itself if it disconnects by the use of `.die` command. Useful if you want to have restart function for any reason.)

#### Updating NadekoBot
- Make sure the bot is closed and is not running (Run `.die` in a connected server to ensure it's not running).
- Once that's checked, go to the `Nadeko` folder.
- Run the `NadekoInstaller.bat` file.
- From the options, 
	- Choose `1` to get the **most recent build**.
	- Choose `2` to get the **stable build**.
- Follow the messages and press any key to continue.
- Wait for it to finish. 
- Press any key to close the window when it shows **Installation complete.** 
- [Start the bot again.](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#starting-the-bot)
- You've updated and are running again, easy as that!
________________________________________________________________________________

### Setting Up NadekoBot For Music

In order to have a functioning music module, you need to install ffmpeg and setup api keys.

#### Setting up `ffmpeg` using NadekoBot Client!
- Go to the `Nadeko` folder that we have created earlier, and run the `NadekoInstaller.bat` file as Administrator.
- From the options select `6` Install ffmpeg (for music)
- Next, **Press Any Key** if you are running as Administrator or just close and relaunch it as Administrator using mouse right-click.
- Wait for it to finish installing and backing up existing. 
- Once done, you should see "ffmpeg Installation complete!".
- Next, **Press Any Key** to go back to NadekoBot Client.
- Press `3` to run the bot normally just to test music. (optional)
- `ffmpeg` installation for Music is now complete.

#### Manual `ffmpeg` setup 
- Create a folder named `ffmpeg` in your main Windows directory. We will use **C:\ffmpeg** (for our guide)
- Download FFMPEG through the link https://ffmpeg.zeranoe.com/builds/ (download static build)
- EXTRACT it using `7zip` and place the FOLDER `ffmpeg-xxxxx-git-xxxxx-xxxx-static` inside **C:\ffmpeg**
- Before proceeding, check out this gif to set up `ffmpeg` PATH correctly ![LINK TO gif](http://i.imgur.com/aR5l1Hn.gif) *(thanks to PooPeePants#7135)*
- Go to My Computer, right click and select Properties. On the left tab, select Advanced System Settings. Under the Advanced tab, select Environmental Variables near the bottom. One of the variables should be called "Path". Add a semi-colon (;) to the end followed by your FFMPEG's **bin** install location (**for example C:\ffmpeg\ffmpeg-xxxxx-git-xxxxx-xxxx-static\bin**). Save and close.
- Setup your API keys as explained above.
- **Restart your computer**

#### Api keys setup
- Follow these steps on how to setup Google API keys:
    - Go to [Google Console][Google Console] and log in.
    - Create a new project (name does not matter). Once the project is created, go into "Enable and manage APIs."
    - Under the "Other Popular APIs" section, enable `URL Shortener API` and `Custom Search API`. Under the `YouTube APIs` section, enable `YouTube Data API`.
    - Under the "Google Maps APIs" section, enable `Google Maps Geocoding API` and `Google Maps Time Zone API`.
    - On the left tab, access `Credentials`. Click `Create Credentials` button. Click on `API Key`. A new window will appear with your `Google API key`. 
    - Copy the key.
    - Open up `credentials.json`. 
    - For `"GoogleAPIKey"`, paste your API key inbetween the quotation marks, like how the `Token` and `ClientId` should be set up.
- Follow these steps on how to setup Soundcloud API key:
    - Go to [Soundcloud][Soundcloud]. 
    - Enter a name for the app and create it. 
    - You will need to fill out an application form to request access to the Soundcloud API.
    - All requests for an API key must go through the review process, where applications will be reviewed on a case by case basis, in line with Soundcloud API Terms of Use. If your application is successful, you will receive an API key. 
- **Restart your computer**

   



[.NET Core SDK]: https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.1-preview2.1-download.md
[Git]: https://git-scm.com/download/win
[7zip]: http://www.7-zip.org/download.html
[DiscordApp]: https://discordapp.com/developers/applications/me
[Notepad++]: https://notepad-plus-plus.org/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
