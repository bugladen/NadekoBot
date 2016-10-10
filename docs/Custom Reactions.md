##Custom Reactions
<p><strong>Every time you add or remove a custom reaction you will need to restart your bot with either <code>.restart</code> if you are hosting or Nadeko is on VPS, or <code>.die</code> if Nadeko is on Droplet</strong></p>
<h3>Important</h3>
<ul style="list-style-type:disc">
<li><strong><code>.acr</code>,&nbsp;<code>.dcr</code>,&nbsp;and <code>.ecr</code> Require you to be Bot Owner</strong></li>
<li>Adding multiple commands of the same name will make Nadeko randomly select one of the command's responses</li>
</ul>
###Commands and Their Use
<table>
<tr>
<th>Command Name</th>
<th>Description</th>
<th>Example</th>
</tr><tr>
<td align="center"><code>.acr</code></td>
<td>Adds a Custom Reaction</td>
<td><code>.acr Hello Hi!</code></td>
</tr><tr>
<td align="center"><code>.dcr</code></td>
<td>Deletes an entire Custom Reaction or a Specified Response Index Number</td>
<td><code>.dcr "command name"</code>&nbsp;or&nbsp;<code>.dcr "command name" 3</code></td>
</tr><tr>
<td align="center"><code>.lcr</code></td>
<td>Lists a specified page of Custom Commands</td>
<td><code>.lcr Number</code>&nbsp;i.e&nbsp;<code>.lcr 2</code></td>
</tr><tr>
<td align="center"><code>.scr</code></td>
<td>Shows all responses to a command.&nbsp;Index Number will be in brackets <code>[&nbsp;]</code></td>
<td><code>.scr Hello</code></td>
</tr><tr>
<td align="center"><code>.ecr</code></td>
<td>Edit a custom reaction, needs: reaction's name, index to change, and new multiword response.</td>
<td><code>.ecr&nbsp;Hello&nbsp;3&nbsp;What's up</code></td></table>
<h4>Now that we know the commands let's take a look at the arguments and placeholders available for <code>.acr</code>,</h4>
<p><code>.addcustreact</code>&nbsp;(<code>.acr</code>)&nbsp;takes two arguments:
<dl>
<dt><strong>●&nbsp;First, The name of the command; this directly follows the <code>.acr</code></strong></dt>
<dd>-&nbsp;If your command name is more than 1 word use quotation marks;<br> i.e <del><code>.acr Nice weather sure is</code></del> > <code>.acr "Nice weather" sure is</code></dd>
<dd>-&nbsp;Now everytime someone says "Nice weather" the bot will respond with, "sure is"</dd>
<dt><strong>●&nbsp;Next, The Response, this follows the name of the command.</strong><dt>
<dd>-&nbsp;i.e <code>.acr "Nice weather"</code><b><i><code>sure is</code></i></b></dd></dl><p>
###And finally on to the Placeholders
<p>There are currently four placeholders, which we will be looking at, with many more to come in the future.<p>
<table>
<tr>
<th>Placeholder</th>
<th>How the placeholder works</th>
<th>Examples</th>
</tr><tr>
<td align="center"><code>%mention%</code></td>
<td>The&nbsp;<code>%mention%</code>&nbsp;placeholder is triggered when you type <code>@botname</code></td>
<td><code>.acr "%mention% Hello" Hello!</code> > User input: @botname Hello | Bot Replies: Hello!</td>
</tr><tr>
<td align="center"><code>%target%</code></td>
<td>The <code>%target%</code> placeholder is used to make Nadeko Mention another person</td>
<td><code>.acr "%mention% Hello" %target% Hi!</code> > User inputs: "@botname Hello @somebody" 
 Bot replies: "Hi @somebody"</td>
 </tr><tr>
 <td align="center"><code>%user%</code></td>
 <td>The <code>%user%</code> placeholder mentions the person who said the command</td>
 <td><code>.acr "Who am i" You are %user%!</code></td>
 </tr><tr>
 <td align="center"><code>%rng%</code></td>
 <td>The <code>%rng%</code> generates a random number</td>
 <td><code>.acr Random %rng%</code>
 </tr></table>
 
 Thanks to Nekai for being creative. <3

