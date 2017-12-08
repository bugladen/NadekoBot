using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Searches.Common;
using Newtonsoft.Json;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace NadekoBot.Modules.Searches
{
	public partial class Searches
	{
		[Group]
		public class PathOfExileCommands : NadekoSubmodule
		{
			private const string _poeURL = "https://www.pathofexile.com/character-window/get-characters?accountName=";
			private const string _ponURL = "http://poe.ninja/api/Data/GetCurrencyOverview?league=";
			private const string _pogsURL = "http://pathofexile.gamepedia.com/api.php?action=opensearch&search=";
			private const string _pogURL = "https://pathofexile.gamepedia.com/api.php?action=browsebysubject&format=json&subject=";
			private const string _pogiURL = "https://pathofexile.gamepedia.com/api.php?action=query&prop=imageinfo&iiprop=url&format=json&titles=File:";
			private const string _profileURL = "https://www.pathofexile.com/account/view-profile/";

			private readonly DiscordSocketClient _client;

			public PathOfExileCommands(DiscordSocketClient client)
			{
				_client = client;
			}

			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExile(string usr, int page = 1)
			{
				if (--page < 0)
					return;

				var channel = (ITextChannel)Context.Channel;
				var characters = new List<Account>();

				if (string.IsNullOrWhiteSpace(usr))
				{
					await channel.SendErrorAsync("Please provide an account name.").ConfigureAwait(false);
					return;
				}

				using (var http = new HttpClient())
				{
					try
					{
						var res = await http.GetStringAsync($"{_poeURL}{usr}").ConfigureAwait(false);
						characters = JsonConvert.DeserializeObject<List<Account>>(res);
					}
					catch
					{
						var embed = new EmbedBuilder()
										.WithDescription(GetText("account_not_found"))
										.WithErrorColor();
						await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
						return;
					}
				}

				await Context.Channel.SendPaginatedConfirmAsync(_client, page, (curPage) =>
				{
					var embed = new EmbedBuilder()
									.WithAuthor(eau => eau.WithName($"Characters on {usr}'s account")
									.WithUrl($"{_profileURL}{usr}")
									.WithIconUrl("https://web.poecdn.com/image/favicon/ogimage.png"))
									.WithOkColor();

					var tempList = characters.Skip(curPage * 9).Take(9).ToList();

					if (characters.Count == 0)
					{
						return embed.WithDescription("This account has no characters.");
					}
					else
					{
						var sb = new System.Text.StringBuilder();
						sb.AppendLine($"```{"#", -5}{"Character Name",-23}{"League",-10}{"Class",-13}{"Level",-3}");
						for (int i = 0; i < tempList.Count; i++)
						{
							var character = tempList[i];

							sb.AppendLine($"#{i + 1 + (curPage * 9), -4}{character.Name, -23}{character.League, -10}{character.Class, -13}{character.Level, -3}");
						}

						sb.AppendLine("```");
						embed.WithDescription(sb.ToString());

						return embed;
					}
				}, characters.Count, 9, true);
			}

			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileLeagues()
			{
				var leagues = new List<Leagues>();
				using (var http = new HttpClient())
				{
					try
					{
						var res = await http.GetStringAsync("http://api.pathofexile.com/leagues?type=main&compact=1").ConfigureAwait(false);
						leagues = JsonConvert.DeserializeObject<List<Leagues>>(res);
					}
					catch
					{
						var eembed = new EmbedBuilder()
										.WithDescription(GetText("leagues_not_found"))
										.WithErrorColor();
						await Context.Channel.EmbedAsync(eembed).ConfigureAwait(false);
						return;
					}

					var embed = new EmbedBuilder()
								.WithAuthor(eau => eau.WithName($"Path of Exile Leagues")
								.WithUrl("https://www.pathofexile.com")
								.WithIconUrl("https://web.poecdn.com/image/favicon/ogimage.png"))
								.WithOkColor();

					foreach (var item in leagues)
					{
						Console.WriteLine(item.Id);
					}

					var sb = new System.Text.StringBuilder();
					sb.AppendLine($"```{"#",-5}{"League Name",-23}");
					for (int i = 0; i < leagues.Count; i++)
					{
						var league = leagues[i];

						sb.AppendLine($"#{i + 1,-4}{league.Id,-23}");
					}
					sb.AppendLine("```");

					embed.WithDescription(sb.ToString());

					await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
				}
			}
			
			// TODO: Include shorthands for various currencies?
			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileCurrency(string leagueName, string currencyName, string convertName = "Chaos Orb")
			{
				var channel = (ITextChannel)Context.Channel;

				if (string.IsNullOrWhiteSpace(leagueName))
				{
					await channel.SendErrorAsync("Please provide league name.").ConfigureAwait(false);
					return;
				}
				if (string.IsNullOrWhiteSpace(currencyName))
				{
					await channel.SendErrorAsync("Please provide currency name.").ConfigureAwait(false);
					return;
				}

				using (var http = new HttpClient())
				{
					try
					{
						var res = $"{_ponURL}{leagueName}";
						var obj = JObject.Parse(await http.GetStringAsync(res).ConfigureAwait(false));

						float chaosEquivalent = 0.0F;
						float conversionEquivalent = 0.0F;

						//	poe.ninja API does not include a "chaosEquivalent" property for Chaos Orbs.
						if (currencyName == "Chaos Orb")
						{
							chaosEquivalent = 1.0F;
						}
						else
						{
							var currencyInput = obj["lines"].Values<JObject>()
															.Where(i => i["currencyTypeName"].Value<string>() == currencyName)
															.FirstOrDefault();
							chaosEquivalent = float.Parse(currencyInput["chaosEquivalent"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
						}
						
						if (convertName == "Chaos Orb")
						{
							conversionEquivalent = 1.0F;
						}
						else
						{
							var currencyOutput = obj["lines"].Values<JObject>()
															.Where(i => i["currencyTypeName"].Value<string>() == convertName)
															.FirstOrDefault();
							conversionEquivalent = float.Parse(currencyOutput["chaosEquivalent"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
						}
												
						var embed = new EmbedBuilder().WithAuthor(eau => eau.WithName($"{leagueName} Currency Exchange")
													  .WithUrl("http://poe.ninja")
													  .WithIconUrl("https://web.poecdn.com/image/favicon/ogimage.png"))
													  .AddField(efb => efb.WithName("Currency Type").WithValue(currencyName).WithIsInline(true))
													  .AddField(efb => efb.WithName($"{convertName} Equivalent").WithValue(chaosEquivalent / conversionEquivalent).WithIsInline(true))
													  .WithOkColor();

						await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
					}
					catch
					{
						var embed = new EmbedBuilder()
										.WithDescription(GetText("ninja_not_found"))
										.WithErrorColor();
						await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
					}
				}
			}

			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileItem(string itemName)
			{
				var channel = (ITextChannel)Context.Channel;

				if (string.IsNullOrWhiteSpace(itemName))
				{
					await channel.SendErrorAsync("Please provide an item name.").ConfigureAwait(false);
					return;
				}
				
				using (var http = new HttpClient())
				{
					try
					{
						var res = $"{_pogsURL}{itemName}";
						var itemNameJson = JArray.Parse($"{await http.GetStringAsync(res).ConfigureAwait(false)}");					
						var parsedName = itemNameJson[1][0].ToString().Replace(" ", "_");
						var fullQueryLink = "https://pathofexile.gamepedia.com/" + parsedName;

						res = $"{_pogURL}{parsedName}";
						var obj = JObject.Parse(await http.GetStringAsync(res).ConfigureAwait(false));

						var infoboxProperty = obj["query"]["data"].Values<JObject>()
															  .Where(i => i["property"].Value<string>() == "Has_infobox_HTML")
															  .FirstOrDefault();
						var infobox = infoboxProperty["dataitem"][0]["item"].ToString();

						// Vessel of Vinktar has multiple variants; separate pages for each but same image.
						if (parsedName.Contains("Vessel_of_Vinktar"))
						{
							parsedName = Regex.Replace(parsedName, @" ?\(.*?\)", string.Empty);
							parsedName = parsedName.Remove(parsedName.Length - 1);
						}

						res = $"{_pogiURL}{parsedName}_inventory_icon.png";
						var img = JObject.Parse(await http.GetStringAsync(res).ConfigureAwait(false));
						
						var imgContainer = img.Descendants()
													.OfType<JProperty>()
													.Where(p => p.Name == "url")
													.Select(p => p.Parent)
													.ToList();

						var imgUrl = imgContainer[0]["url"].ToString();						
						var imageUrl = imgUrl ?? "http://icecream.me/uploads/870b03f36b59cc16ebfe314ef2dde781.png";

						var parser = new HtmlParser();
						infobox = Regex.Replace(infobox, @"[\[\]']+", "");
						var document = parser.Parse(infobox);

						var itemStats = document.QuerySelector("span.item-box.-unique");
						var names = itemStats.QuerySelector("span.header.-double").InnerHtml.Split("<br>");
						var itemInformation = itemStats.QuerySelector("span.item-stats");
						var itemImplicits = itemStats.QuerySelector("span.group").InnerHtml.Replace("<br>", "\n");
						var itemModsList = itemInformation.QuerySelectorAll("span.group.-mod");

						var itemModsBuilder = new System.Text.StringBuilder();
						foreach (var span in itemModsList)
						{
							itemModsBuilder.Append(span.InnerHtml.Replace("<br>", "\n"));
							itemModsBuilder.Append("\n");
						}
						
						var flavorText = String.Empty;
						if (itemInformation.QuerySelector("span.group.-flavour") != null)
						{
							flavorText = "*" + itemInformation.QuerySelector("span.group.-flavour").InnerHtml.Replace("<br>", "\n") + "*\n";
						}

						itemImplicits = Regex.Replace(itemImplicits, "<.*?>", String.Empty);
						var itemMods = Regex.Replace(itemModsBuilder.ToString(), "<.*?>", String.Empty);

						var itemInfobox = new System.Text.StringBuilder();
						itemInfobox.AppendLine($"{names[1]}\n");
						itemInfobox.AppendLine($"{itemImplicits}\n");
						itemInfobox.AppendLine($"{itemModsBuilder}");
						itemInfobox.AppendLine($"{flavorText}");

						var embed = new EmbedBuilder()
									.WithAuthor(eau => eau.WithName($"{itemName.ToTitleCase()}")
									.WithUrl(fullQueryLink)
									.WithIconUrl("https://web.poecdn.com/image/favicon/ogimage.png"))
									.WithDescription(itemInfobox.ToString())
									.WithImageUrl(imageUrl)
									.WithOkColor();

						await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
					}
					catch
					{
						var eembed = new EmbedBuilder()
										.WithDescription(GetText("pog_not_found"))
										.WithErrorColor();
						await Context.Channel.EmbedAsync(eembed).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
