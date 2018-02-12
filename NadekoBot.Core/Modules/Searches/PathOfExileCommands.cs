using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Searches.Common;
using Newtonsoft.Json;
using AngleSharp.Parser.Html;
using NadekoBot.Modules.Searches.Services;

namespace NadekoBot.Modules.Searches
{
	public partial class Searches
	{
		[Group]
		public class PathOfExileCommands : NadekoSubmodule<SearchesService>
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
			public async Task PathOfExile(string usr, string league = "", int page = 1)
			{
				if (--page < 0)
					return;

				if (string.IsNullOrWhiteSpace(usr))
				{
					await Context.Channel.SendErrorAsync("Please provide an account name.").ConfigureAwait(false);
					return;
				}

				var characters = new List<Account>();

				try
				{
					var res = await _service.Http.GetStringAsync($"{_poeURL}{usr}").ConfigureAwait(false);
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
				
				if (league != String.Empty)
				{
					characters.RemoveAll(c => c.League != league);
				}

				await Context.SendPaginatedConfirmAsync(page, (curPage) =>
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
							
							sb.AppendLine($"#{i + 1 + (curPage * 9), -4}{character.Name, -23}{ShortLeagueName(character.League), -10}{character.Class, -13}{character.Level, -3}");
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

				try
				{
					var res = await _service.Http.GetStringAsync("http://api.pathofexile.com/leagues?type=main&compact=1").ConfigureAwait(false);
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
			
			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileCurrency(string leagueName, string currencyName, string convertName = "Chaos Orb")
			{
				if (string.IsNullOrWhiteSpace(leagueName))
				{
					await Context.Channel.SendErrorAsync("Please provide league name.").ConfigureAwait(false);
					return;
				}
				if (string.IsNullOrWhiteSpace(currencyName))
				{
					await Context.Channel.SendErrorAsync("Please provide currency name.").ConfigureAwait(false);
					return;
				}

				var cleanCurrency = ShortCurrencyName(currencyName);
				var cleanConvert = ShortCurrencyName(convertName);
				
				try
				{
					var res = $"{_ponURL}{leagueName}";
					var obj = JObject.Parse(await _service.Http.GetStringAsync(res).ConfigureAwait(false));

					float chaosEquivalent = 0.0F;
					float conversionEquivalent = 0.0F;

					//	poe.ninja API does not include a "chaosEquivalent" property for Chaos Orbs.
					if (cleanCurrency == "Chaos Orb")
					{
						chaosEquivalent = 1.0F;
					}
					else
					{
						var currencyInput = obj["lines"].Values<JObject>()
														.Where(i => i["currencyTypeName"].Value<string>() == cleanCurrency)
														.FirstOrDefault();
						chaosEquivalent = float.Parse(currencyInput["chaosEquivalent"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
					}

					if (cleanConvert == "Chaos Orb")
					{
						conversionEquivalent = 1.0F;
					}
					else
					{
						var currencyOutput = obj["lines"].Values<JObject>()
														.Where(i => i["currencyTypeName"].Value<string>() == cleanConvert)
														.FirstOrDefault();
						conversionEquivalent = float.Parse(currencyOutput["chaosEquivalent"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
					}

					var embed = new EmbedBuilder().WithAuthor(eau => eau.WithName($"{leagueName} Currency Exchange")
						.WithUrl("http://poe.ninja")
						.WithIconUrl("https://web.poecdn.com/image/favicon/ogimage.png"))
						.AddField(efb => efb.WithName("Currency Type").WithValue(cleanCurrency).WithIsInline(true))
						.AddField(efb => efb.WithName($"{cleanConvert} Equivalent").WithValue(chaosEquivalent / conversionEquivalent).WithIsInline(true))
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

			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileItem(string itemName)
			{
				if (string.IsNullOrWhiteSpace(itemName))
				{
					await Context.Channel.SendErrorAsync("Please provide an item name.").ConfigureAwait(false);
					return;
				}

				try
				{
					var res = $"{_pogsURL}{itemName}";
					var itemNameJson = JArray.Parse($"{await _service.Http.GetStringAsync(res).ConfigureAwait(false)}");
					var parsedName = itemNameJson[1][0].ToString().Replace(" ", "_");
					var fullQueryLink = "https://pathofexile.gamepedia.com/" + parsedName;

					res = $"{_pogURL}{parsedName}";
					var obj = JObject.Parse(await _service.Http.GetStringAsync(res).ConfigureAwait(false));

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
					var img = JObject.Parse(await _service.Http.GetStringAsync(res).ConfigureAwait(false));

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
			
			Dictionary<string, string> currencyDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{"Chaos Orb", "Chaos Orb" },
					{"Orb of Alchemy", "Orb of Alchemy" },
					{"Jeweller's Orb", "Jeweller's Orb" },
					{"Exalted Orb", "Exalted Orb" },
					{"Mirror of Kalandra", "Mirror of Kalandra" },
					{"Vaal Orb", "Vaal Orb" },
					{"Orb of Alteration", "Orb of Alteration" },
					{"Orb of Scouring", "Orb of Scouring" },
					{"Divine Orb", "Divine Orb" },
					{"Orb of Annulment", "Orb of Annulment" },
					{"Master Cartographer's Sextant", "Master Cartographer's Sextant" },
					{"Journeyman Cartographer's Sextant", "Journeyman Cartographer's Sextant" },
					{"Apprentice Cartographer's Sextant", "Apprentice Cartographer's Sextant" },
					{"Blessed Orb", "Blessed Orb" },
					{"Orb of Regret", "Orb of Regret" },
					{"Gemcutter's Prism", "Gemcutter's Prism" },
					{"Glassblower's Bauble", "Glassblower's Bauble" },
					{"Orb of Fusing", "Orb of Fusing" },
					{"Cartographer's Chisel", "Cartographer's Chisel" },
					{"Chromatic Orb", "Chromatic Orb" },
					{"Orb of Augmentation", "Orb of Augmentation" },
					{"Blacksmith's Whetstone", "Blacksmith's Whetstone" },
					{"Orb of Transmutation", "Orb of Transmutation" },
					{"Armourer's Scrap", "Armourer's Scrap" },
					{"Scroll of Wisdom", "Scroll of Wisdom" },
					{"Regal Orb", "Regal Orb" },
					{"Chaos", "Chaos Orb" },
					{"Alch", "Orb of Alchemy" },
					{"Alchs", "Orb of Alchemy" },
					{"Jews", "Jeweller's Orb" },
					{"Jeweller", "Jeweller's Orb" },
					{"Jewellers", "Jeweller's Orb" },
					{"Jeweller's", "Jeweller's Orb" },
					{"X", "Exalted Orb" },
					{"Ex", "Exalted Orb" },
					{"Exalt", "Exalted Orb" },
					{"Exalts", "Exalted Orb" },
					{"Mirror", "Mirror of Kalandra" },
					{"Mirrors", "Mirror of Kalandra" },
					{"Vaal", "Vaal Orb" },
					{"Alt", "Orb of Alteration" },
					{"Alts", "Orb of Alteration" },
					{"Scour", "Orb of Scouring" },
					{"Scours", "Orb of Scouring" },
					{"Divine", "Divine Orb" },
					{"Annul", "Orb of Annulment" },
					{"Annulment", "Orb of Annulment" },
					{"Master Sextant", "Master Cartographer's Sextant" },
					{"Journeyman Sextant", "Journeyman Cartographer's Sextant" },
					{"Apprentice Sextant", "Apprentice Cartographer's Sextant" },
					{"Blessed", "Blessed Orb" },
					{"Regret", "Orb of Regret" },
					{"Regrets", "Orb of Regret" },
					{"Gcp", "Gemcutter's Prism" },
					{"Glassblowers", "Glassblower's Bauble" },
					{"Glassblower's", "Glassblower's Bauble" },
					{"Fusing", "Orb of Fusing" },
					{"Fuses", "Orb of Fusing" },
					{"Fuse", "Orb of Fusing" },
					{"Chisel", "Cartographer's Chisel" },
					{"Chisels", "Cartographer's Chisel" },
					{"Chance", "Orb of Chance" },
					{"Chances", "Orb of Chance" },
					{"Chrome", "Chromatic Orb" },
					{"Chromes", "Chromatic Orb" },
					{"Aug", "Orb of Augmentation" },
					{"Augmentation", "Orb of Augmentation" },
					{"Augment", "Orb of Augmentation" },
					{"Augments", "Orb of Augmentation" },
					{"Whetstone", "Blacksmith's Whetstone" },
					{"Whetstones", "Blacksmith's Whetstone" },
					{"Transmute", "Orb of Transmutation" },
					{"Transmutes", "Orb of Transmutation" },
					{"Armourers", "Armourer's Scrap" },
					{"Armourer's", "Armourer's Scrap" },
					{"Wisdom Scroll", "Scroll of Wisdom" },
					{"Wisdom Scrolls", "Scroll of Wisdom" },
					{"Regal", "Regal Orb" },
					{"Regals", "Regal Orb" }
				};

			private string ShortCurrencyName(string str)
			{
				if (currencyDictionary.ContainsValue(str))
				{
					return str;
				}

				var currency = currencyDictionary[str];
								
				return currency;
			}
			
			private string ShortLeagueName(string str)
			{
				var league = Regex.Replace(str, "Hardcore", "HC", RegexOptions.IgnoreCase);

				return league;
			}
		}
	}
}
