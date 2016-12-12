NadekoBot Docker Image
======================

This is a Docker setup for NadekoBot

Usage
-----

The docker image creates a volume to hold the NadekoBot application to be able to quickly edit the credentials and data.

To initialize NadekoBot, run this:

`docker run --name=NadekoBot -t reiuiji/nadekobot:stable`

If you want to create a separate volume to handle the data for NadekoBot run the following.

```Nadeko_DATA="NadekoBot-data"
docker volume create --name $Nadeko_DATA
docker run --name=NadekoBot -v $Nadeko_DATA:/opt -t reiuiji/nadekobot:stable
```

If you want to link the volumes you can link credentials.json and data individual.

`-v /path/to/credentials.json:/opt/NadekoBot/src/NadekoBot/credentials.json`

`-v /path/to/data:/opt/NadekoBot/src/NadekoBot/bin/Release/netcoreapp1.0/data`

If you want to use the latest developmental version then change tag from "stable" to "dev".

`docker run --name=NadekoBot -t reiuiji/nadekobot:dev`

Build
-----

There are two versions of the docker container for stable and dev branches. Select dev if you want latest unstable build or select stable for latest stable build.

Once you selected the build process you want now enter that directory and build the docker image.

`docker build -t reiuiji/nadekobot .`

