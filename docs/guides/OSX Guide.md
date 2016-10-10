### Setting Up NadekoBot on OSX
#### Prerequisites 
- 1) [Homebrew][Homebrew]
- 2) Mono
- 3) Google Account
- 4) Soundcloud Account (if you want soundcloud support)
- 5) Text Editor (TextWrangler, or equivalent) or outside editor such as [Atom][Atom]

####Installing Homebrew

`/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`

Run `brew update` to fetch the latest package data.
####Installing dependencies
```
brew install git
brew install ffmpeg
brew update && brew upgrade ffmpeg
brew install opus
brew install opus-tools
brew install opusfile
brew install libffi
brew install libsodium
brew install tmux
```

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

####Installing Mono
- Building Mono dependencies:

`brew install autoconf automake libtool pkg-config`

- Building Mono from Source:
 
To build Mono from a Git Source Code checkout, you will want to have the official Mono installed on the system, as the build requires a working C# compiler to run. Once you do this, run the following commands, remember to replace PREFIX with your installation prefix that you selected:

```
PATH=$PREFIX/bin:$PATH
git clone https://github.com/mono/mono.git
cd mono
CC='cc -m32' ./autogen.sh --prefix=$PREFIX --disable-nls --build=i386-apple-darwin11.2.0
make
make install
```
To build Mono in 64 bit mode instead use this to configure the build:

`./autogen.sh --prefix=$PREFIX --disable-nls`

####Nadeko Setup
- Create a new folder and name it `Nadeko`.
- Move to our `Nadeko` folder
 
`cd Nadeko`
- Go to [Releases][Releases] and copy the zip file address of the lalest version available, it should look like `https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`
- Get the correct link, type `curl -O` and past the link, then hit `Enter`
- It should be something like this:
 
`curl -O https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`

^ do not copy-paste it

- Unzip the downloaded file in our `Nadeko` folder

####Creating DiscordBot application
- Go to [DiscordApp][DiscordApp].
- Log in with your Discord account.
- On the left side, press `New Application`.
- Fill out the `App Name` (your bot's name, in this case), put the image you want, and add an app description(optional).
- Create the application.
- Once the application is created, click on `Create a Bot User` and confirm it.
- Keep this window open for now.
 
####Setting up Credentials.json file
- In our `NadekoBot` folder you should have `.json` file named `credentials_example.json`. (Note: If you do not see a **.json** after `credentials_example.json `, do not add the `**.json**`. You most likely have `"Hide file extensions"` enabled.)
- Rename `credentials_example.json` to `credentials.json`.
- Open the file with your Text editor.
- In there you will see fields like `Token`, `ClientId`, `BotId`, `OwnerIDs`.
- In your [DiscordApp][DiscordApp], under `Bot User` part, you will see the `Token:click to reveal` part, click to reveal it.
- Copy your bot's token, and put it between `" "` in your `credentials.json` file.
- Copy `Client ID` and replace it with the example one in your `credentials.json` in `Client ID` **and** `BotID` field.
- Save your `credentials.json` but keep it open. We need to put your `User ID` and owner.
 
####Running NadekoBot
- Copy/past and hit `Enter`
 
`tmux new -s nadeko`

^this will create a new session named “nadeko” `(you can replace “nadeko” with anything you prefer and remember its your
session name)`.

or if you want to use Screen, run:

`screen -S nadeko`

^this will create a new screen named “nadeko” `(you can replace “nadeko” with anything you prefer and remember its your
screen name)`.

`cd nadeko`

- Start NadekoBot.exe using Mono:
 
`mono NadekoBot.exe`

CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING

Now time to move bot to background and to do that, press CTRL+B+D (this will ditach the nadeko session using TMUX)

*if you used Screen press CTRL+A+D (this will detach the nadeko screen)*

####Inviting your bot to your server - [Invite Guide][Invite Guide]
- Create a new server in Discord.
- Copy your `Client ID` from your [DiscordApp][DiscordApp].
- Replace `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- Link should look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID**&scope=bot&permissions=66186303`.
- Go to newly created link and pick the server we created, and click `Authorize`.
- Bot should be added to your server.
 
####Setting up OwnerIds
- In the server where your bot is, in a text channel, type `.uid`
- Your `User ID` should show, copy it.
- Close `NadekoBot.exe`
- Replace your `User ID` in the `credentials.json` between `[ ]` and save the changes.
- Run `NadekoBot.exe` again.
- Now you are the bot owner.
- You can add `User IDs` from the other users by separating IDs with a comma if you want to have more owners.

####Setting NadekoBot Music

For Music Setup and API keys check [Setting up NadekoBot for Music](Windows Guide.md#setting-up-nadekobot-for-music) and [JSON Explanations](JSON Explanations.md).

####Some more Info - TMUX

-If you want to see the sessions after logging back again, type `tmux ls`, and that will give you the list of sessions running. 
-If you want to switch to/ see that session, type `tmux a -t nadeko` (nadeko is the name of the session we created before so, replace `“nadeko”` with the session name you created.)
-If you want to kill NadekoBot session, type `tmux kill-session -t nadeko`

####Some more Info - Screen

-If you want to see the sessions after logging back again, type `screen -ls`, and that will give you the list of screens. 
-If you want to switch to/ see that screen, type `screen -r nadeko` (nadeko is the name of the screen we created before so, replace `“nadeko”` with the screen name you created.)
-If you want to kill the NadekoBot screen, type `screen -X -S nadeko quit`

[Homebrew]: http://brew.sh/
[Mono]: http://www.mono-project.com/docs/compiling-mono/mac/
[Releases]: https://github.com/Kwoth/NadekoBot/releases
[DiscordApp]: https://discordapp.com/developers/applications/me
[Atom]: https://atom.io/
[Invite Guide]: http://discord.kongslien.net/guide.html
[Google Console]: https://console.developers.google.com
[Soundcloud]: https://soundcloud.com/you/apps/new
