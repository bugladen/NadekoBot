### Setting Up NadekoBot on OSX
#### Prerequisites 
- 1) [Homebrew][Homebrew]
- 2) Google Account
- 3) Soundcloud Account (if you want soundcloud support)
- 4) Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

####Installing Homebrew

`/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`

Run `brew update` to fetch the latest package data.
####Installing dependencies
```
brew install git
brew install ffmpeg
brew update && brew upgrade ffmpeg
brew install openssl
brew install opus
brew install opus-tools
brew install opusfile
brew install libffi
brew install libsodium
brew install tmux
```

####Installing .NET Core SDK
- `ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/`
- `ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/`
- Download the .NET Core SDK, found [here.](https://go.microsoft.com/fwlink/?LinkID=827526)
- Open the `.pkg` and install it.

####Check your `FFMPEG`

**In case your `FFMPEG` wasnt installed properly**

- `brew options ffmpeg`
- `brew install ffmpeg --with-x --with-y --with-z` etc.
- `brew update && brew upgrade` (Update formulae and Homebrew itself && Install newer versions of outdated packages)
- `brew prune` (Remove dead symlinks from Homebrew’s prefix)
- `brew doctor` (Check your Homebrew installation for common issues)
- Then try `brew install ffmpeg` again.

####Installing xcode-select
Xcode command line tools. You will do this in Terminal.app by running the following command line:

`xcode-select --install`

A dialog box will open asking if you want to install `xcode-select`. Select install and finish the installation.

####Downloading and building Nadeko
- `cd ~`
- `git clone -b 1.0 --recursive https://github.com/Kwoth/NadekoBot.git`
- `cd /NadekoBot/discord.net/src/Discord.Net`
- `dotnet restore && dotnet build --configuration Release`
- `cd ../Discord.Net.Commands/`
- `dotnet restore && dotnet build --configuration Release`
- `cd ../../../src/NadekoBot/`
- `dotnet restore && dotnet build --configuration Release`
- `dotnet run --configuration Release`
- The above step **will** crash, giving you an error, which will say that `credentials_example.json` has been generated, we'll use this soon

####Creating DiscordBot application
- Go to [the Discord developer application page.][DiscordApp]
- Log in with your Discord account.
- On the left side, press `New Application`.
- Fill out the `App Name` (your bot's name, in this case), put the image you want, and add an app description(optional).
- Create the application.
- Click on `Create a Bot User` and confirm it.
- Keep this window open for now.
 
####Setting up Credentials.json file
- Open up the `NadekoBot` folder, which should be in your home directory, then the `src` folder and then the additonal `NadekoBot` folder.
- In our `NadekoBot` folder you should have `.json` file named `credentials_example.json`. (Note: If you do not see a **.json** after `credentials_example.json `, do not add the `**.json**`. You most likely have `"Hide file extensions"` enabled.)
- Rename `credentials_example.json` to `credentials.json`.
- Open the file with your Text editor.
- In your [applications page][DiscordApp] (the window you were asked to keep open earlier), under the `Bot User` section, you will see `Token:click to reveal`, click to reveal the token.
- Copy your bot's token, and on the `"Token"` line of your `credentials.json`, replace `null` with your bot token and put quotation marks before and after the token, like so `"Example.Token"`
- Copy the `Client ID` on the page and replace the null part of the `ClientId` line with it, and put quotation marks before and after, like earlier.
- Again, copy the same `Client ID` and replace the null part of the `BotId` line with it, and do **not** put quotation marks before and after the ID.
- Save your `credentials.json` but keep it open. We need to add your `User ID` as one of the `OwnerIds` shortly.
 
####Running NadekoBot
 
`tmux new -s nadeko`

^this will create a new session named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your session name)`.

or if you want to use Screen, run:

`screen -S nadeko`

^this will create a new screen named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your screen name)`.

`cd ~/NadekoBot/src/NadekoBot/`

- Start Nadeko using dotnet:
 
`dotnet run --configuration Release`

CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING

Now time to move bot to background and to do that, press CTRL+B+D (this will ditach the nadeko session using TMUX)

*If you used Screen press CTRL+A+D (this will detach the nadeko screen)*

####Inviting your bot to your server - [Invite Guide][Invite Guide]
- Create a new server in Discord.
- Copy your `Client ID` from your [Discord bot applications page.][DiscordApp]
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- Your edited link should look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID**&scope=bot&permissions=66186303`.
- Go to newly created link and pick the server we created, and click `Authorize`.
- Bot should be added to your server.
 
####Setting up OwnerIds
- In the server where your bot is, in a text channel, type `.uid`
- Your `User ID` should show, copy it.
- Stop NadekoBot from running by presing `Ctrl + C` in the terminal that the bot is running in 
- Replace the `null` section on the `OwnerIds` line with your user ID shown earlier and put a square bracket around each end of the ID like so, `[105635576866156544]`
- Run Nadeko again, as guided above.
- If done correctly, you are now the bot owner.
- You can add multiple owner IDs by seperating them with a comma within the square brackets.

####Setting NadekoBot Music

For Music Setup and API keys check [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/1.0/guides/Windows%20Guide/#setting-up-nadekobot-for-music) and [JSON Explanations](http://nadekobot.readthedocs.io/en/1.0/JSON%20Explanations/).

####Updating Nadeko

Nadeko is really easy to update as of version 1.0! just copy and paste the command below to update Nadeko to the latest version

`cd ~/NadekoBot/ && git init && git pull`

####Some more Info - TMUX

- If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
- If you want to switch to/ see that session, type `tmux a -t nadeko` (nadeko is the name of the session we created before so, replace `“nadeko”` with the session name you created.)
- If you want to kill NadekoBot session, type `tmux kill-session -t nadeko`

####Some more Info - Screen

- If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
- If you want to switch to/ see that screen, type `screen -r nadeko` (nadeko is the name of the screen we created before so, replace `“nadeko”` with the screen name you created.)
- If you want to kill the NadekoBot screen, type `screen -X -S nadeko quit`

[Homebrew]: http://brew.sh/
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
