## Setting Up NadekoBot on Windows With the Updater

| Table of Contents                                                                                                                                                                             |
| :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Prerequisites](#prerequisites)                                                                                             |
| [Setup](#setup)                                                                                                             |
| [Starting the Bot](#starting-the-bot)                                                                                       |
| [Updating Nadeko](#updating-nadeko)                                                                                         |
| [Manually Installing the Prerequisites from the Updater](#if-the-updater-fails-to-install-the-prerequisites-for-any-reason) |

*Note: If you want to make changes to Nadeko's source code, please follow the [From Source][SourceGuide] guide instead.*

*If you have Windows 7 or a 32-bit system, please refer to the [From Source][SourceGuide] or [Docker][DockerGuide] guides.*

#### Prerequisites

- Windows 8 or later (64-bit)
- [dotNET Core 2.1 SDK][dotNET] (restart Windows after installation)
- [Redis][Redis] (supplied with the updater)
- [Create a Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server).

**Optional**

- [Notepad++] (makes it easier to edit your credentials)
- [Visual C++ 2010 (x86)] and [Visual C++ 2017 (x64)] (both are required if you want Nadeko to play music - restart Windows after installation)

#### Setup

- Download and run the [NadekoBot Updater][Updater].
- <img src="https://i.imgur.com/1g58bo1.png" alt="NadekoBot Updater Window" width="495" height="395" border="10"/>
- Click on **`Install Redis`** to install Redis.
- Select this option during the Redis installation:
- ![Redis Path](https://i.imgur.com/uUby6Xw.png "Redis PATH")
- Click on **`Install ffmpeg`** and **`Install youtube-dl`** if you want music features.
- Click on **`Update`** and go through the installation wizard to install Nadeko.
- When installation is finished, make sure the **`Open credentials.json`** option is checked.

*If you happen to close the wizard with that option unchecked, you can easily find the credentials file in `C:\Program Files\NadekoBot\system`.*

- [Set up the credentials.json](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file) file.

#### Starting the bot

- Either click on **`Launch Bot`** button in the updater or run the bot via its desktop shortcut.

#### Updating Nadeko

- Make sure Nadeko is closed and not running  
  (Run `.die` in a connected server to ensure it's not running).
- Open NadekoBot Updater
- If updates are available, you will be able to click on the Update button
- Launch the bot
- You've updated and are running again, easy as that!

#### If the updater fails to install the prerequisites for any reason

You can still install them manually:

- [Redis Installer](https://github.com/MicrosoftArchive/redis/releases/tag/win-3.0.504) - Download and run the **`.msi`** file
- [ffmpeg] - Download the Release build and move the file to **`C:\ffmpeg`**, extract its contents and rename the folder to `nightly`.
    - If that still fails, move the `ffmpeg.exe` file to **`C:\Program Files\NadekoBot\system`**.
- [youtube-dl] - Click on `Windows.exe` (on the top left corner) and download the file. Then move it to **`C:\Program Files\NadekoBot\system`**.

[Updater]: https://dl.nadeko.bot/
[Notepad++]: https://notepad-plus-plus.org/
[dotNET]: https://www.microsoft.com/net/download/dotnet-core/2.1
[Redis]: https://github.com/MicrosoftArchive/redis/releases/download/win-3.0.504/Redis-x64-3.0.504.msi
[Visual C++ 2010 (x86)]: https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe
[Visual C++ 2017 (x64)]: https://aka.ms/vs/15/release/vc_redist.x64.exe
[SourceGuide]: https://nadekobot.readthedocs.io/en/latest/guides/From%20Source/
[DockerGuide]: https://nadekobot.readthedocs.io/en/latest/guides/Docker%20Guide/
[ffmpeg]: https://ffmpeg.zeranoe.com/builds/
[youtube-dl]: https://rg3.github.io/youtube-dl/download.html
