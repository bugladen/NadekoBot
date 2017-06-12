##Upgrading Nadeko from an older release

**If you have NadekoBot 1.x**

- Follow the [Windows Guide](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/)/[Linux Guide](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/)/[OS X Guide](http://nadekobot.readthedocs.io/en/latest/guides/OSX%20Guide/) and install the latest version of **NadekoBot**.
- Navigate to your **old** `Nadeko` folder and copy your `credentials.json` file and the `data` folder.
- Paste credentials into the **NadekoBot 1.4x+** `C:\Program Files\NadekoBot\system` folder.
- Paste your **old** `Nadeko` data folder into **NadekoBot 1.4x+** `C:\Program Files\NadekoBot\system` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** Nadeko as the guide describes, if it is not already running.


**If you are running Dockerised Nadeko**
- Shutdown your existing container **docker stop nadeko**
- Move you credentials and other files to another folder
- Delete your container **docker rm nadeko**
- Create a new container **docker create --name=nadeko -v /nadeko/:/root/nadeko uirel/nadeko:dev**
- Start the container **docker start nadeko** wait for it to complain about lacking credentials
- Stop the container **docker stop nadeko** open the nadeko folder and replace the crednetials, database and other files with your copies
- Restart the container **docker start nadeko**
