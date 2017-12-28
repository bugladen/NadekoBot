## Setting up NadekoBot From Source

**Note: 32-bit Windows version is experimental**  
32-bit linux is not possible because of .Net compatability.

#### Prerequisites  
- [.net core sdk 2.x][.netcore]  
- [ffmpeg][ffmpeg] either download or install using your distro's package manager. For 32 bit Windows, download [this ffmpeg](https://github.com/MaybeGoogle/NadekoFiles/blob/master/x86%20Prereqs/NadekoBot_Music/ffmpeg.exe?raw=true).  
- [youtube-dl](http://rg3.github.io/youtube-dl/download.html)  
- [git][git]  
- [redis][redis] for windows, or `apt-get install redis-server` for linux. For 32 bit windows, download [redis-server.exe](https://github.com/MaybeGoogle/NadekoFiles/blob/master/x86%20Prereqs/redis-server.exe?raw=true).  
- In addition, for 32-bit Windows, download [libsodium](https://github.com/MaybeGoogle/NadekoFiles/blob/master/x86%20Prereqs/NadekoBot_Music/libsodium.dll?raw=true) and (lib)[opus](https://github.com/MaybeGoogle/NadekoFiles/blob/master/x86%20Prereqs/NadekoBot_Music/opus.dll?raw=true).  
- [Create Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server).   

#### Getting Nadeko Ready to Run  
`git clone -b 1.9 https://github.com/Kwoth/NadekoBot`  
- Edit the `credentials.json` in `NadekoBot/src/NadekoBot` according to this [guide](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-credentialsjson-file).  
- Move `youtube-dl.exe` and `ffmpeg.exe` into `NadekoBot/src/NadekoBot`. For 32-bit Windows, also replace `libsodium.dll` and `opus.dll` with the ones you downloaded.   

#### Running NadekoBot  
- For 32-bit Windows, run the `redis-server.exe` that you downloaded. You must have this window open when you use NadekoBot.
`cd NadekoBot/src/NadekoBot`   

`dotnet run -c Release`  

#### Updating Nadeko  
- Might not work if you've made custom edits to the source, make sure you know how git works)  

`git pull`  
`dotnet run -c Release`

**!!! NOTE FOR WINDOWS USERS  !!!**  
If you're running from source on windows, you will have to add these 2 extra lines to your credentials, after the first open bracket:
```js
    "ShardRunCommand": "dotnet",
    "ShardRunArguments": "run -c Release -- {0} {1}",
```

[.netcore]: https://www.microsoft.com/net/download/core#/sdk
[ffmpeg]: http://ffmpeg.zeranoe.com/builds/
[git]: https://git-scm.com/downloads
[redis]: https://github.com/MicrosoftArchive/redis/releases/latest
