Placeholders are used in Quotes, Custom Reactions, Greet/bye messages, playing statuses, and a few other places.  

They can be used to make the message more user friendly, generate random numbers or pictures, etc...  

Some features have their own specific placeholders which are noted in that feature's command help. Some placeholders are not available in certain features because they don't make sense there.

### Here is a list of the usual placeholders:  
- `%mention%` - Mention the bot  
- `%shardid%` - Shard id
- `%server%` - Server name  
- `%sid%` - Server Id  
- `%channel%` - Channel mention  
- `%chname%` - Channel mention
- `%cid%` - Channel Id  
- `%user%` - User mention
- `%id%` or `%uid%` -  User Id
- `%userfull%` - Username#discriminator
- `%userdiscrim%` - discriminator (for example 1234)
- `%rngX-Y%` - Replace X and Y with the range (for example `%rng5-10%` - random between 5 and 10)
- `%time%` - Bot time
- `%server_time%` - Time on this server, set with `.timezone` command
- `%target%` - Used only in custom reactions, it shows the part of the message after the trigger

**If you're using placeholders in embeds, don't use %user% and %mention% in titles, footers and field names. They will not show properly.**

![img](http://i.imgur.com/lNNNfs1.png)
