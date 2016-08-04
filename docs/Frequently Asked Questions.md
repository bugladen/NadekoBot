Frequently Asked Questions
####Q1: How do I get @Nadeko to join my server?
A: Send her a Direct Message with -h to get the link. Only Server Owners can allow the bot to join
####Q2: I want to change permissions, but it doesn't work D:
A: To change permissions, you have to set the ;permsrole (default permission role is called `Nadeko`, you can create a role named like that and assign it to yourself). *Only the owner of the server can change permissions without having this role.*
####Q3: Music doesn't work on a Mac?!  
You have to build `mono` from source - [Mono Source][Mono Source]. 
####Q5: I want to disable NSFW on my server, please?
A: You would first have to be able to change permissions (see Q4 ), and then run `;sm NSFW disable`
####Q6: How do I get NadekoFlowers/whatever I changed my currency to?
A: You get NadekoFlowers by answering Trivia questions or picking them up after they have been generated with `>gc`, which you can then either plant (give away to a channel so that someone can pick it), gamble it with $betflip, $betroll and $jr, or spend on healing and setting your type in the Pokemon game.
####Q7: I have an issue/bug/suggestion, where can I get it noticed?
A: First of all, check [Issues][Issues] and `#suggestions` for your problem/improvement. If it's not there, create a new issue on [Issues][Issues].
####Q8: How do I use the command XXXX?
A: most commands have a description, with a usage guide if required; use -h command, like -h ;pr
####Q9: Music doesn't work!?
A: Music on @Nadeko will be re-enabled in the future, but for now your only option is to host yourself
If you are hosting your own bot, make sure ffmpeg is working correctly; running ffmpeg in the commandline should have a response. see [Guide](guides/Windows Guide.md) for more
####Q10: My music is still not working/very laggy?
A: Try switching server location, try giving the bot permissions on the server you want to use it on.
####Q12: I want to change data in the database (like NadekoFlowers or the pokemontypes of users, but how?
A: Open data/nadekobot.sqlite using sqlitebrowser (or some alternative), Browse Data, select relevant table, change data, Write changes
####Q13: The .greet and .bye commands doesn't work, but everything else is (From @Kong) 
A: Set a greeting message by using .greetmsg YourMessageHere 
and a bye-message by using .byemsg YourMessageHere
####Q15: How to import certs on linux?
A:
`certmgr -ssl https://discordapp.com`  
`certmgr -ssl https://gateway.discord.gg`
####Q16: I want "BOT" tag with my bot and I can't follow up with Q14, is there a simple way? 
A: Yes, you can create an application using your account and use the APP BOT USER TOKEN from here: [DiscordApp][DiscordApp]
NOTE: This will create a new bot account
####Q17: I made an application following Q16, but I can't add that new bot to my server, how do I invite it to my server?
A: You need to use oauth link to add it to you server, just copy your CLIENT ID (that's in the same Developer page where you brought your token) and replace `12345678` in the link below: 
`https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303`  
FOLLOW THIS [DETAILED GUIDE][DETAILED GUIDE] IF IT IS HARD FOR YOU TO UNDERSTAND 
####Q18: I'm building NadekoBot from source, but I get hundreds of (namespace) errors without changing anything!?
A: Using Visual Studio, you can solve these errors by going to Tools -> `NuGet Package Manager -> Manage NuGet Packages` for Solution. Go to the Installed tab, select the Packages that were missing (usually `Newtonsoft.json` and `RestSharp`) and install them for all projects  
####Q19: My bot has all permissions but it's still saying, "Failed to add roles. Bot has insufficient permissions.", how do I fix this?
A: Discord has added few new features and roles now follow hierarchy, that means you need to place your bot role above every-other role your server has. Also do NOTE that bot can only set/add all roles below its own highest role. And can not assign it's "highest role" to anyone else.

[Mono Source]:http://www.mono-project.com/docs/compiling-mono/mac/
[Issues]: https://github.com/Kwoth/NadekoBot/issues
[DiscordApp]: https://discordapp.com/developers/applications/me
[DETAILED GUIDE]: http://discord.kongslien.net/guide.html
