________________________________________________________________________________
*Thanks to @Flatbread for making this guide*
________________________________________________________________________________

#### Setting Up NadekoBot v0.98
###### Prerequisites: 
1) NET Framework 4.5.2 (or 4.6)
- Start with making a folder, lets name it `Nadeko`
- Make sure you have **7zip** installed, if not then head to http://www.7-zip.org/download.html and download/install it.
- Now head to https://github.com/Kwoth/NadekoUpdater/releases/tag/v1.0 and download `WINDOWS.-.nadeupdater.7z
    - *Alternatively, you can download nadekobot from [releases](https://github.com/Kwoth/NadekoBot/releases) and extract the zip yourself. That is what updater does, except it makes it easier for you to update because it doesn't overwrite important files. If you are downloading releases you will have to be careful about your config, credentials, and other files you edited in order to preserve your data every time you update.*
- Copy `WINDOWS.-.nadeupdater.7z` to the `Nadeko` (folder we created before) and extract everything.
- You will see a file `NadekoUpdater.bat ` and a folder `publish ` after extraction.
- Run/Launch/Open the file `NadekoUpdater.bat ` and you will see it running in cmd.exe asking you with **3 options** *1-3*.
- You can try the stable release, but its better to get the newest release if you want all features/upgrades. So for that press `2 ` and hit `Enter ` (read everything shows up on screen)
- It should ask you `Are you sure you want to update?` and for that type `y ` and hit `Enter ` 
- It should complete downloading (might take a while) and you can type `3 ` and hit `Enter ` and close it.
- You should now see a new folder `NadekoBot ` inside our `Nadeko ` folder.
- Open it and head over to https://github.com/Kwoth/NadekoBot/blob/master/README.md to setup credentials or just check it out for your knowledge.
- **For Basic Credentials Setup:**
- Rename `credentials_example.json ` into `credentials.json ` (Note: If you do not see a **.json** after `credentials_example.json `, do not add the **.json**. `You are most likely to have "Hide file extensions" as enabled. `)
- **Go to:** http://discord.kongslien.net/guide.html **for detailed guidance with screenshots.** or :arrow_down:
- **Follow for textual guidelines:** Go here https://discordapp.com/developers/applications/me
- Log in with your Discord account. Press **New Application** and fill out an **App Name** and, optionally, an app description and icon. Afterwards, create the application. Once the application is created, click on **Create a Bot User** and confirm it. You will then see the bot's username, ID and token. Reveal and copy the **token** and the **bot ID** *(one by one xD)*.
- Open up `credentials.json` with **NotePad++** Paste the **token** into the **Token** field, between the `"quotes"`. Paste the **Bot ID** into the **BotID** field, when done, *Save* and *close* `credentials.json`
- **To Invite your bot to your server:**
- Copy your CLIENT ID (that's in the same Developer page where you brought your token) and replace `12345678` in this link: 
`https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303 ` with it. Go to that link and you will be able to add your bot to your server.
- Start NadekoBot.exe. In a text channel, **not a direct message**, type in [.uid @______] without the brackets, filling in the underlined portion with your name and send the message. Your bot will reply with a number; this is your ID. Copy this ID and close NadekoBot.exe.   
- Reopen credentials.json. Paste your ID into the square brackets ("OwnerIds": [1231312313]). **Just paste/replace your ID with existing and keep the default format intact, you don't need to add extra space or anything else.** You can add multiple owners by separating IDs with a comma. Close and save credentials.json.  


________________________________________________________________________________

#### Setting Up NadekoBot For Music
###### Setting up `ffmpeg` with installer:
1) Google Account  
2) Soundcloud Account (if you want soundcloud support)  
3) Download installer here: https://goo.gl/lQZnsH (pick the one for your system, either 32 or 64bit and then click 'download')  
4) Run the installer  

5) Follow these steps on how to setup API keys:
- Go to https://console.developers.google.com and log in.
- Create a new project (name does not matter). Once the project is created, go into "Enable and manage APIs."
- Under the "Other Popular APIs" section, enable "URL Shortener API". Under the "YouTube APIs" section, enable "YouTube Data API". Also enable Custom Search Api.
- On the left tab, access Credentials. There will be a line saying "If you wish to skip this step and create an API key, client ID or service account." Click on API Key, and then Server Key in the new window that appears. Enter in a name for the server key. A new window will appear with your Google API key. Copy the key.
- Open up credentials.json. For "GoogleAPIKey", fill in with the new key.
- Go to (https://soundcloud.com/you/apps/new). Enter a name for the app and create it. You will see a page with the title of your app, and a field labeled Client ID. Copy the ID. In credentials.json, fill in "SoundcloudClientID" with the copied ID.
- Restart your computer.

###### Prerequisites for manual `ffmpeg` setup: 
1) Google Account  
2) Soundcloud Account (if you want soundcloud support)
- Create a folder named `ffmpeg` in your main Windows directory. We will use **C:\ffmpeg** (for our guide)
- Download FFMPEG through the link https://ffmpeg.zeranoe.com/builds/ (download static build)
- Extract it using `7zip` and place the folder `ffmpeg-xxxxx-git-xxxxx-xxxx-static` inside **C:\ffmpeg**
- Before proceeding, check out this gif to set up `ffmpeg` PATH correctly http://i.imgur.com/aR5l1Hn.gif *(thanks to PooPeePants#7135)*
- Go to My Computer, right click and select Properties. On the left tab, select Advanced System Settings. Under the Advanced tab, select Environmental Variables near the bottom. One of the variables should be called "Path". Add a semi-colon (;) to the end followed by your FFMPEG's **bin** install location (**for example C:\ffmpeg\ffmpeg-xxxxx-git-xxxxx-xxxx-static\bin**). Save and close.
- Go to https://console.developers.google.com and log in.
- Create a new project (name does not matter). Once the project is created, go into "Enable and manage APIs."
- Under the "Other Popular APIs" section, enable "URL Shortener API". Under the "YouTube APIs" section, enable "YouTube Data API". Also enable Custom Search Api.
- On the left tab, access Credentials. There will be a line saying "If you wish to skip this step and create an API key, client ID or service account." Click on API Key, and then Server Key in the new window that appears. Enter in a name for the server key. A new window will appear with your Google API key. Copy the key.
- Open up credentials.json. For "GoogleAPIKey", fill in with the new key.
- Go to (https://soundcloud.com/you/apps/new). Enter a name for the app and create it. You will see a page with the title of your app, and a field labeled Client ID. Copy the ID. In credentials.json, fill in "SoundcloudClientID" with the copied ID.
- Restart your computer.
