###Upgrading Nadeko from an older release

- Navigate to your old nadeko folder
- Follow the correct install process for your operating system, linked on the left hand side. Nadeko has some new dependencies, so make sure you get these as guided!
- For upgrading, you can use your old credentials file without any issues and most data can also be migrated
- Copy both the data folder and `credentials.json` from your old Nadeko installation, to the following folder
- `NadekoBot\src\NadekoBot` - This may ask if you want to overwrite some files, this is perfectly fine to do.
- Now launch the new Nadeko as the guide describes - it should now start.
- In any channel, run the `.migratedata` command - nadeko will now attempt to migrate your old data to nadeko's new storage system
- Your data should now have been migrated succesfully, restart nadeko and everything should be working as expected
