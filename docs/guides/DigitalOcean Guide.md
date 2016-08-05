##Setting up NadekoBot on DigitalOcean Droplet

*If you want Nadeko to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try Nadeko on Linux Digital Ocean Droplet using the link [DigitalOcean][DigitalOcean] (and using this link will be supporting Nadeko and will give you **$10 credit**)

Keep this helpful video handy [Linux Setup Video][Linux Setup Video] (thanks to klincheR) it contains how to set up the Digital Ocean droplet aswell.*

Assuming you have followed the link above to created an account in Digital Ocean and video to set up the bot until you get the `IP address and root password (in email)` to login, its time to begin.

#### Prerequisites
- Download [PuTTY][PuTTY]
- Download [CyberDuck][CyberDuck]

####Setting up NadekoBot

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and hit enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then hit Enter.

*(as you are running it for the first time, it will most likely to ask you to change your root password, for that, type the "password you received through email", hit Enter, enter a "new password", hit Enter and confirm that "new password" again.)*
**SAVE that new password somewhere safe not just in mind**. After you done that, you are ready to write commands.

**Copy and just paste** using **mouse right-click** (it should paste automatically)

####FFMPEG for Windows

- To install `FFMPEG` on Windows download and install [FFMPEG][FFMPEG]

####FFMPEG for Linux

`apt-get install ffmpeg`

**Type** `y` **hit Enter**

NOTE: if its "not installing" then, follow the guide here: [FFMPEG Help Guide][FFMPEG Help Guide]

**All you need to do, if you are running UBUNTU 14.04 is initiate these:**

`sudo add-apt-repository ppa:mc3man/trusty-media`
`sudo apt-get update`
`sudo apt-get dist-upgrade`

*Before executing* `sudo apt-get install ffmpeg`

**If you are running Debian 8 Jessie, please, follow these steps:**

`wget http://luxcaeli.de/installer.sh && sudo bash installer.sh` (Thanks to Eleria<3)

In case you are not able to install it with installer ^up there, follow these steps:

`sudo apt-get update`
`echo "deb http://ftp.debian.org/debian jessie-backports main" | tee /etc/apt/sources.list.d/debian-backports.list`
`sudo apt-get update`
`sudo  apt-get install ffmpeg -y`

####Installing TMUX
*If on Linux*
`apt-get install tmux`

####Creating Nadeko folder
- Create a new folder “nadeko” or anything you prefer

`mkdir nadeko`

- Move to “nadeko” folder (note `cd --` to go back the directory)

`cd nadeko`

####Getting NadekoBot from Releases

Go to this link: [Releases][Releases] and **copy the zip file address** of the lalest version available,
it should look like `https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`

-If on Windows, just download and extract the content in your `Nadeko` folder.
-If on Linux, follow the guide bellow:
Get the correct link, type `wget`, then *paste the link*, then hit **Enter**.

`wget https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`

**^Do not copy-paste it**

Now we need to `unzip` the downloaded zip file and to do that, type the file name as it showed in your screen or just copy from the screen, should be like ` NadekoBot.vx.x.zip`

`unzip NadekoBot.vx.x.zip`

**^Do not copy-paste it**

####Setting up NadekoBot

- Open **CyberDuck**
- Click on **Open Connection** (top-left corner), a new window should appear.
- You should see **FTP (File Transfer Protocol)** in drop-down.
- Change it to **SFTP (SSH File Transfer Protocol)**
- Now, in **Server:** paste or type in your `Digital Ocean Droplets IP address`, leave `Port: 22` (no need to change it)
- In **Username:** type `root`
- In **Password:** type `the new root password (you changed at the start)`
- Click on **Connect**
- It should show you the new folder you created.
- Open it.

####Renaming Credentials.json

- Copy the `credentials_example.json` to desktop
- EDIT it as it is guided here: [Setting up Credentials.json](Windows Guide.md#setting-up-credentialsjson-file)
- Read here how to [Create DiscordBot application](https://github.com/miraai/NadekoBot/blob/dev/docs/guides/Windows%20Guide.md#creating-discordbot-application)
- Rename it to `credentials.json` and paste/put it back in the folder. `(Yes, using CyberDuck)`
- You should see two files `credentials_example.json` and `credentials.json`
- Also if you already have nadeko setup and have `credentials.json`, `config.json`, `nadekobot.sqlite`, and `"permissions" folder`, you can just copy and paste it to the Droplets folder using CyberDuck.

####Running NadekoBot

- Go back to **PuTTY**, `(hope its still running xD)`
- Type/ Copy and hit **Enter**.

*If you are on Linux run:*
`tmux new -s nadeko`

**^this will create a new session named “nadeko”** `(you can replace “nadeko” with anything you prefer and remember 
its your session name) so you can run the bot in background without having to keep running PuTTY in the background.`
- Enter your Nadeko folder 

`cd nadeko`

*If you are on Linux run:*

`mono NadekoBot.exe`

**CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING**

Now time to **move bot to background** and to do that, press **CTRL+B+D** (this will ditach the nadeko session using TMUX), and you can finally close PuTTY now.

Copy your CLIENT ID (that's in the same Developer page where you brought your token) and replace `12345678` in this link: `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with it. Go to that link and you will be able to add your bot to your server.
Or check this guide also [Inviting your bot to your server](Windows Guide.md#inviting-your-bot-to-your-server)

####How to restart Nadeko with the server (for science)

- Open **PuTTY** and login as you have before, type `reboot` and hit Enter.

[PuTTY]: http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html
[CyberDuck]: https://cyberduck.io
[Linux Setup Video]: https://www.youtube.com/watch?v=icV4_WPqPQk&feature=youtu.be
[Releases]: https://github.com/Kwoth/NadekoBot/releases
[FFMPEG Help Guide]: http://www.faqforge.com/linux/how-to-install-ffmpeg-on-ubuntu-14-04/
[DigitalOcean]: http://m.do.co/c/46b4d3d44795/
[FFMPEG]: https://github.com/Soundofdarkness/FFMPEG-Installer
