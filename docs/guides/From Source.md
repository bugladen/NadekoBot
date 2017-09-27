### Prerequisites  
- [.net core sdk 2.0][.netcore]  
- [ffmpeg][ffmpeg] (and added to path) either download or install using your distro's package manager  
- [git][git]
- [redis][redis] for windows, or `apt-get install redis-server` for linux

### Clone The Repo
`git clone -b 1.9 https://github.com/Kwoth/NadekoBot`  
`cd NadekoBot/src/NadekoBot`  
Edit `credentials.json.` Read the JSON Exaplanations guide on the left if you don't know how to set it up   

### Run
`dotnet run -c Release`  

*when you decide to update in the future (might not work if you've made custom edits to the source, make sure you know how git works)*  
`git pull`  
`dotnet run -c Release`

[.netcore]: https://www.microsoft.com/net/download/core#/sdk
[ffmpeg]: http://ffmpeg.zeranoe.com/builds/
[git]: https://git-scm.com/downloads
[redis]: https://github.com/MicrosoftArchive/redis/releases/latest