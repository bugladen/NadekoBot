Permissions Overview
===================
Have you ever felt confused or even overwhelmed when trying to set Nadeko's permissions? In this guide we will be explaining how to use the 
permission commands correctly and even cover a few common questions! Every command we discuss here can be found in the [Commands List](http://nadekobot.readthedocs.io/en/latest/Commands%20List/#permissions).

Why do we use the Permissions Commands?
------------------------------
Permissions are very handy for setting who can use what commands in your server. By default, every command is enabled for everybody, however a few exclusions are the Administration Commands like, `.kick` and `.prune` and Bot Owner-Only commands as these require your id to be in [`credentials.json`](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/ "Setting up your credentials"). 
With the Permission Commands it is possible to restrict who can skip the current song, pick NadekoFlowers, or even use the NSFW module.

First Time Setup
------------------
To change permissions you must meet one of two requirements, either:
+ **Be the Server Owner** 
+ Have the role specified by `;permrole` (is Nadeko by default).

If you meet neither of these requirements, you ***can not*** edit permissions. 

If you would like to allow Admins to edit permissions you may want to rename the `;permrole` to `;permrole Admins` and give each admin that role.

Basics & Heirachy
-----
Most of the commands found in the list describe what they do, but we will cover a few here.

If you would like to disable the NSFW module for a certain role you would use `;rolemdl NSFW disable SomeRole`. 

Similarly you can view which Modules and Commands are banned on a _specific_ channel with `;chnlperms SomeChannel`.

The heirachy of the Permissions are simple. If you disable a Module/command with a command that affects the server, `;sm NSFW disable` you can not then enable it another way such as, `;cm NSFW enable SomeChannel`. Roles are an exemption to this, i.e if all roles except for the DJ Role have music disabled, you can still use music commands if you have the DJ Role.

The bot, by default will notify you when a command can't be used. To disable this notification simply use `;verbose false`.


Commonly Asked Questions
---------------
###How do I create a music DJ?
To allow users to only see the current song and have a DJ role for queuing follow these five steps: 

  1. `;arc music disable all` 
1. Disables all music commands for everyone.
  2. `;arc music enable DJ` 
1. Gives all music commands to DJ role.
  3. `;rc !!nowplaying enable all` 
1.  Enables the "nowplaying" command for eveone.
  4. `;rc !!getlink enable all` 
1. Enables the "getlink" command for everyone.
  5. `;rc !!listqueue enable all` 
1. Enables the "listqueue" command for everyone.

###How do I create an NSFW channel?
You want to only allow NSFW commands in the #nsfw channel. - `;cm nsfw disable all` disable the nsfw module in every channel. - `;cm nsfw enable #nsfw` re-enable the nsfw module in the #nsfw channel.

_-- Thanks to @applemac for writing this guide_

Old Guide
---------
**NadekoBot's permissions can be set up to be very specific through commands in the Permissions module.**

Each command or module can be turned on or off at: 
- The user level (so specific users can or cannot use a command/module)
- The role level (so only certain roles have access to certain commands/module) 
- The channel level (so certain commands can be limited to certain channels, which can prevent music / trivia / NSFW spam in serious channels)
- The server level.

Use `.modules` to see a list of modules (sets of commands). Use `.commands [module_name]` to see a list of commands in a certain module.

Permissions use a semicolon as the prefix, so always start the command with a `;`.

Follow the semicolon with the letter of the level which you want to edit: 
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

####Examples #1

- `;rm NSFW 0 [Role_Name]` Disables the NSFW module for the role,
- `;cc "!!n" 0 [Channel_Name]` Disables skipping to the next song in the channel, 
- `;uc "!!q" 1 [User_Name]` Enables queuing of songs for the user, 
- `;sm Gambling 0 Disables` gambling in the server.
 
 Check permissions by using the letter of the level you want to check followed by a p, and then the name of the level in which you want to check. If there is no name, it will default to yourself for users, the @everyone role for roles, and the channel in which the command is sent for channels.

####Examples #2

- `;cp [Channel_Name]`
- `;rp [Role_Name]`

Insert an "a" before the level to edit the permission for all commands / modules for all users / roles / channels / server.

Reference the Help command (-h) for more Permissions related commands.
