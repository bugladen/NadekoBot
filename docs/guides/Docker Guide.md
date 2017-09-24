# NadekoBot a Discord bot 
Nadeko is written in C# and Discord.Net for more information visit <https://github.com/Kwoth/NadekoBot>

## Install Docker
Follow the respective guide for your operating system found here [Docker Engine Install Guide](https://docs.docker.com/engine/installation/)

## Nadeko Setup Guide
For this guide we will be using the folder /nadeko as our config root folder.

```bash
docker create --name=nadeko -v /nadeko/conf/:/root/nadeko -v /nadeko/data:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.1/data uirel/nadeko:1.4
```
-If you are coming from a previous version of nadeko (the old docker) make sure your credentials.json has been copied into this directory and is the only thing in this folder. 

-If you are making a fresh install, create your credentials.json from the following guide and place it in the /nadeko folder [Nadeko JSON Guide](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/)

Next start the docker up with `docker start nadeko; docker logs -f nadeko`

The docker will start and the log file will start scrolling past. Depending on hardware the bot start can take up to 5 minutes on a small DigitalOcean droplet.
Once the log ends with "NadekoBot | Starting NadekoBot v1.0-rc2" the bot is ready and can be invited to your server. Ctrl+C at this point to stop viewing the logs.

After a few moments you should be able to invite Nadeko to your server. If you cannot, check the log file for errors. 

## Monitoring

* Monitor the logs of the container in realtime `docker logs -f nadeko`.

## Updates

# Manual
Updates are handled by pulling the new layer of the Docker Container which contains a pre compiled update to Nadeko.
The following commands are required for the default options

`docker pull uirel/nadeko:latest`

`docker stop nadeko; docker rm nadeko`

```
docker create --name=nadeko -v /nadeko/conf/:/root/nadeko -v /nadeko/data:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.1/data uirel/nadeko:1.4
```

`docker start nadeko`


# Automatic Updates
Automatic update are now handled by WatchTower [WatchTower GitHub](https://github.com/CenturyLinkLabs/watchtower)
To setup WatchTower to keep Nadeko up-to-date for you with the default settings, use the following command

```bash
docker run -d --name watchtower -v /var/run/docker.sock:/var/run/docker.sock centurylink/watchtower --cleanup nadeko
```

This will check for updates to the docker every 5 minutes and update immediately. Alternatively using the `--interval X` command to change the interval, where X is the amount of time in seconds to wait. e.g 21600 for 6 hours.


If you have any issues with the docker setup, please ask in #help channel on our [Discord server](https://discordapp.com/invite/nadekobot) but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the [documentation](http://nadekobot.readthedocs.io/en/latest).
