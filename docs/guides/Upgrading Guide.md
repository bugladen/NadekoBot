# This section is for users who are upgrading from versions older than 1.4 to 1.4+

#### If you have NadekoBot 1.x on Windows

- Go to `NadekoBot\src\NadekoBot` and backup your `credentials.json` file; then go to `NadekoBot\src\NadekoBot\bin\Release\netcoreapp1.0` and backup your `data` folder.
- Follow the [Windows Guide](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/) and install the latest version of **NadekoBot**.
- Paste your `credentials.json` file into the `C:\Program Files\NadekoBot\system` folder.
- Paste your `data` folder into `C:\Program Files\NadekoBot\system` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** Nadeko as the guide describes, if it is not already running.


#### If you are running Dockerised Nadeko

- There is an updating section in the docker guide.

#### If you have NadekoBot 1.x on Linux or macOS

- Backup the `NadekoBot.db` from `NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data`
- Backup the `credentials.json` from `NadekoBot/src/NadekoBot/`
- **For MacOS Users Only:** download and install the latest version of [.NET Core SDK](https://www.microsoft.com/net/core#macos)
- Next, use the command `cd ~ && wget -N https://github.com/Kwoth/NadekoBot-BashScript/raw/1.9/linuxAIO.sh && bash linuxAIO.sh`
- **For Ubuntu, Debian and CentOS Users Only:** use the option `4. Auto-Install Prerequisites` to install the latest version of .NET Core SDK.
- Use option `1. Download NadekoBot` to update your NadekoBot to 1.9.x.
- Next, just [run your NadekoBot.](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/#running-nadekobot)
- *NOTE: 1.9.x uses `NadekoBot.db` file from `NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data` folder.*
