#### If you have NadekoBot 1.x on Windows

- Follow the [Windows Guide](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/) and install the latest version of **NadekoBot**.
- Navigate to your **old** `Nadeko` folder and copy your `credentials.json` file and the `data` folder.
- Paste credentials into the **NadekoBot 1.4x+** `C:\Program Files\NadekoBot\system` folder.
- Paste your **old** `Nadeko` data folder into **NadekoBot 1.4x+** `C:\Program Files\NadekoBot\system` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** Nadeko as the guide describes, if it is not already running.


#### If you are running Dockerised Nadeko

- Shutdown your existing container **docker stop nadeko**.
- Move you credentials and other files to another folder.
- Delete your container **docker rm nadeko**.
- Create a new container **docker create --name=nadeko -v /nadeko/:/root/nadeko uirel/nadeko:1.4**.
- Start the container **docker start nadeko** wait for it to complain about lacking credentials.
- Stop the container **docker stop nadeko** open the nadeko folder and replace the credentials, database and other files with your copies.
- Restart the container **docker start nadeko**.

#### If you have NadekoBot 1.x on Linux or macOS

- Backup the `NadekoBot.db` from `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data`
- Backup the `credentials.json` from `NadekoBot/src/NadekoBot/`
- **For MacOS Users Only:** download and install the latest version of [.NET Core SDK](https://www.microsoft.com/net/core#macos)
- Next, use the command `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.4/linuxAIO.sh && bash linuxAIO.sh`
- **For Ubuntu, Debian and CentOS Users Only:** use the option `4. Auto-Install Prerequisites` to install the latest version of .NET Core SDK.
- Use option `1. Download NadekoBot` to update your NadekoBot to 1.4.x.
- Next, just [run your NadekoBot.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)
- *NOTE: 1.4.x uses `NadekoBot.db` file from `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.1/data` folder.*