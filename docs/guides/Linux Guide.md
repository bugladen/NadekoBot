##Setting up NadekoBot on Linux

####Setting up NadekoBot on Linux Digital Ocean Droplet
If you want Nadeko to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try Nadeko on Linux Digital Ocean Droplet using the link [DigitalOcean](http://m.do.co/c/46b4d3d44795/) (and using this link will be supporting Nadeko and will give you **$10 credit**)

####Setting up NadekoBot
Assuming you have followed the link above to setup an account and Droplet with 64bit OS in Digital Ocean and got the `IP address and root password (in email)` to login, its time to get started.

**Go through this whole guide before setting up Nadeko**

#### Prerequisites
- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [CyberDuck](https://cyberduck.io) or [WinSCP](https://winscp.net/eng/download.php)

#### Follow these steps

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and hit enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then hit Enter.

*as you are running it for the first time, it will most likely to ask you to change your root password, for that, type the "password you received through email", hit Enter, enter a "new password", hit Enter and confirm that "new password" again.*
**SAVE that new password somewhere safe, not just in your mind**. After you've done that, you are ready to write commands.

**NOTE:** Copy the commands, and just paste them using **mouse single right-click.**

####Installing Git

![img1](https://cdn.discordapp.com/attachments/251504306010849280/251504416019054592/git.gif)

`sudo apt-get install git -y`

**NOTE:** If the command is not being initiated, hit **Enter**

####Installing .NET Core SDK

![img2](https://cdn.discordapp.com/attachments/251504306010849280/251504746987388938/dotnet.gif)

Go to [this link](https://www.microsoft.com/net/core#ubuntu) provided by microsoft for instructions on how to get the most up to date version of the dotnet core sdk!  
Make sure that you're on the correct page for your distribution of linux as the guides are different for the various distributions  

We'll go over the steps here for Ubuntu 16.04 anyway (these will **only** work on Ubuntu 16.04), accurate as of 25/11/2016

```
sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
sudo apt-get update && sudo apt-get install dotnet-dev-1.0.0-preview2.1-003177 -y
```

**NOTE:** .NET CORE SDK only supports 64-bit Linux Operating Systems (Raspberry Pis are not supported because of this)

####Installing Opus Voice Codec and libsodium

![img3](https://cdn.discordapp.com/attachments/251504306010849280/251505294654308353/libopus.gif)

`sudo apt-get install libopus0 opus-tools libopus-dev libsodium-dev -y`

####Installing FFMPEG

![img4](https://cdn.discordapp.com/attachments/251504306010849280/251505443111829505/ffmpeg.gif)

`apt-get install ffmpeg -y`

**NOTE:** If you are running **UBUNTU 14.04**, you must run these first:

```
sudo add-apt-repository ppa:mc3man/trusty-media
sudo apt-get update
sudo apt-get dist-upgrade
```

**Before executing:** `sudo apt-get install ffmpeg`


**NOTE:** If you are running **Debian 8 Jessie**, please, follow these steps:

`wget http://luxcaeli.de/installer.sh && sudo bash installer.sh` *Thanks to Eleria <3*

In case you are not able to install it with **installer**, follow these steps:

```
sudo apt-get update
echo "deb http://ftp.debian.org/debian jessie-backports main" | tee /etc/apt/sources.list.d/debian-backports.list
sudo apt-get update && sudo apt-get install ffmpeg -y
```

####Installing TMUX

![img5](https://cdn.discordapp.com/attachments/251504306010849280/251505519758409728/tmux.gif)

`sudo apt-get install tmux -y`

####Getting NadekoBot

Use the following command to get and run `linuxAIO.sh`:		
(Remember **DO NOT** rename the file `linuxAIO.sh`)

`cd ~ && wget https://github.com/Kwoth/NadekoBot-BashScript/raw/master/linuxAIO.sh && bash linuxAIO.sh`

Follow the on screen instructions:

1. To Get the latest build. (most recent updates)
2. To Get the stable build.

Choose either `1` or `2` then press `enter` key.	
Once Installation is completed you should see the options again.	
Next, choose `5` to exit. 

####Creating and Inviting bot

- Read here how to [create a DiscordBot application](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#creating-discordbot-application)
- [Visual Invite Guide](http://discord.kongslien.net/guide.html) *NOTE: Client ID is your Bot ID*
- Copy your `Client ID` from your [applications page](https://discordapp.com/developers/applications/me).
- Replace the `12345678` in this link `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with your `Client ID`.
- The link should now look like this: `https://discordapp.com/oauth2/authorize?client_id=**YOUR_CLENT_ID_HERE**&scope=bot&permissions=66186303`.
- Go to the newly created link and pick the server we created, and click `Authorize`.
- The bot should have been added to your server.

####Guide for Advance Users

**Skip this step if you are a Regular User or New to Linux.**

[![img7][img7]](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-nadekobot)

- Right after [Getting NadekoBot](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-nadekobot)
- `cd NadekoBot/src/NadekoBot/` (go to this folder)
- `pico credentials.json` (open credentials.json to edit)
- Insert your bot **Client ID, Bot ID** (should be same as your Client ID) **and Token** if you got it following [Creating and Inviting bot](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#creating-and-inviting-bot).
- Insert your own ID in Owners ID follow: [Setting up credentials.json](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- And Google API from [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music)
- Once done, press `CTRL+X`
- It will ask for "Save Modified Buffer?", press `Y` for yes
- It will then ask "File Name to Write" (rename), just hit `Enter` and Done.
- You can now move to [Running NadekoBot](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)

####Setting up SFTP

- Open **CyberDuck**
- Click on **Open Connection** (top-left corner), a new window should appear.
- You should see **FTP (File Transfer Protocol)** in drop-down.
- Change it to **SFTP (SSH File Transfer Protocol)**
- Now, in **Server:** paste or type in your `Digital Ocean Droplets IP address`, leave `Port: 22` (no need to change it)
- In **Username:** type `root`
- In **Password:** type `the new root password (you changed at the start)`
- Click on **Connect**
- It should show you the NadekoBot folder which was created by git earlier
- Open that folder, then open the `src` folder, followed by another `NadekoBot` folder and you should see `credentials.json` there.

####Setting up credentials.json

- Copy the `credentials.json` to desktop
- EDIT it as it is guided here: [Setting up credentials.json](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- Paste/put it back in the folder once done. `(Using CyberDuck/WinSCP)`
- **If** you already have Nadeko 1.0 setup and have `credentials.json` and `NadekoBot.db`, you can just copy and paste the `credentials.json` to `NadekoBot/src/NadekoBot` and `NadekoBot.db` to `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data` using CyberDuck.
- **If** you have Nadeko 0.9x follow the [Upgrading Guide](http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/)

####Setting up Music

To set up Nadeko for music and Google API Keys, follow [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music)

Once done, go back to **PuTTY**

####Running NadekoBot

**Create a new Session:**

- `tmux new -s nadeko`  
  
The above command will create a new session named **nadeko** *(you can replace “nadeko” with anything you prefer and remember its your session name)* so you can run the bot in background without having to keep the PuTTY running.

**Next, we need to run `linuxAIO.sh` in order to get the latest running scripts with patches:**

- `cd ~ && bash linuxAIO.sh`

From the options,

Choose `3` To Run the bot normally.		
**NOTE:** With option `3` (Running Normally), if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) in discord. The bot will shut down and will stay offline until you manually run it again. (best if you want to check the bot.)

Choose `4` To Run the bot with Auto Restart.	
**NOTE:** With option `4` (Running with Auto Restart), bot will auto run if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) making the command `.die` to function as restart.	

See how that happens:

![img9](https://cdn.discordapp.com/attachments/251504306010849280/251506312893038592/die_explaination.gif)

**Remember** that, while running with Auto Restart, you will need to [close the tmux session](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-nadeko) to stop the bot completely.

**Now check your Discord, the bot should be online**

Next to **move the bot to background** and to do that, press **CTRL+B+D** (this will detach the nadeko session using TMUX), and you can finally close PuTTY now.

####Some more Info (just in case)

**Info about tmux:**

- If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.
- If you want to **switch to/ see that session**, type `tmux a -t nadeko` (**nadeko** is the name of the session we created before so, replace **“nadeko”** with the session name you created.)
- If you want to **kill** NadekoBot **session**, type `tmux kill-session -t nadeko`

**If you are running Ubuntu 16.10, and having trouble installing .NET Core:**

- Go to [Download Page for libicu55_55.1-7_amd64.deb](http://packages.ubuntu.com/en/xenial/amd64/libicu55/download)
- Copy the link with a download option closest to you
- `wget <copied link>` *e.g.* `wget http://mirrors.kernel.org/ubuntu/pool/main/i/icu/libicu55_55.1-7_amd64.deb` (make sure it is downloaded)
- Install with: `dpkg –i libicu55_55.1-7_amd64.deb`
- Now go back and install the .NET Core

####Restarting Nadeko

**Restarting Nadeko with the Server:**

Open **PuTTY** and login as you have before, type `reboot` and hit Enter.

**Restarting Manually:**

- Kill your previous session, check with `tmux ls`
- `tmux kill-session -t nadeko` (don't forget to replace "nadeko" to what ever you named your bot's session)
- [Run the bot again.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)

####Updating Nadeko

- Connect to the terminal through PuTTY.
- `tmux kill-session -t nadeko` (don't forget to replace **nadeko** in the command with the name of your bot's session)
- Make sure the bot is **not** running.
- `tmux new -s nadeko` (**nadeko** is the name of the session)
- `cd ~ && bash linuxAIO.sh`
- Choose either `1` or `2` to update the bot with **latest build** or **stable build** respectively.
- Choose either `3` or `4` to run the bot again with **normally** or **auto restart** respectively.
- Done. You can close PuTTY now.

####Alternative way to Install

If the [Nadeko installer](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-nadekobot) shows any kind error, check if you have the `linuxAIO.sh` file and make sure its not renamed or if you want to manually install the bot. Use the following command(s):


![img6](https://cdn.discordapp.com/attachments/251504306010849280/251505587089571850/getting_nadeko.gif)

`cd ~ && curl -L https://github.com/Kwoth/NadekoBot-BashScript/raw/master/nadeko_installer.sh | sh`

**OR**

```
cd ~ && git clone -b 1.0 --recursive --depth 1 https://github.com/Kwoth/NadekoBot.git
cd ~/NadekoBot/discord.net/src/Discord.Net && dotnet restore && cd ../Discord.Net.Commands && dotnet restore && cd ../../../src/NadekoBot/ && dotnet restore && dotnet build --configuration Release
```
  
If you are getting error using the above steps try:

```
cd ~/NadekoBot/discord.net && dotnet restore -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json && dotnet restore
cd ~/NadekoBot/src/NadekoBot/ && dotnet restore && dotnet build --configuration Release
```
[img7]: https://cdn.discordapp.com/attachments/251504306010849280/251505766370902016/setting_up_credentials.gif
