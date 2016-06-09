________________________________________________________________________________
*Thanks to @Flatbread for making this guide*
________________________________________________________________________________

#### Setting Up NadekoBot v0.98
###### Prerequisites: 
1) NET Framework 4.5.2 (or 4.6)
- Start with making a folder, lets name it `Nadeko`
- Make sure you have **7zip** installed, if not then head to http://www.7-zip.org/download.html and download/install it.
- Now head to https://github.com/Kwoth/NadekoUpdater/releases/tag/v1.0 and download `WINDOWS.-.nadeupdater.7z`
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
###### Prerequisites: 
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

________________________________________________________________________________

#### Setting Up NadekoBot Permissions
###### NadekoBot's permissions can be set up to be very specific through commands in the Permissions module.  
Each command or module can be turned on or off at: 
- a user level (so specific users can or cannot use a command/module)  
- a role level (so only certain roles have access to certain commands/module)
- a channel level (so certain commands can be limited to certain channels, which can prevent music / trivia / NSFW spam in serious channels)
- a server level. 

Use .modules to see a list of modules (sets of commands).
Use .commands [module_name] to see a list of commands in a certain module.

Permissions use a semicolon as the prefix, so always start the command with a ;.

Follow the semicolon with the letter of the level which you want to edit.
- "u" for Users.
- "r" for Roles.
- "c" for Channels.
- "s" for Servers.

Follow the level with whether you want to edit the permissions of a command or a module.
- "c" for Command.
- "m" for Module.

Follow with a space and then the command or module name (surround the command with quotation marks if there is a space within the command, for example "!m q" or "!m n").

Follow that with another space and, to enable it, type one of the following: [1, true, t, enable], or to disable it, one of the following: [0, false, f, disable].

Follow that with another space and the name of the user, role, channel. (depending on the first letter you picked)

###### Examples
- **;rm NSFW 0 [Role_Name]**  Disables the NSFW module for the role, <Role_Name>.
- **;cc "!m n" 0 [Channel_Name]**  Disables skipping to the next song in the channel, <Channel_Name>.
- **;uc "!m q" 1 [User_Name]**  Enables queuing of songs for the user, <User_Name>.
- **;sm Gambling 0**  Disables gambling in the server.

Check permissions by using the letter of the level you want to check followed by a p, and then the name of the level in which you want to check. If there is no name, it will default to yourself for users, the @everyone role for roles, and the channel in which the command is sent for channels.

###### Examples 
- ;cp [Channel_Name]
- ;rp [Role_Name]

Insert an **a** before the level to edit the permission for all commands / modules for all users / roles / channels / server.

Reference the Help command (-h) for more Permissions related commands.
