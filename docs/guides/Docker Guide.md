# Setting up NadekoBot on Docker
Nadeko is written in C# and Discord.Net for more information visit <https://github.com/Kwoth/NadekoBot>

## Before you start ...

... If your PC falls under any of the following cases, please grab Docker Toolbox instead.

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

## Fool-proof Quick start guide - Just want to get things working

Just copy everything down below (in one block of text), and paste it to your console, and it should perform it's magic on its own.

```
docker pull willysunny/nadecker:latest
docker stop nadeko
docker cp nadeko:/root/nadeko/credentials.json credentials.json
docker cp nadeko:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data/NadekoBot.db NadekoBot.db
docker rm nadeko
docker create --name=nadeko -v /nadeko/conf:/root/nadeko -v /nadeko/data/:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data willysunny/nadecker:latest
docker cp credentials.json nadeko:/root/nadeko
docker cp NadekoBot.db nadeko:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data/NadekoBot.db
docker start -a nadeko
```

First time install might encounter a few errors along the way (Namely step 2, 3, 4, 5, 8), this is to be expected, as you do not have the settings/files set up.

## Step-by-step Explanation

### 1. Grabbing the latest build

**Command:** `docker pull willysunny/nadecker:latest`

This will grab the latest Nadeko Docker image file from the internet and get ready to be used later.

### 2. Stopping any existing Nadekobot container

**Command:** `docker stop nadeko`

This will stop previously running docker container (if exist)

### 3. Backup your credentials.json file

**Command:** `docker cp nadeko:/root/nadeko/credentials.json credentials.json`

Technically speaking, you do not need to run this. But for the sake of fool-proof, this would make a copy of the credentials.json from the docker container and put it to your home folder.

### 4. Backup your NadekoBot.db file

**Command:** `docker cp nadeko:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data/NadekoBot.db NadekoBot.db`

Again, you most likely do not need to run this. But for the sake of fool-proof, this would make a copy of the NadekoBot.db from the docker container and put it to your home folder.

### 5. Remove the current NadekoBot container

**Command:** `docker rm nadeko`

This will delete the bot container, along with any of its settings inside. (That's why we made the backup of the two important files above)

### 6. Creating a new NadekoBot container with updated files

**Command:** `docker create --name=nadeko -v /nadeko/conf:/root/nadeko -v /nadeko/data/:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data willysunny/nadecker:latest`

This command will build a new nadekobot container based on the files we've pulled from **__Step 1__**.

And it will link two folders from your local drive and store the data within. Namely your **__credentials.json__**, which is saved under **__/nadeko/conf__**,  and **__NadekoBot.db__**, which is saved under **__/nadeko/data__**.

However, in the case if you did not create the folders before hand, or if you were using Windows and did not set up permission right, no files will be generated. (This is why there's the fool-proof steps 3, 4, 7 and 8)

### 7. Copy credentials.json file back into the container

**Command:** `docker cp credentials.json nadeko:/root/nadeko`

Technically speaking, if the file exists in /nadeko/conf, then you do not need to run this. But for the sake of fool-proof, this command makes a copy of the credentials.json from your home folder and it'll be placed in the docker container.

### 8. Copy NadekoBot.db database back into the container

**Command:** `docker cp NadekoBot.db nadeko:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data/NadekoBot.db`

As I've been saying, this is yet another redundent step, just to make the whole thing fool-proof. This command copies the database with all the user info (such as the currency, experience, level, waifus, etc) and put it into the container.

### 9. Start the bot!

**Command:** `docker start -a nadeko`

This would start the bot and attach the output of the bot on screen, similiar to you running `docker logs -f nadeko` after the bot has started.

### Additional Info
If you have any issues with the docker setup, please ask in #help channel on our [Discord server](https://discordapp.com/invite/nadekobot) but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the [documentation](http://nadekobot.readthedocs.io/en/latest).
