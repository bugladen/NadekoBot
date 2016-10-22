###Upgrading Nadeko from an older release

- Follow the Windows Guide/Linux Guide/OS X Guide linked on the left.
- Navigate to your old `Nadeko` folder and copy `credentials.json` and the `/data/` folder.
- Paste this into the new Nadeko's `/NadekoBot/src/NadekoBot/` folder.
- If it asks you to overwrite files, it is fine to do so.
- Now launch new Nadeko as the guide describes.
- In any channel, run the `.migratedata` command - nadeko will now migrate your old data.
- Restart nadeko and everything should work as expected!
