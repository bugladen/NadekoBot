#Frequently Asked Questions


###Question 1: How do I get Nadeko to join my server?
----
**Answer:** Simply send Nadeko a Direct Message with -h and follow the link. **Only Server Owners can add the bot to the server**
###Question 2: I want to change permissions, but it isn't working!
----
**Answer:** You must have the ;permsrole (by default this is the "Nadeko" role, for more details on permissions check [here](http://nadekobot.readthedocs.io/en/latest/Permissions%20System/ "Permissions"))

**Please note:** *Only the Server Owner can change permissions without the "Nadeko" role*.
###Question 3: Music isn't working on Mac!!
----
**Answer:** You will have to build `mono` from source. Simply follow the [mono-guide](http://www.mono-project.com/docs/compiling-mono/mac/ "Building mono").
###Question 4: I want to disable NSFW on my server.
----
**Answer:** To disable the NSFW Module for your server type, `;sm NSFW disable`. If this does not work refer to Question 2.
###Question 5: How do I get NadekoFlowers/Currency?
----
**Answer:** You get NadekoFlowers by answering Trivia questions or picking them up after they have been generated with `>gc`, which you can then either plant (give away to a channel so that someone can pick it), gamble it with `$betflip`, `$betroll` and `$jr`, or spend on healing and setting your type in the Pokemon game.
###Question 6: I have an issue/bug/suggestion, where do I put it so it gets noticed?
-----------
**Answer:** First, check [issues](https://github.com/Kwoth/NadekoBot/issues "GitHub NadekoBot Issues"), then check the `#suggestions` in the Nadeko [help server](https://discord.gg/0ehQwTK2RBjAxzEY).

If your problem or suggestion is not there, feel free to request it either in Issues or in `#suggestions`.
###Question 7: How do I use this command?
--------
**Answer:** You can see the description and usage of certain commands by using `-h command` **i.e** `-h ;sm`. 

The whole list of commands can be found [here](http://nadekobot.readthedocs.io/en/latest/Commands%20List/ "Command List")
###Question 8: Music isn't working?
----
**Answer:** Music is disabled on public Nadeko, it will be re-enabled later in the future. 

**If you would like music you must host Nadeko yourself**. Be sure you have FFMPEG installed correctly, read the [guide](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/) for more info.
###Question 9: My music is still not working/very laggy?
----
**Answer:** Try changing your discord [location][1], if this doesn't work be sure you have enabled the correct permissions for Nadeko.
[1]: https://support.discordapp.com/hc/en-us/articles/216661717-How-do-I-change-my-Voice-Server-Region-
###Question 10: I want to change data in the database (like NadekoFlowers or the pokemontypes of users, but how?
----
**Answer:** Open data/nadekobot.sqlite using sqlitebrowser (or some alternative), Browse Data, select relevant table, change data, Write changes
###Question 11: The .greet and .bye commands doesn't work, but everything else is (From @Kong)
-----
**Answer:** Set a greeting message by using `.greetmsg YourMessageHere` and a bye-message by using `.byemsg YourMessageHere`
###Question 12: How do I import certs on linux?
-------
**Answer:** 

`certmgr -ssl https://discordapp.com`

`certmgr -ssl https://gateway.discord.gg`
###Question 13: I want "BOT" tag with my bot a, is there a simple way?
----
**Answer:** Yes, you can create an application using your account and use the APP BOT USER TOKEN from here: [DiscordApp][1] **NOTE: This will create a new bot account**
[1]:https://discordapp.com/developers/applications/me

###Question 14:  I made an application, but I can't add that new bot to my server, how do I invite it to my server?
----
**Answer:** You need to use oauth link to add it to you server, just copy your CLIENT ID (that's in the same Developer page where you brought your token) and replace `12345678` in the link below: https://discordapp.com/oauth2/authorize?client_id=12345678&scope=bot&permissions=66186303

Follow this Detailed [Guide](http://discord.kongslien.net/guide.html) if you do not understand.
###Question 15:I'm building NadekoBot from source, but I get hundreds of (namespace) errors without changing anything!?
-----
**Answer:** Using Visual Studio, you can solve these errors by going to `Tools` -> `NuGet Package Manager` -> `Manage NuGet Packages for Solution`. Go to the Installed tab, select the Packages that were missing (usually `Newtonsoft.json` and `RestSharp`) and install them for all projects
###Question 16:  My bot has all permissions but it's still saying, "Failed to add roles. Bot has insufficient permissions.". How do I fix this?
----------
**Answer:** Discord has added a few new features and roles now follow hierarchy. This means you need to place your bot's role above every-other role your server has. 

**Please Note:** *The bot can only set/add all roles below its own highest role. It can not assign it's "highest role" to anyone else.*
