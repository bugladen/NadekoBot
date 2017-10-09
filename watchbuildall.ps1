$cur = (Get-Item -Path ".\" -Verbose).FullName

function WatchBuild([string] $path, [string] $args)
{
    cd $cur
    cd $path
    cmd.exe /c "dotnet watch"
    cd $cur
}


WatchBuild(".\NadekoBot.Modules.CustomReactions")
WatchBuild(".\NadekoBot.Modules.Gambling")
WatchBuild(".\NadekoBot.Modules.Games")
WatchBuild(".\NadekoBot.Modules.Music")
WatchBuild(".\NadekoBot.Modules.Nsfw")
WatchBuild(".\NadekoBot.Modules.Pokemon")
WatchBuild(".\NadekoBot.Modules.Searches")
WatchBuild(".\NadekoBot.Modules.Utility")
WatchBuild(".\NadekoBot.Modules.Xp")
