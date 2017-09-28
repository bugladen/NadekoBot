## Setting up NadekoBot on Linux

**Setting up NadekoBot on Linux Digital Ocean Droplet**			
If you want Nadeko to play music for you 24/7 without having to hosting it on your PC and want to keep it cheap, reliable and convenient as possible, you can try Nadeko on Linux Digital Ocean Droplet using the link [DigitalOcean](http://m.do.co/c/46b4d3d44795/) (and using this link will be supporting Nadeko and will give you **$10 credit**)

**Setting up NadekoBot**			
Assuming you have followed the link above to setup an account and Droplet with 64bit OS in Digital Ocean and got the `IP address and root password (in email)` to login, its time to get started.

**Go through this whole guide before setting up Nadeko**

#### Prerequisites
- Download [PuTTY](http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- Download [WinSCP](https://winscp.net/eng/download.php) *(optional)*
- Create and Invite the bot.
	- Read here how to [create a Discord Bot application and invite it.](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application)

#### Starting up

- **Open PuTTY.exe** that you downloaded before, and paste or enter your `IP address` and then click **Open**.
If you entered your Droplets IP address correctly, it should show **login as:** in a newly opened window.
- Now for **login as:**, type `root` and press enter.
- It should then, ask for password, type the `root password` you have received in your **email address registered with Digital Ocean**, then press Enter.

If you are running your VPS/ droplet for the first time, it will most likely ask you to change your VPS root password, to do that, type the **password you received through email** it won't show any changes on the screen like `******` when password is being typed, press Enter once done. 			
Type a **new password**, press Enter and type the **new password** again and you're done.			
**Write down and save the new password somewhere safe.**				
After you've done that, you are ready to use your VPS.


#### Getting NadekoBot
##### Part I - Downloading the installer
Use the following command to get and run `linuxAIO.sh`		
(Remember **Do Not** rename the file **linuxAIO.sh**)

`cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

You should see these following options after using the above command:

```
1. Download NadekoBot
2. Run Nadeko (Normally)
3. Run Nadeko with Auto Restart (Run Nadeko normally before using this.)
4. Auto-Install Prerequisites (for Ubuntu, Debian and CentOS)
5. Set up credentials.json (if you have downloaded the bot already)
6. To exit
```
##### Part II - Downloading Nadekobot prerequisites

**If** you are running NadekoBot for the first time on your system and never had any *prerequisites* installed and have Ubuntu, Debian or CentOS, Press `4` and `enter` key, then `y` when you see the following:
```
Welcome to NadekoBot Auto Prerequisites Installer.
Would you like to continue?
```
That will install all the prerequisites your system need to run NadekoBot.			
(Optional) **If** you want to install it manually, you can try finding it [here.](https://github.com/Kwoth/NadekoBot-BashScript/blob/1.9/nadekoautoinstaller.sh)

Once *prerequisites* finish installing,

##### Part III - Installing Nadeko
Choose `1` to get the **most updated build of NadekoBot** 

and then press `enter` key.	

When installation is complete, you will see the options again.

Next, check out:
##### Part IV - Setting up credentials

- [1. Set up credentials.json](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- [2. Get the Google API](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)

You will need the following for the next step:
![botimg](https://cdn.discordapp.com/attachments/251504306010849280/276455844223123457/Capture.PNG)

- **Bot's Client ID** and **Bot's ID** (both are same) [(*required)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- **Bot's Token** (not client secret) [(*required)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- Your **Discord userID** [(*required)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file)
- **Google Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **LoL Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **Mashape Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)
- **Osu Api Key** [(optional)](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys)

Once you have acquired them, press `5` to **Set up credentials.json**

You will be asked to enter the required informations, just follow the on-screen instructions and enter the required information.		
*i.e* If you are asked **Bot's Token**, then just copy and paste or type the **Bot's Token** and press `enter` key.

(If you want to skip any optional infos, just press `enter` key without typing/pasting anything.)		
Once done,		
##### Part V - Checking if Nadeko is working
You should see the options again.	
Next, press `2` to **Run Nadeko (Normally)**.
Check in your discord server if your new bot is working properly.	
##### Part VI - Running Nadeko on tmux
If your bot is working properly in your server, type `.die` to **shut down the bot**, then press `6` on the console to **exit**.
Next, [Run your bot again with **tmux**.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)	

[Check this when you need to **restart** your **NadekoBot** anytime later along with tmux session.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-nadeko)

#### Running NadekoBot

**Create a new Session:**

- `tmux new -s nadeko`  
  
The above command will create a new session named **nadeko** *(you can replace “nadeko” with anything you prefer and remember its your session name)* so you can run the bot in background without having to keep the PuTTY running.

**Next, we need to run `linuxAIO.sh` in order to get the latest running scripts with patches:**

- `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`

**From the options,**

Choose `2` to **Run NadekoBot normally.**		
**NOTE:** With option `2` (Running normally), if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) in discord. The bot will shut down and will stay offline until you manually run it again. (best if you want to check the bot.)

Choose `3` to **Run NadekoBot with Auto Restart.**	
**NOTE:** With option `3` (Running with Auto Restart), bot will auto run if you use `.die` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#administration) making the command `.die` to function as restart.	

It will show you the following options: 
```
1. Run Auto Restart normally without Updating.
2. Run Auto Restart and update NadekoBot.
3. Exit
```

- With option `1. Run Auto Restart normally without Updating.` Bot will restart on `die` command and will not be downloading the latest build available.
- With option `2. Run Auto Restart and update NadekoBot.` Bot will restart and download the latest build of bot available everytime `die` command is used.

**Remember** that, while running with Auto Restart, you will need to [close the tmux session](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#restarting-nadeko) to stop the bot completely.

**Now check your Discord, the bot should be online**

Next to **move the bot to background** and to do that, press **CTRL+B, release, D** (that will detach the nadeko session using TMUX) and you can finally close **PuTTY**.

#### Restarting Nadeko

**Restarting NadekoBot:**

**If** you have chosen option `2` to **Run Nadeko with Auto Restart** from Nadeko's `linuxAIO.sh` *[(you got it from this step)](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#getting-nadekobot)*	
You can simply type `.die` in the server you have your NadekoBot to make her restart.

**Restarting Nadeko with the Server:**

Open **PuTTY** and login as you have before, type `reboot` and press Enter.

**Restarting Manually:**

- Kill your previous session, check with `tmux ls`
- `tmux kill-session -t nadeko` (don't forget to replace "nadeko" to what ever you named your bot's session)
- [Run the bot again.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)

#### Updating Nadeko

- Connect to the terminal through **PuTTY**.
- `tmux kill-session -t nadeko` (don't forget to replace **nadeko** in the command with the name of your bot's session)
- Make sure the bot is **not** running.
- `tmux new -s nadeko` (**nadeko** is the name of the session)
- `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
- Choose `1` to update the bot with **latest build** available.
- Next, choose either `2` or `3` to run the bot again with **normally** or **auto restart** respectively.
- Done.

#### Additional Information

##### Setting up Music

To set up Nadeko for music and Google API Keys, follow [Setting up your API keys.][setup music]

##### tmux

- If you want to **see the sessions** after logging back again, type `tmux ls`, and that will give you the list of sessions running.
- If you want to **switch to/ see that session**, type `tmux a -t nadeko` (**nadeko** is the name of the session we created before so, replace **“nadeko”** with the session name you created.)
- If you want to **kill** NadekoBot **session**, type `tmux kill-session -t nadeko`

##### Setting up SFTP

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

##### Setting up credentials.json

- Copy the `credentials.json` to desktop
- EDIT it as it is guided here: [Setting up credentials.json][setup credentials]
- Paste/put it back in the folder once done. `(Using WinSCP)`
- **If** you already have Nadeko 1.3.x setup and have `credentials.json` and `NadekoBot.db`, you can just copy and paste the `credentials.json` to `NadekoBot/src/NadekoBot` and `NadekoBot.db` to `NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data` using WinSCP.			
**Or** follow the [Upgrading Guide.][upgrading]


[img7]: https://cdn.discordapp.com/attachments/251504306010849280/251505766370902016/setting_up_credentials.gif
[setup credentials]: http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file
[setup music]: http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-api-keys
[upgrading]: http://nadekobot.readthedocs.io/en/latest/guides/Upgrading%20Guide/
