**After you add/delete custom commands, you should restart the bot. (either by typing `.restart` if you are on WINDOWS or just manually restarting if you prefer/are on other platform)**

`.acr` and `.dcr` require you to be a bot owner.

`.acr`
Adds a new custom command. **If you add multiple commands with the same name, it will pick random one.** First argument is the name, second one is the response. For example `.acr hello hi`. Now the bot will reply `hi` whenever someone types `hello`. For more than 1 word command, wrap it in `"`.
For example: `.acr "hello there" hi there` - now it will print "hi there" whenever someone types "hello there". Currently you can add this placeholders which will get replaced with appropriate text:  
`%mention%` - replaces it with bot mention  
`%user%` - replaces it with the user runner's mention  
`%target%` - replaces it with a mention of another person from within the original message  
`%rng%` replaces it with a random number  
for example: `.acr "%mention% hello" Hello %user%`  
(we will add much more of these over time)


`.dcr "command name" (optional index)`
Deletes either whole custom command and all its responses or a single command's response via an index (if you have multiple responses for the same command).
For example: `.dcr "hi there"` or `.dcr "hi there" 1`. You can get an index by using `.lcr [page number]`

`.lcr [number]` 
Prints a list of custom reactions. Paginated. (for example: `.lcr 1` or `.lcr 4`)
