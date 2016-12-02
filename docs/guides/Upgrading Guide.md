##Upgrading Nadeko from an older release

**If you have NadekoBot 0.9x**

- Follow the [Windows Guide](http://nadekobot.readthedocs.io/en/latest/guides/Windows%20Guide/)/[Linux Guide](http://nadekobot.readthedocs.io/en/latest/guides/Linux%20Guide/)/[OS X Guide](http://nadekobot.readthedocs.io/en/latest/guides/OSX%20Guide/) and install **NadekoBot 1.0**.
- Navigate to your **old** `Nadeko` folder and copy `credentials.json` file and the `data` folder.
- Paste them into **NadekoBot 1.0** `/NadekoBot/src/NadekoBot/` folder.
- If it asks you to overwrite files, it is fine to do so.
- Next launch your **new** Nadeko as the guide describes, if it is not already running.
- In any channel, run the `.migratedata` [command](http://nadekobot.readthedocs.io/en/latest/Commands%20List/) and Nadeko will start migrating your old data.
- Once that is done **restart** Nadeko and everything should work as expected!
