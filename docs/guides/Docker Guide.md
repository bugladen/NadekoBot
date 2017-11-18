# Setting up NadekoBot on Docker
Nadeko is written in C# and Discord.Net for more information visit <https://github.com/Kwoth/NadekoBot>

#### Prerequisites
- [Docker](https://docs.docker.com/engine/installation/)
- [Create Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server). 

#### Setting up the container
For this guide we will be using the folder /nadeko as our config root folder.
```
docker create --name=nadeko -v /nadeko/conf/:/root/nadeko -v /nadeko/data:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data uirel/nadeko:1.4
```

#### Moving `credentials.json` into the docker container. 

- If you are coming from a previous version of nadeko (the old docker) make sure your credentials.json has been copied into this directory and is the only thing in this folder.
- If you are making a fresh install, create your credentials.json from the following guide and place it in the /nadeko folder [Nadeko JSON Guide](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/). 
- To copy the the file from your computer to a container: 
```
docker cp /Directory/That/Contains/Your/credentials.json nadeko:/credentials.json
```

#### Start up docker
```
docker start nadeko; docker logs -f nadeko
```
The docker will start and the log file will start scrolling past. This may take a long time. The bot start can take up to 5 minutes on a small DigitalOcean droplet.
Once the log ends with "NadekoBot | Starting NadekoBot vX.X" the bot is ready and can be invited to your server. Ctrl+C at this point if you would like to stop viewing the logs.

After a few moments, Nadeko should come online on your server. If it doesn't, check the log file for errors. 

#### Monitoring
**To monitor the logs of the container in realtime** 
```
docker logs -f nadeko
```

### Updates

#### Manual
Updates are handled by pulling the new layer of the Docker Container which contains a pre compiled update to Nadeko.
The following commands are required for the default options

`docker pull uirel/nadeko:latest`

`docker stop nadeko; docker rm nadeko`

```
docker create --name=nadeko -v /nadeko/conf/:/root/nadeko -v /nadeko/data:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp2.0/data uirel/nadeko:1.4
```

`docker start nadeko`


#### Automatic
Automatic update are handled by [WatchTower](https://github.com/CenturyLinkLabs/watchtower).
To setup WatchTower to keep Nadeko up-to-date for you with the default settings, use the following command:

```bash
docker run -d --name watchtower -v /var/run/docker.sock:/var/run/docker.sock centurylink/watchtower --cleanup nadeko --interval 300
```

This will check for updates to the docker every 5 minutes and update immediately. To check in different intervals, change `X`. X is the amount of time, in seconds. (e.g 21600 for 6 hours)

### Additional Info
If you have any issues with the docker setup, please ask in #help channel on our [Discord server](https://discordapp.com/invite/nadekobot) but indicate you are using the docker.

For information about configuring your bot or its functionality, please check the [documentation](http://nadekobot.readthedocs.io/en/latest).
