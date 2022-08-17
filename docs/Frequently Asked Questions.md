# Frequently Asked Questions

### Question 1: How do I get Nadeko to join my server?

---

**Answer:** Send Nadeko a Direct Message with `.h` and follow the link or simply [click here](https://invite.nadeko.bot/).  
Only users with **Manage Server permission** can add bots to a server.

### Question 2: I want to change permissions, but it isn't working!

---

**Answer:** You must have **Administrator Server permission** or the `.permrole` to be able to use any command from the Permissions module. For more information about permissions, check the [Permission guide](http://nadekobot.readthedocs.io/en/latest/Permissions%20System/ "Permissions").

### Question 3: I want to enable NSFW on a channel.

---

**Answer:** To enable the NSFW module on one channel, you need to mark the channel as NSFW in the channel's settings. [Click here](https://cdn.discordapp.com/attachments/422985724053159946/429510585097650186/nsfwenable2.gif) to learn how.

### Question 4: How do I get NadekoFlowers/Currency?

---

**Answer:** On public Nadeko, you can get NadekoFlowers by [voting](https://discordbots.org/bot/nadeko/vote) for her on DiscordBots.com or donating money on [Patreon](https://patreon.com/nadekobot) or [PayPal](https://paypal.me/Kwoth). You can also gamble with potential profit with `.betflip`, `.betroll` and other gambling commands.  

On self-hosts, you can also get NadekoFlowers by picking them up after they have been generated with `.gc` or planted by someone else, clicking on reaction events or typing `.timely` (assuming the bot owner has set a timely reward).

### Question 5: I have an issue/bug/suggestion, where do I put it so it gets noticed?

---

**Answer:** First, check [issues](https://gitlab.com/Kwoth/nadekobot/issues "GitLab NadekoBot Issues"), then check the [suggestions](https://nadeko.bot/suggest).

If your problem or suggestion is not there, feel free to request/notify us about it either in the Issues section of GitLab for issues or on [suggestions page](https://nadeko.bot/suggest) for suggestions.

### Question 6: How do I use this command?

---

**Answer:** You can see the description and usage of certain commands by using `.h command` **i.e** `.h .sm`. Additionally, you can check all commands within a module by typing `.cmds moduleName -v 1`.

The list of commands can be found [here](https://nadeko.bot/commands "Command List")

### Question 7: Music isn't working?

---

**Answer:** Music is disabled on public Nadeko due to large hosting costs.

**If you would like music in the meantime, you must host Nadeko yourself**. Be sure you have FFMPEG and youtube-dl installed correctly, and have followed the guide for your OS carefully. Keep in mind that, for Linux, music is currently broken on Debian and rpm-based distros (i.e. CentOS).

### Question 8: My music is still not working/very laggy?

---

**Answer:** Try changing your discord [location][1]. If this doesn't work, be sure you have enabled the correct permissions for Nadeko and rebooted since installing FFMPEG. If you're on Windows, make sure you've installed Visual C++.

[1]: https://support.discordapp.com/hc/en-us/articles/216661717.how-do-I-change-my-Voice-Server-Region-

### Question 9: I want to change data in the database like NadekoFlowers or something else but how?

---

**Answer:** Follow the [DB Guide](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#db-files), Open `/data/NadekoBot.db` using **sqlitebrowser** (or some alternative), browse data, select the relevant table, change the data, write the changes and you're done. Alternatively, you can also run SQL queries with `.sqlexec` and `.sqlselect`.

### Question 10: The .greet and .bye commands don't work, but everything else does!

---

**Answer:** Set a greeting message by using `.greetmsg YourMessageHere` and a bye-message by using `.byemsg YourMessageHere`. Don't forget that `.greet` and `.bye` only apply to users joining a server, not coming online/offline. Also, keep in mind that these messages are automatically deleted after 30 seconds of being sent. If you don't want that, disable automatic deletion by setting `.greetdel` and `.byedel` to zero.

### Question 11: I made an application, but I can't add that new bot to my server, how do I invite it to my server?

---

**Answer:** You need to use oauth link to add it to you server, just copy your **CLIENT ID** (that's in the same [Developer page](https://discordapp.com/developers/applications/me) where you brought your token) and replace `12345678` in the link below: **https://discordapp.com/oauth2/authorize?client_id=`12345678`&scope=bot&permissions=66186303**

### Question 12: I'm building NadekoBot from source, but I get hundreds of (namespace) errors without changing anything!?

---

**Answer:** Using Visual Studio, you can solve these errors by going to `Tools` -> `NuGet Package Manager` -> `Manage NuGet Packages for Solution`. Go to the Installed tab, select the Packages that were missing (usually `Newtonsoft.json` and `RestSharp`) and install them for all projects

### Question 13: My bot has all permissions but it's still saying, "Failed to add roles. Bot has insufficient permissions". How do I fix this?

---

**Answer:** Discord has added a few new features and the roles now follow the role hierarchy, which means you need to place your bot's role above every other role your server has to fix the role hierarchy issue. [Here](https://support.discordapp.com/hc/en-us/articles/214836687-Role-Management-101) is a link to Discord's Role Management 101.

**Please Note:** *The bot can only set/add all roles below its own highest role. It cannot assign it's "highest role" to anyone else.*

### Question 14: I've broken permissions and am stuck. Can I reset it?

---

**Answer:** Yes, there is a way, in one easy command! Just run `.resetperms` and all the permissions you've set through **Permissions Module** will reset.
