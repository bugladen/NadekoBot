## Docker guide with digitalocean

#####Prerequisites:
- Digital ocean account (you can use my reflink to support the project and get 10$ after you register http://m.do.co/c/46b4d3d44795/ )
- Putty (get it here http://www.chiark.greenend.org.uk/~sgtatham/putty/download.html)
- A bot account - (follow http://discord.kongslien.net/guide.html)
- Common sense

Click on the create droplet button
![img](http://i.imgur.com/g2ayOcC.png)

pick one click apps and select docker on 14.04 

![img](http://imgur.com/065Xkme.png)

- pick any droplet size you want (5$ will work ok-ish on a few servers)
- pick location closest to your discord server's location
- Pick a hostname  
![img](http://imgur.com/ifPKB6p.png)

- click create 

You will get an email from digitalocean with your creds now.

Open putty and type ip adress **you got in your email** with port 22  

![img](http://imgur.com/Mh5ehsh.png)

console will open and you will be prompted for a username, type `root`  
type in the password you got in the email  
confirm the password you just typed in  
type in the new password  
confirm new password  

when you are successfully logged in, type   
`docker run --name nadeko -v /nadeko:/config uirel/nadeko`

wait for it to download and at one point it is going to start throwing errors due to credentials.json being empty  
CTRL+C to exit that  
type `docker stop nadeko`  
type `nano /nadeko/credentials.json` and type in your credentials  
CTRL+X then CTRL+Y to save  
type `docker start nadeko`  

Your bot is running, enjoy

*When you want to update the bot, just type `docker restart nadeko` as it always downloads latest prerelease*
