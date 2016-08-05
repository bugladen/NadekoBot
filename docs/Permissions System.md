# Overview
Ever stuck handling your Nadeko permissions? Look no further than this guide!
We'll handle a few example cases that we've seen frequently on the help channel, as well as explaining the order of permissions in general. Any command used here can be found in the [Commands list](Commands List.md#permissions)

# How does it work?
Permissions are handy to set up to limit who in your server can use what. by default, every command is enabled for everyone, except for the administration commands, like `.kick` and `.prune`, which are set to correspond to discord's permissions, (kicking and managing messages respectively here), the permissions module, which we will show in the next paragraph, and some other commands throughout the other modules that are owner-only, meaning that only someone who has his or her id in the list of owners of the bot can use them. 
Since you may not want to allow everyone to use the NSFW module, skip the current song, pick up flowers, or attack in the pokegame, you're in need of setting up permissions.

#First time setting up
When you want to change your first permissions, you need to fulfill one of two conditions:
* Be the owner of the server
* Have the role set by `;permrole` (Nadeko by default)

If you have neither of these, you **can't** set up permissions.
You may want to change the `;permrole` to the role of the admins, using `;permrole Admins`.

# Basics
Most of the commands found in the list are pretty much self-explanatory. `;rolemdl NSFW disable lurkers` would disable the NSFW module for the lurkers (let them come out of the shadows!). similarly `;chnlperms #general` would show which permissions are banned *specifically* for this channel.
Since permissions are enabled by default, the hierarchy of rulings is simple, if you disable something that affects a user, like `;sm nsfw disable`, you **can't** enable it in a particular other way, like `;cm nsfw enable #nsfw`. Now roles are an exemption to this, e.g. if all roles have music disabled except for the DJ role, you can still use music commands if you have the DJ role.

By default, the bot notifies when a command can't be used. To disable this, you can use `;verbose false`.

# Common Cases
These are some common cases of particular settings of permissions.

## Create a music DJ
e.g. you only want your users to be able to see what's playing, and have a DJ role for the rest.
- `;arc music disable all`, disable all commands of the music module for everyone.
- `;arc music enable DJ`, give permissions to the DJ to do everything
- `;rc "!!nowplaying" enable all`, enable the command for everyone
- `;rc "!!getlink" enable all`, as above.
- `;rc "!!listqueue" enable all`, as above.

## Create a NSFW channel
You want to only allow NSFW commands in the #nsfw channel.
- `;cm nsfw disable all` disable the nsfw module in every channel.
- `;cm nsfw enable #nsfw` re-enable the nsfw module in the #nsfw channel.


-- *Thanks to @applemac for writing this guide*

#Old Guide

**NadekoBot's permissions can be set up to be very specific through commands in the Permissions module.**

Each command or module can be turned on or off at: 
- a user level (so specific users can or cannot use a command/module)  
- a role level (so only certain roles have access to certain commands/module)
- a channel level (so certain commands can be limited to certain channels, which can prevent music / trivia / NSFW spam in serious channels)
- a server level. 

Use .modules to see a list of modules (sets of commands).
Use .commands [module_name] to see a list of commands in a certain module.

Permissions use a semicolon as the prefix, so always start the command with a ;.

Follow the semicolon with the letter of the level which you want to edit.
- "u" for Users.
- "r" for Roles.
- "c" for Channels.
- "s" for Servers.

Follow the level with whether you want to edit the permissions of a command or a module.
- "c" for Command.
- "m" for Module.

Follow with a space and then the command or module name (surround the command with quotation marks if there is a space within the command, for example "!!q" or "!!n").

Follow that with another space and, to enable it, type one of the following: [1, true, t, enable], or to disable it, one of the following: [0, false, f, disable].

Follow that with another space and the name of the user, role, channel. (depending on the first letter you picked)

###### Examples #1
- **;rm NSFW 0 [Role_Name]**  Disables the NSFW module for the role, <Role_Name>.
- **;cc "!!n" 0 [Channel_Name]**  Disables skipping to the next song in the channel, <Channel_Name>.
- **;uc "!!q" 1 [User_Name]**  Enables queuing of songs for the user, <User_Name>.
- **;sm Gambling 0**  Disables gambling in the server.

Check permissions by using the letter of the level you want to check followed by a p, and then the name of the level in which you want to check. If there is no name, it will default to yourself for users, the @everyone role for roles, and the channel in which the command is sent for channels.

###### Examples #2
- ;cp [Channel_Name]
- ;rp [Role_Name]

Insert an **a** before the level to edit the permission for all commands / modules for all users / roles / channels / server.

Reference the Help command (-h) for more Permissions related commands.
