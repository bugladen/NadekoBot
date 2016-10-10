##Custom Reactions
<h3>Important</h3>
<ul style="list-style-type:disc">
<li>For modifying <strong>global</strong> custom reactions, the ones which will work across all the servers your bot is connected to, you <strong>must</strong> be a Bot Owner.<br> You must also use the commands for adding, deleting and listing these reactions in a direct message with the bot.</li>
<li>For modifying <strong>local</strong> custom reactions, the ones which will only work on the server that they are added on, require you to have the <strong>Administrator</strong> permission.<br> You must also use the commands for adding, deleting and listing these reactions in the server you want the custom reactions to work on.</li>
</ul>
###Commands and Their Use
<table>
	<tr>
		<th>Command Name</th>
		<th>Description</th>
		<th>Example</th>
	</tr>
	<tr>
		<td align="center"><code>.acr</code></td>
		<td>Add a custom reaction with a trigger and a response. Running this command in a server requries the Administrator permission. Running this command in DM is Bot Owner only, and adds a new global custom reaction. Guide here: <a href="http://nadekobot.readthedocs.io/en/1.0/Custom%20Reactions/">http://nadekobot.readthedocs.io/en/1.0/Custom Reactions/</a></td>
		<td><code>.acr "hello" Hi there, %user%!</code></td>
	</tr>
	<tr>
		<td align="center"><code>.lcr</code></td>
		<td>Lists a page of global or server custom reactions (15 reactions per page). Running this command in a DM will list the global custom reactions, while running it in a server will list that server's custom reactions.</td>
		<td><code>.lcr 1</code></td>
	</tr>
	<tr>
		<td align="center"><code>.dcr</code></td>
		<td>Deletes a custom reaction based on the provided index. Running this command in a server requires the Administrator permission. Running this command in DM is Bot Owner only, and will delete a global custom reaction.</td>
		<td><code>.dcr 5</code></td>
	</tr>
</table>

<h4>Now that we know the commands let's take a look at an example of adding a command with <code>.acr</code>,</h4>
<p><code>.acr "Nice Weather" It sure is, %user%!</code></p>
<p>This command can be split into two different arguments:<ul><li>The trigger <code>"Nice Weather"</code></li><li>And the response, <code>It sure is, %user%!</code></li></ul></p>
<p>Because we wanted the trigger to be more than one word, we had to wrap it with quotation marks, <code>"Like this"</code> otherwise, only the first word would have been recognised as the trigger, and the second word would have been recognised as part of the response.</p>
<p>There's no special requirement for the formatting of the response, so we could just write it in exactly the same way we want it to respond, albeit with a placeholder - which will be explained in this next section</p>

###Placeholders!
<p>There are currently three different placeholders which we will look at, with more placeholders potentially coming in the future.</p>

<table>
	<tr>
		<th>Placeholder</th>
		<th>How the placeholder works</th>
		<th>Examples</th>
	</tr>
	<tr>
		<td align="center"><code>%mention%</code></td>
		<td>The&nbsp;<code>%mention%</code>&nbsp;placeholder is triggered when you type <code>@botname</code> - It's important to note that if you've given the bot a nickname, this trigger won't work!</td>
		<td><code>.acr "%mention% Hello" Hello!</code> > User input: @botname Hello | Bot Replies: Hello!</td>
	</tr>
	<!-- <tr>
		<td align="center"><code>%target%</code></td>
		<td>The <code>%target%</code> placeholder is used to make Nadeko Mention another person</td>
		<td><code>.acr "Hello" %target%, Hi!</code> > User inputs: "Hello @somebody!" 
		Bot replies: "@somebody, Hi!"</td>
	</tr> -->
	<tr>
		<td align="center"><code>%user%</code></td>
		<td>The <code>%user%</code> placeholder mentions the person who said the command</td>
		<td><code>.acr "Who am I" You are %user%!</code></td>
	</tr>
	<tr>
		<td align="center"><code>%rng%</code></td>
		<td>The <code>%rng%</code> generates a random number between 0 and 10</td>
		<td><code>.acr Random %rng%</code>
	</tr>
</table>
 
 Thanks to Nekai for being creative. <3 <!-- and to fearnlj01, for making it less creative (sorry) -->
