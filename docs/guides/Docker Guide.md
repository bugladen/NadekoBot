# Setting up NadekoBot on Docker
Nadeko is written in C# and Discord.Net. For more information visit <https://github.com/Kwoth/NadekoBot>

## Before you start

If your PC falls under any of the following cases, please grab Docker Toolbox instead.

For Windows [[Download Link](https://download.docker.com/win/stable/DockerToolbox.exe)]
- Any Windows version without Hyper-V Support
- Windows 10 Home Edition
- Windows 8 and earlier

For Mac [[Download Link](https://download.docker.com/mac/stable/DockerToolbox.pkg)]
- Any version between 10.8 “Mountain Lion” and 10.10.2 "Yosemite"

## Prerequisites
- [Docker](https://store.docker.com/search?type=edition&offering=community) or [Docker Toolbox](https://www.docker.com/products/docker-toolbox).
- [Create Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 
- Have your [credentials.json](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-credentials) in your home folder. To go to your home folder on ...
- Linux: **cd ~**
- Mac: **⌘ + Shift + H**
- Windows: Enter **%userprofile%** in your address bar

## Get a default credentials.json

You can use the following instructions to get a usable credentials.json. You then just have to edit the file with the Discord identifiers you got from the prerequisites.

- [Windows] Open a command prompt by opening the Start menu, typing cmd then selecting Command Prompt
- [*nix] Open a terminal
- [Windows] Go on your home folder by typing `cd %userprofile%`
- [*nix] Go on your home folder by typing `cd ~`
- Type the following commands

```
docker run --name _nadeko kwoth/nadekobot sh
docker stop _nadeko
docker cp _nadeko:/app/credentials_example.json credentials.json
docker rm _nadeko
```

## Start the bot

- Creates an empty folder. It will contain all Nadeko's data, including the credentials.json
- Download the file [docker-compose.yml](https://raw.githubusercontent.com/Kwoth/NadekoBot/1.9/docker-compose.yml) and place it on the previously created folder.
- Edit the `docker-compose.yml` by uncommenting the line `image:` and commenting the line `build:`
- Put your credentials.json in this folder
- Open a command line, go to the folder with this command line: `cd "your_folder"`
- Type the following command to start the bot

```
docker-compose up -d nadeko
```

By default, the bot will automatically restart with the Docker daemon with the instruction `restart: unless-stopped`.

You can see what's going on with the following command. This is very recommanded the first time you run the bot or if something goes south to help you diagnose the issue:

```
docker-compose logs -ft --tail=50 nadeko
```

## Watchtower

Watchtower is a small utility that monitors Docker containers and update them if a new release is available on the upstream. It can be used to automatically update Nadeko.

You will find a configuration sample on the `docker-compose.yml`. You just have to remove the comment symbol and then `docker-compose up -d watchtower` to spin it up.

WARNING: as the docker.sock is mount into this container, this means in fact that this container have a full control not only on all containers but also on your computer. You can read more [here](https://www.projectatomic.io/blog/2015/08/why-we-dont-let-non-root-users-run-docker-in-centos-fedora-or-rhel/)

### Additional Info
If you have any issues with the docker setup, please ping willy_sunny by typing <@113540879297302528> in #help channel on our [Discord server](https://discordapp.com/invite/nadekobot), or dm him directly, but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the [documentation](http://nadekobot.readthedocs.io/en/latest).
