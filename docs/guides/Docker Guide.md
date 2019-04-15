# Setting up NadekoBot on Docker

Nadeko is written in C# and Discord.Net. For more information visit <https://gitlab.com/Kwoth/nadekobot>

## Before you start

If your PC falls under any of the following cases, please grab Docker Toolbox instead.

For Windows [[Download Link](https://download.docker.com/win/stable/DockerToolbox.exe)]

- Any Windows version without Hyper-V support
- You have Hyper-V but it must be disabled
- Windows 10 Home Edition
- Windows 8 and earlier

For Mac [[Download Link](https://download.docker.com/mac/stable/DockerToolbox.pkg)]

- Any version between 10.8 “Mountain Lion” and 10.10.2 "Yosemite"

## Prerequisites

1. [Docker](https://store.docker.com/search?type=edition&offering=community) or [Docker Toolbox](https://www.docker.com/products/docker-toolbox).
2. [Create Discord Bot application](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#creating-discord-bot-application) and [Invite the bot to your server](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#inviting-your-bot-to-your-server).
3. Have your [credentials.json](http://nadekobot.readthedocs.io/en/latest/JSON%20Explanations/#setting-up-your-credentials) ready. See the next section to get a default credentials.json.

## Get a default credentials.json

You can use this section to get a blank credentials.json. You then just have to edit the file with the Discord identifiers you got from the prerequisites.

### Windows

- Open a command prompt by opening the Start menu, typing cmd then selecting Command Prompt
- Copy/paste the following commands

```batch
cd %TEMP%
docker run --name temp_nadeko --entrypoint sh kwoth/nadeko
docker stop temp_nadeko
docker cp temp_nadeko:/app/credentials_example.json credentials.json
docker rm temp_nadeko
echo Your blank credentials.json has been downloaded here: %TEMP%\credentials.json
```

### Linux and other \*NIX systems

- Open your favorite terminal
- Copy/paste the following commands

```bash
cd /tmp
docker run --name temp_nadeko --entrypoint sh kwoth/nadeko
docker stop temp_nadeko
docker cp temp_nadeko:/app/credentials_example.json credentials.json
docker rm temp_nadeko
echo Your blank credentials.json has been downloaded here: /tmp/credentials.json
```

## Start the bot

- Creates an empty folder. It will contain all Nadeko's datas, including the credentials.json.

### Windows

This step-by-step will assume you choose %LOCALAPPDATA%\Nadeko to store the Nadeko's data but you can choose another location.

- Copy your modified credentials.json into this folder
- Copy/paste the following commands

```powershell
# you can modify thoses variables
$workdir="$env:LOCALAPPDATA\Nadeko"
$url_dc="https://gitlab.com/Kwoth/nadekobot/raw/1.9/docker-compose.yml"

# please do not modify thoses variables
$dc="$workdir\docker-compose.yml"

# installation
If (!(Test-Path "$workdir")) {
    New-Item "$workdir" -Type Directory
}
Invoke-WebRequest -Uri "$url_dc" -OutFile "$dc"
cat "$dc" | %{ $_ -replace '#image: kwoth','image: kwoth' } | %{ $_ -replace 'build:','#build:' } > "$dc.tmp"
Move-Item "$dc.tmp" "$dc" -Force
cat "$dc" | %{ $_ -replace '- ./([^:]*):', "- $workdir\`$1:" } > "$dc.tmp"
Move-Item "$dc.tmp" "$dc" -Force

# start
cd "$workdir"
If ((Test-Path "$dc" -PathType Leaf) -And (Test-Path "credentials.json" -PathType Leaf)) {
docker-compose up -d nadeko
} Else {
    Write-Host "Please ensure the $dc and credentials.json files exist and are correct before starting the bot!"
}
```

### Linux

This step-by-step will assume you choose $HOME/nadeko to store the Nadeko's data but you can choose another location.

- Copy your modified credentials.json into this folder
- Copy/paste the following commands

```bash
# you can modify thoses variables
workdir="$HOME/nadeko"
url_dc="https://gitlab.com/Kwoth/nadekobot/raw/1.9/docker-compose.yml"

# please do not modify thoses variables
dc="$workdir/docker-compose.yml"

# installation
mkdir -p "$workdir"
wget -O "$dc" "$url_dc"
sed -i -e 's/#\(image: kwoth\)/\1/' -e '/build:/d' "$dc"
sed -i -e 's%- ./\([^:]*\):%- '$workdir'/\1:%g' $dc

# start
cd "$workdir"
[ -f "$dc" -a -f "credentials.json" ] && docker-compose up -d nadeko || echo "Please ensure the $dc and credentials.json files exist and are correct before starting the bot!"
```

### Explanations

The script will:

- create the nadeko's directory
- download the [docker-compose.yml](https://gitlab.com/Kwoth/nadekobot/raw/1.9/docker-compose.yml) that describe how to run the containers
- convert the docker-compose.yml from developer configuration to end-user configuration
- edit the docker-compose.yml to specify full path instead of relative path
- create and start the containers

By default, the bot will automatically restart with the Docker daemon with the instruction `restart: unless-stopped`.

### If things goes south

You can see what's going on with the following command. This is very recommanded the first time you run the bot or if something goes south to help you diagnose the issue:

```bash
docker-compose logs -ft --tail=50 nadeko
```

## Watchtower

Watchtower is a small utility that monitors Docker containers and updates them if a new release is available on the upstream. It can be used to automatically update Nadeko.

You will find a configuration sample on the `docker-compose.yml`. You just have to remove the comment symbol and then `docker-compose up -d watchtower` to spin it up.

WARNING: as the docker.sock is mount into this container, this means in fact that this container have a full control not only on all containers but also on your computer. You can read more [here](https://www.projectatomic.io/blog/2015/08/why-we-dont-let-non-root-users-run-docker-in-centos-fedora-or-rhel/)

## Additional informations

If you have any issues with the docker setup, you can request some assistance in the in #help channel on our [Discord server](https://discord.nadeko.bot/), but indicate you are using the docker. As a last resort, you can ping the maintainer @Veovis in the #help channel.

For information about configuring your bot or its functionality, please check the [documentation](https://nadekobot.readthedocs.io/en/latest).
