## Docker guide with digitalocean

#####Prerequisites:
- Digital ocean account (you can use my [reflink][reflink] to support the project and get 10$ after you register)
- [PuTTY][PuTTY] 
- A bot account - follow this [guide][guide]
- $5
- Common sense

#####Guide
- Click on the create droplet button
![img](http://i.imgur.com/g2ayOcC.png)

- Pick one click apps and select docker on 14.04 

![img](http://imgur.com/065Xkme.png)

- Pick any droplet size you want (5$ will work ok-ish on a few servers)
- Pick location closest to your discord server's location
- Pick a hostname  
![img](http://imgur.com/ifPKB6p.png)

- Click create 

You will get an email from DigitalOcean with your credentials now.

Open putty and type ip adress **you got in your email** with port 22  

![img](http://imgur.com/Mh5ehsh.png)

- Console will open and you will be prompted for a username, type `root`.  
- Type in the password you got in the email.  
- Confirm the password you just typed in.  
- Type in the new password.  
- Confirm new password.  

- When you are successfully logged in, type   
`docker run --name nadeko -v /nadeko:/config uirel/nadeko`

- Wait for it to download and at one point it is going to start throwing errors due to `credentials.json` being empty  
- CTRL+C to exit that  
- Type `docker stop nadeko`  
- Type `nano /nadeko/credentials.json` and type in your `credentials`  
- CTRL+X then CTRL+Y to save  
- Type `docker start nadeko`  
- Type `docker logs -f nadeko` to see the console output

**Your bot is running, enjoy! o/**

*When you want to update the bot, just type `docker restart nadeko` as it always downloads latest prerelease*

[reflink]: http://m.do.co/c/46b4d3d44795/
[PuTTY]: http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html
[guide]: http://discord.kongslien.net/guide.html
