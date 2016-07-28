#Setting up NadekoBot on Linux

####Setting up NadekoBot on Linux Digital Ocean Droplet
######If you want Nadeko to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try Nadeko on Linux Digital Ocean Droplet using the link [DigitalOcean][DigitalOcean] (and using this link will be supporting Nadeko and will give you **$10 credit**)

######Keep this helpful video handy [Linux Setup Video][Linux Setup Video] (thanks to klincheR) it contains how to set up the Digital Ocean droplet aswell.

####Setting up NadekoBot
Assuming you have followed the link above to created an account in Digital Ocean and video to set up the bot until you get the `IP address and root password (in email)` to login, its time to begin.

#### Prerequisites
- Download [PuTTY][PuTTY]
- Download [CyberDuck][CyberDuck]

#### Follow these steps

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and hit enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then hit Enter.

*(as you are running it for the first time, it will most likely to ask you to change your root password, for that, type the "password you received through email", hit Enter, enter a "new password", hit Enter and confirm that "new password" again.*
**SAVE that new password somewhere safe not just in mind**. After you done that, you are ready to write commands.

**Copy and just paste** using **mouse right-click** (it should paste automatically)

######MONO (Source: [Mono Source][Mono Source])

**1)**

`sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF`
`echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list`
`sudo apt-get update`

Note if the command is not being initiated, hit **Enter**

**2)**

`echo "deb http://download.mono-project.com/repo/debian wheezy-apache24-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list`

**2.5)**
*ONLY DEBIAN 8 and later*

`echo "deb http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list`

**2.6)**
*ONLY CentOS 7, Fedora 19 (and later)*

`yum install yum-util`
`rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"`
`yum-config-manager --add-repo http://download.mono-project.com/repo/centos/`

**3)**
*Mono Devel*

`apt-get install mono-devel`

**Type** `y` **hit Enter**


**4)**
Opus Voice Codec

`sudo apt-get install libopus0 opus-tools`

**Type** `y` **hit Enter**

**5)**
`sudo apt-get install libopus-dev`

**In case you are having issues with Mono where you get a random string and the bot won't run, do this:**

`sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF`
`echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list`
`apt-get install ca-certificates-mono`
`mozroots --import --sync`


####FFMPEG

**6)**
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

####Uncomplicated Firewall UFW

**7)**
`apt-get install ufw`

**it is most likely to have it already installed so if you see it is already installed, check with following command, and/or enable it**

**8)**
`ufw status`

**9)**
`ufw enable`

**Type** `y` **hit Enter**

**10)**
`sudo ufw allow ssh`



**11)**
Unzip

`apt-get install unzip`

**12)**
TMUX

`apt-get install tmux`

**Type** `y` **hit Enter**

####NOW WE NEED TO IMPORT SOME DISCORD CERTS
**13)**
`certmgr -ssl https://discordapp.com`

**14)**
`certmgr --ssl https://gateway.discord.gg`

Type `yes` and hit Enter **(three times - as it will ask for three times)**


**15)**
Create a new folder “nadeko” or anything you prefer

`mkdir nadeko`

**16)**
Move to “nadeko” folder (note `cd --` to go back the directory)

`cd nadeko`

**NOW WE NEED TO GET NADEKO FROM RELEASES**

Go to this link: [Releases][Releases] and **copy the zip file address** of the lalest version available,
it should look like `https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`

**17)**
Get the correct link, type `wget`, then *paste the link*, then hit **Enter**.

`wget https://github.com/Kwoth/NadekoBot/releases/download/vx.xx/NadekoBot.vx.x.zip`

**^Do not copy-paste it**

**18)**

Now we need to `unzip` the downloaded zip file and to do that, type the file name as it showed in your screen or just copy from the screen, should be like ` NadekoBot.vx.x.zip`

`unzip NadekoBot.vx.x.zip`

**^Do not copy-paste it**

#####NOW TO SETUP NADEKO

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

#####MAKE SURE YOU READ THE README BEFORE PROCEEDING

- Copy the `credentials_example.json` to desktop
- EDIT it as it is guided here: [Readme][Readme]
- Rename it to `credentials.json` and paste/put it back in the folder. `(Yes, using CyberDuck)`
- You should see two files `credentials_example.json` and `credentials.json`
- Also if you already have nadeko setup and have `credentials.json`, `config.json`, `nadekobot.sqlite`, and `"permissions" folder`, you can just copy and paste it to the Droplets folder using CyberDuck.

######TIME TO RUN

Go back to **PuTTY**, `(hope its still running xD)`

**19)**
Type/ Copy and hit **Enter**.

`tmux new -s nadeko`

**^this will create a new session named “nadeko”** `(you can replace “nadeko” with anything you prefer and remember its your session name) so you can run the bot in background without having to keep running PuTTY in the background.`

`cd nadeko`

**20)**
`mono NadekoBot.exe`

**CHECK THE BOT IN DISCORD, IF EVERYTHING IS WORKING**

Now time to **move bot to background** and to do that, press **CTRL+B+D** (this will ditach the nadeko session using TMUX), and you can finally close PuTTY now.

Copy your CLIENT ID (that's in the same Developer page where you brought your token) and replace `12345678` in this link: `https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303` with it. Go to that link and you will be able to add your bot to your server.

**NOW YOU HAVE YOUR OWN NADEKO BOT** `Thanks to Kwoth <3`

######SOME MORE INFO (JUST TO KNOW):

-If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.
-If you want to **switch to/ see that session**, type `tmux a -t nadeko` (**nadeko** is the name of the session we created before so, replace **“nadeko”** with the session name you created.)

**21)**
-If you want to **kill** NadekoBot **session**, type `tmux kill-session -t nadeko`

######TO RESTART YOUR BOT ALONG WITH THE WHOLE SERVER (for science):
**22)**
Open **PuTTY** and login as you have before, type `reboot` and hit Enter.

######IF YOU WANT TO UPDATE YOUR BOT

**FOLLOW THESE STEPS SERIALLY**

- **-21 OR 22**
- **-19**
- **-16**
- **-17**
- **-18**
- **-20**

HIT **CTRL+B+D** and close **PuTTY**

`IF YOU FACE ANY TROUBLE ANYWHERE IN THE GUIDE JUST FIND US IN NADEKO'S DISCORD SERVER`

[PuTTY]: http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html
[CyberDuck]: https://cyberduck.io
[Linux Setup Video]: https://www.youtube.com/watch?v=icV4_WPqPQk&feature=youtu.be
[Releases]: https://github.com/Kwoth/NadekoBot/releases
[Readme]: https://github.com/Kwoth/NadekoBot/blob/master/README.md
[FFMPEG Help Guide]: http://www.faqforge.com/linux/how-to-install-ffmpeg-on-ubuntu-14-04/
[Mono Source]: http://www.mono-project.com/docs/getting-started/install/linux/
[DigitalOcean]: http://m.do.co/c/46b4d3d44795/
