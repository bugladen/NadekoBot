### Setting Up NadekoBot on OSX
#### Prerequisites 
- [Homebrew][Homebrew]
- Google Account
- Soundcloud Account (if you want soundcloud support)
- Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

####Installing Homebrew

```/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"```

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
- Open the `.pkg` file you downloaded and install it.
- `ln -s /usr/local/share/dotnet/dotnet /usr/local/bin`

####Check your `FFMPEG`

**In case your `FFMPEG` wasnt installed properly (Optional)**

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

**METHOD I**

- `cd ~`
- `curl -L https://github.com/Kwoth/NadekoBot-BashScript/raw/master/nadeko_installer.sh | sh`

####Creating DiscordBot application
- Go to [the Discord developer application page.][DiscordApp]
- Log in with your Discord account.
- On the left side, press `New Application`.
- Fill out the `App Name` (your bot's name, in this case), put the image you want, and add an app description(optional).
- Create the application.
- Click on `Create a Bot User` and confirm it.
- Keep this window open for now.
 
####Setting up Credentials.json file
- Open up the `NadekoBot` folder, which should be in your home directory, then `NadekoBot` folder then `src` folder and then the additonal `NadekoBot` folder.
- In our `NadekoBot` folder you should have `.json` file named `credentials.json`. (Note: If you do not see a **.json** after `credentials.json `, do not add the `**.json**`. You most likely have `"Hide file extensions"` enabled.)
- If you mess up the setup of `credentials.json`, rename `credentials_example.json` to `credentials.json`.
- Open the file with your Text editor.
- In your [applications page][DiscordApp] (the window you were asked to keep open earlier), under the `Bot User` section, you will see `Token:click to reveal`, click to reveal the token.
- Copy your bot's token, and on the `"Token"` line of your `credentials.json`, paste your bot token inbetween the quotation marks before and after the token, like so `"Example.Token"`
- Copy the `Client ID` on the page and replace the `123123123` part of the `ClientId` line with it, and put quotation marks before and after, like earlier.
- Again, copy the same `Client ID` and replace the null part of the `BotId` line with it, and do **not** put quotation marks before and after the ID.
- Go to a server on discord and attempt to mention yourself, but put a backslash at the start as shown below
- So the message `\@fearnlj01#3535` will appears as `<@145521851676884992>` after you send the message (to make it slightly easier, add the backslash after you type the mention out)
- The message will appear as a mention if done correctly, copy the numbers from the message you sent (`145521851676884992`) and replace the `0` on the `OwnerIds` section with your user ID shown earlier.
- Save `credentials.json` (make sure you aren't saving it as `credentials.json.txt`)
- If done correctly, you are now the bot owner. You can add multiple owners by seperating each owner ID with a comma within the square brackets.
 
####Running NadekoBot

- Using tmux

`tmux new -s nadeko`

^this will create a new session named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your session name)`.

- Using Screen

`screen -S nadeko`

^this will create a new screen named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your screen name)`.

- Start Nadeko using .NET Core:

`cd ~/NadekoBot/src/NadekoBot/`

`dotnet run --configuration Release`

CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING

Now time to move bot to background and to do that, press CTRL+B+D (this will detach the nadeko session using TMUX)

*If you used Screen press CTRL+A+D (this will detach the nadeko screen)*

####Inviting your bot to your server 
- [Invite Guide](http://discord.kongslien.net/guide.html)
- Copy your `Client ID` from your [applications page](https://discordapp.com/developers/applications/me).
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID_HERE**&scope=bot&permissions=66186303`.
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.  
  
####Setting NadekoBot Music

For Music Setup and API keys check [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/1.0/guides/Windows%20Guide/#setting-up-nadekobot-for-music) and [JSON Explanations](http://nadekobot.readthedocs.io/en/1.0/JSON%20Explanations/).

####Updating Nadeko

Nadeko is really easy to update as of version 1.0! just copy and paste the command below to update Nadeko to the latest version

`cd ~/NadekoBot/ && git init && git pull`

####Alternative Method to Install Nadeko

*If you fail to install the bot using [METHOD I](http://nadekobot.readthedocs.io/en/1.0/guides/OSX%20Guide/#downloading-and-building-nadeko) try:*

**METHOD II**

- `cd ~`
- `git clone -b 1.0 --recursive https://github.com/Kwoth/NadekoBot.git`
- `cd ~/NadekoBot/discord.net`
- `dotnet restore -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json`
- `dotnet restore`
- `cd ~/NadekoBot/src/NadekoBot/`
- `dotnet restore` 
- `dotnet build --configuration Release`

####Some more Info

**TMUX**

- If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
- If you want to switch to/ see that session, type `tmux a -t nadeko` (nadeko is the name of the session we created before so, replace `nadeko` with the session name you created.)
- If you want to kill NadekoBot session, type `tmux kill-session -t nadeko`

**Screen**

- If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
- If you want to switch to/ see that screen, type `screen -r nadeko` (nadeko is the name of the screen we created before so, replace `nadeko` with the screen name you created.)
- If you want to kill the NadekoBot screen, type `screen -X -S nadeko quit`

[Homebrew]: http://brew.sh/
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
