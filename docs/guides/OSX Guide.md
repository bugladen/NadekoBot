## Setting Up NadekoBot on OSX

#### Prerequisites 
- [Homebrew][Homebrew]
- Google Account
- Soundcloud Account (if you want soundcloud support)
- Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

#### Installing Homebrew

```/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"```

Run `brew update` to fetch the latest package data.  

#### Installing dependencies
```
brew install wget
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

#### Installing .NET Core SDK

- `ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/`
- `ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/`
- Download the [.NET Core SDK][.NET Core SDK]
- Open the `.pkg` file you downloaded and install it.
- `ln -s /usr/local/share/dotnet/dotnet /usr/local/bin`

#### Check your `FFMPEG`

**In case your `FFMPEG` wasnt installed properly (Optional)**

- `brew options ffmpeg`
- `brew install ffmpeg --with-x --with-y --with-z` etc.
- `brew update && brew upgrade` (Update formulae and Homebrew itself && Install newer versions of outdated packages)
- `brew prune` (Remove dead symlinks from Homebrew’s prefix)
- `brew doctor` (Check your Homebrew installation for common issues)
- Then try `brew install ffmpeg` again.

#### Installing xcode-select

Xcode command line tools. You will do this in Terminal.app by running the following command line:

`xcode-select --install`

A dialog box will open asking if you want to install `xcode-select`. Select install and finish the installation.

#### Downloading and building Nadeko

Use the following command to get and run `linuxAIO.sh`:		
(Remember **DO NOT** rename the file `linuxAIO.sh`)

`cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/master/linuxAIO.sh && bash linuxAIO.sh`

Follow the on screen instructions:

1. To Get the latest build. (most recent updates)
2. To Get the stable build.

Choose either `1` or `2` then press `enter` key.	
Once Installation is completed you should see the options again.	
Next, choose `5` to exit. 

#### Creating and Inviting bot

- Read here how to [create a DiscordBot application](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#creating-discordbot-application)
- [Visual Invite Guide](http://discord.kongslien.net/guide.html) *NOTE: Client ID is your Bot ID*
- Copy your `Client ID` from your [applications page](https://discordapp.com/developers/applications/me).
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID_HERE**&scope=bot&permissions=66186303`.
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.
 
#### Setting up Credentials.json file
- Open up the `NadekoBot` folder, which should be in your home directory, then `NadekoBot` folder then `src` folder and then the additonal `NadekoBot` folder.
- EDIT it as it is guided here: [Setting up credentials.json](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- **If** you already have Nadeko 1.0 setup and have `credentials.json` and `NadekoBot.db`, you can just copy and paste the `credentials.json` to `NadekoBot/src/NadekoBot` and `NadekoBot.db` to `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data`.
- **If** you have Nadeko 0.9x follow the [Upgrading Guide](http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/)

#### Setting NadekoBot Music

For Music Setup and API keys check [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music) and [JSON Explanations](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/).

#### Running NadekoBot

- Using tmux

`tmux new -s nadeko`

^this will create a new session named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your session name)`.

- Using Screen

`screen -S nadeko`

^this will create a new screen named “nadeko”  
`(you can replace “nadeko” with anything you prefer and remember its your screen name)`.

- Start Nadeko using .NET Core:

`cd ~ && bash linuxAIO.sh`

From the options,

Choose `3` To Run the bot normally.		
**NOTE:** With option `3` (Running Normally), if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) in discord. The bot will shut down and will stay offline untill you manually run it again. (best if you want to check the bot.)

Choose `4` To Run the bot with Auto Restart.	
**NOTE:** With option `4` (Running with Auto Restart), bot will auto run if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) making the command `.die` to be used as restart.	
**NOTE:** [To stop the bot you will have to kill the session.](http://nadekobot.readthedocs.io/en/latest/guides/OSX%20Guide/#some-more-info)

**Now check your Discord, the bot should be online**

Now time to move bot to background and to do that, press CTRL+B+D (this will detach the nadeko session using TMUX)	
If you used Screen press CTRL+A+D (this will detach the nadeko screen) 

#### Updating Nadeko

- Connect to the terminal.
- `tmux kill-session -t nadeko` [(don't forget to replace **nadeko** in the command to what ever you named your bot's session)](http://nadekobot.readthedocs.io/en/latest/guides/OSX%20Guide/#some-more-info)
- Make sure the bot is **not** running.
- `tmux new -s nadeko` (**nadeko** is the name of the session)
- `cd ~ && bash linuxAIO.sh`
- Choose either `1` or `2` to update the bot with **latest build** or **stable build** respectively.
- Choose either `3` or `4` to run the bot again with **normally** or **auto restart** respectively.
- Done. You can close terminal now.

#### Some more Info

**TMUX**

- If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
- If you want to switch to/ see that session, type `tmux a -t nadeko` (nadeko is the name of the session we created before so, replace `nadeko` with the session name you created.)
- If you want to kill NadekoBot session, type `tmux kill-session -t nadeko`

**Screen**

- If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
- If you want to switch to/ see that screen, type `screen -r nadeko` (nadeko is the name of the screen we created before so, replace `nadeko` with the screen name you created.)
- If you want to kill the NadekoBot screen, type `screen -X -S nadeko quit`

#### Alternative Method to Install Nadeko

**METHOD I**

- `cd ~ && curl -L https://github.com/Kwoth/NadekoBot-BashScript/raw/master/nadeko_installer.sh | sh`

**METHOD II**

- `cd ~`
- `git clone -b 1.0 --recursive https://github.com/Kwoth/NadekoBot.git`
- `cd ~/NadekoBot/discord.net`
- `dotnet restore -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json`
- `dotnet restore`
- `cd ~/NadekoBot/src/NadekoBot/`
- `dotnet restore` 
- `dotnet build --configuration Release`

[Homebrew]: http://brew.sh/
[.NET Core SDK]: https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.1-preview2.1-download.md
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
