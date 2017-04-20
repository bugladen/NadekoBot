## Setting up NadekoBot on Linux

#### Setting up NadekoBot on Linux Digital Ocean Droplet
If you want Nadeko to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try Nadeko on Linux Digital Ocean Droplet using the link [DigitalOcean](http://m.do.co/c/46b4d3d44795/) (and using this link will be supporting Nadeko and will give you **$10 credit**)

#### Setting up NadekoBot
Assuming you have followed the link above to setup an account and Droplet with 64bit OS in Digital Ocean and got the `IP address and root password (in email)` to login, its time to get started.

**Go through this whole guide before setting up Nadeko**

#### Prerequisites
- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [WinSCP](https://winscp.net/eng/download.php) *(optional)*

#### Starting up

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and hit enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then hit Enter.

*as you are running it for the first time, it will most likely to ask you to change your root password, for that, type the "password you received through email", hit Enter, enter a "new password", hit Enter and confirm that "new password" again.*
**SAVE that new password somewhere safe, not just in your mind**. After you've done that, you are ready to write commands.

**NOTE:** Copy the commands, and just paste them using **mouse single right-click.**

#### Creating and Inviting bot

- Read here how to [create a DiscordBot application](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#creating-discordbot-application)
- [Visual Invite Guide](http://discord.kongslien.net/guide.html) **(Note: Client ID is your Bot ID)**
- Copy your `Client ID` from your [applications page](https://discordapp.com/developers/applications/me).
- Replace the **12345678** in this link: 	
`https://discordapp.com/oauth2/authorize?client_id=`12345678`&scope=bot&permissions=66186303`		
 with your `Client ID`
- The link should now look like this: 	
`https://discordapp.com/oauth2/authorize?client_id=`**YOUR_CLENT_ID_HERE**`&scope=bot&permissions=66186303`
- Go to the newly created link and pick the server we created, and click `Authorize`
- The bot should have been added to your server.

#### Getting NadekoBot
##### Part I - Downloading the installer
Use the following command to get and run `linuxAIO.sh`		
(Remember **Do Not** rename the file **linuxAIO.sh**)

`cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/master/linuxAIO.sh && bash linuxAIO.sh`

You should see these following options after using the above command:

```
1. Download Dev Build (Latest)
2. Download Stable Build
3. Run Nadeko (Normally)
4. Run Nadeko with Auto Restart (Run Nadeko normally before using this.)
5. Auto-Install Prerequisites (for Ubuntu, Debian and CentOS)
6. Set up credentials.json (if you have downloaded the bot already)
7. To exit
```
##### Part II - Downloading Nadekobot prerequisites

**If** you are running NadekoBot for the first time on your system and never had any *prerequisites* installed and have Ubuntu, Debian or CentOS, Press `5` and `enter` key, then `y` when you see the following:
```
Welcome to NadekoBot Auto Prerequisites Installer.
Would you like to continue?
```
That will install all the prerequisites your system need to run NadekoBot.

(Optional) **If** you want to install it manually, you can try finding it [here](https://github.com/Kwoth/NadekoBot-BashScript/blob/master/nadekoautoinstaller.sh)

Once *prerequisites* finish installing,

##### Part III - Installing Nadeko
Choose either 
`1` to get the **most updated build of NadekoBot** 
or 
`2` to get the **previously stable build of NadekoBot**
and then press `enter` key.	

Once Installation is completed you should see the options again.

Next, check out:
##### Part IV - Setting up credentials

- [1. Setting up credentials.json](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#setting-up-credentialsjson)
- [2. To Get the Google API](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music)
- [3. JSON Explanations for other APIs](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)

You will need the following for the next step:
![botimg](https://cdn.discordapp.com/attachments/251504306010849280/276455844223123457/Capture.PNG)

- **Bot's Client ID** and **Bot's ID** (both are same) [(*required)](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- **Bot's Token** (not client secret) [(*required)](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- Your **Discord userID** [(*required)](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- **Google Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music)
- **LoL Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)
- **Mashape Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)
- **Osu Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)
- **Sound Cloud Client Id** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)

Once you have acquired them, press `6` to **Set up credentials.json**

You will be asked to enter the required informations, just follow the on-screen instructions and enter the required information.		
*i.e* If you are asked **Bot's Token**, then just copy and paste or type the **Bot's Token** and press `enter` key.

(If you want to skip any optional infos, just press `enter` key without typing/pasting anything.)		
Once done,		
##### Part V - Checking if Nadeko is working
You should see the options again.	
Next, press `3` to **Run Nadeko (Normally)**.
Check in your discord server if your new bot is working properly.	
##### Part VI - Running Nadeko on tmux
If your bot is working properly in your server, type `.die` to **shut down the bot**, then press `7` to **exit**.
Next, [Run your bot again with **tmux**.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)	

[Check this when you need to **restart** your **NadekoBot** anytime later along with tmux session.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-nadeko)

#### Running NadekoBot

**Create a new Session:**

- `tmux new -s nadeko`  
  
The above command will create a new session named **nadeko** *(you can replace “nadeko” with anything you prefer and remember its your session name)* so you can run the bot in background without having to keep the PuTTY running.

**Next, we need to run `linuxAIO.sh` in order to get the latest running scripts with patches:**

- `cd ~ && bash linuxAIO.sh`

**From the options,**

Choose `3` to **Run NadekoBot normally.**		
**NOTE:** With option `3` (Running Normally), if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) in discord. The bot will shut down and will stay offline until you manually run it again. (best if you want to check the bot.)

Choose `4` to **Run NadekoBot with Auto Restart.**	
It will show you more options: 
```
1. Run Auto Restart normally without Updating.
2. Auto Restart and Update with Dev Build (latest)
3. Auto Restart and Update with Stable Build
4. Exit
```
**NOTE:** With option `4` (Running with Auto Restart), bot will auto run if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) making the command `.die` to function as restart.	

See how that happens:

![img9](https://cdn.discordapp.com/attachments/251504306010849280/251506312893038592/die_explaination.gif)

**Remember** that, while running with Auto Restart, you will need to [close the tmux session](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-nadeko) to stop the bot completely.

**Now check your Discord, the bot should be online**

Next to **move the bot to background** and to do that, press **CTRL+B, release, D** (that will detach the nadeko session using TMUX) and you can finally close **PuTTY** if you want.

#### Restarting Nadeko

**Restarting NadekoBot:**

**If** you have chosen option `4` to **Run Nadeko with Auto Restart** from Nadeko's `linuxAIO.sh` *[(you got it from this step)](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-nadekobot)*	
You can simply type `.die` in the server you have your NadekoBot to make her restart.

**Restarting Nadeko with the Server:**

Open **PuTTY** and login as you have before, type `reboot` and hit Enter.

**Restarting Manually:**

- Kill your previous session, check with `tmux ls`
- `tmux kill-session -t nadeko` (don't forget to replace "nadeko" to what ever you named your bot's session)
- [Run the bot again.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)

#### Updating Nadeko

- Connect to the terminal through **PuTTY**.
- `tmux kill-session -t nadeko` (don't forget to replace **nadeko** in the command with the name of your bot's session)
- Make sure the bot is **not** running.
- `tmux new -s nadeko` (**nadeko** is the name of the session)
- `cd ~ && bash linuxAIO.sh`
- Choose either `1` or `2` to update the bot with **latest build** or **stable build** respectively.
- Choose either `3` or `4` to run the bot again with **normally** or **auto restart** respectively.
- Done. You can close **PuTTY** now.

#### Setting up Music

To set up Nadeko for music and Google API Keys, follow [Setting up NadekoBot for Music](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-nadekobot-for-music)

Once done, go back to **PuTTY**

#### Some more Info

##### Info about tmux

- If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.
- If you want to **switch to/ see that session**, type `tmux a -t nadeko` (**nadeko** is the name of the session we created before so, replace **“nadeko”** with the session name you created.)
- If you want to **kill** NadekoBot **session**, type `tmux kill-session -t nadeko`

#### Guide for Advance Users (Optional)

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

#### Setting up SFTP

- Open **WinSCP**
- Click on **New Site** (top-left corner).
- On the right-hand side, you should see **File Protocol** above a drop-down selection menu.
- Select **SFTP** *(SSH File Transfer Protocol)* if its not already selected.
- Now, in **Host name:** paste or type in your `Digital Ocean Droplets IP address` and leave `Port: 22` (no need to change it).
- In **Username:** type `root`
- In **Password:** type `the new root password (you changed at the start)`
- Click on **Login**, it should connect.
- It should show you the NadekoBot folder which was created by git earlier on the right-hand side window.
- Open that folder, then open the `src` folder, followed by another `NadekoBot` folder and you should see `credentials.json` there.

#### Setting up credentials.json

- Copy the `credentials.json` to desktop
- EDIT it as it is guided here: [Setting up credentials.json](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/#setting-up-credentialsjson-file)
- Paste/put it back in the folder once done. `(Using WinSCP)`
- **If** you already have Nadeko 1.0 setup and have `credentials.json` and `NadekoBot.db`, you can just copy and paste the `credentials.json` to `NadekoBot/src/NadekoBot` and `NadekoBot.db` to `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data` using WinSCP.
- **If** you have Nadeko 0.9x follow the [Upgrading Guide](http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/)


[img7]: https://cdn.discordapp.com/attachments/251504306010849280/251505766370902016/setting_up_credentials.gif
