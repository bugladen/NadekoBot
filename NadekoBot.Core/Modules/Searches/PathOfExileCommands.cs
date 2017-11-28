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
using NadekoBot.Common.Attributes;
using Newtonsoft.Json;
using AngleSharp;
using AngleSharp.Dom.Html;

namespace NadekoBot.Modules.Searches
{
	public partial class Searches
	{
		[Group]
		public class PathOfExileCommands : NadekoSubmodule
		{
			private const string _poeURL = "https://www.pathofexile.com/character-window/get-characters?accountName=";
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
				var characters = new System.Collections.Generic.List<Account>();

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
						characters = JsonConvert.DeserializeObject<System.Collections.Generic.List<Account>>(res);
					}
					catch (Exception ex)
					{
						await ReplyErrorLocalized("something_went_wrong").ConfigureAwait(false);
						_log.Warn(ex);
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

			// League names: Standard, Hardcore, tmpstandard, tmphardcore
			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileCurrency(string leagueName, string currencyName)
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
						var reqString = $"http://poe.ninja/api/Data/GetCurrencyOverview?league={leagueName}";
						var obj = JObject.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false));

						foreach (var currency in obj["lines"])
						{
							string currencyTypeName = currency["currencyTypeName"].ToString();

							if (currencyTypeName == currencyName)
							{
								string chaosEquivalent = currency["chaosEquivalent"].ToString();

								var embed = new EmbedBuilder().WithColor(NadekoBot.OkColor)
															  .WithTitle($"{leagueName} Currency Exchange")
															  .AddField(efb => efb.WithName("Currency Type").WithValue(currencyTypeName).WithIsInline(true))
															  .AddField(efb => efb.WithName("Chaos Equivalent").WithValue(chaosEquivalent).WithIsInline(true));

								var sent = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
							}
						}
					}
					catch (Exception ex)
					{
						await ReplyErrorLocalized("something_went_wrong").ConfigureAwait(false);
						_log.Warn(ex);
					}
				}
			}

			[NadekoCommand, Usage, Description, Aliases]
			public async Task PathOfExileItem(string itemName)
			{
				if (string.IsNullOrWhiteSpace(itemName))
					return;

				var parsedName = itemName.Replace(" ", "_");
				var fullQueryLink = "https://pathofexile.gamepedia.com/" + parsedName;

				var config = Configuration.Default.WithDefaultLoader();
				var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

				var imageElem = document.QuerySelector("span.item-box > a.image > img");
				var imageUrl = ((IHtmlImageElement)imageElem)?.Source ?? "http://icecream.me/uploads/870b03f36b59cc16ebfe314ef2dde781.png";

				var sb = new System.Text.StringBuilder();
				var itemStats = document.QuerySelector("span.item-box.-unique");
				var names = itemStats.QuerySelector("span.header.-double").InnerHtml.Split("<br>");

				var itemInformation = itemStats.QuerySelector("span.item-stats");

				var itemImplicits = itemStats.QuerySelector("span.group").InnerHtml.Replace("<br>", "\n");
				itemImplicits = Regex.Replace(itemImplicits, "<.*?>", String.Empty);

				var itemMods = itemInformation.QuerySelector("span.group.-mod").InnerHtml.Replace("<br>", "\n");
				itemMods = Regex.Replace(itemMods, "<.*?>", String.Empty);
				
				var flavorText = "";

				if (itemInformation.QuerySelector("span.group.-flavour") != null)
				{
					flavorText = itemInformation.QuerySelector("span.group.-flavour").InnerHtml.Replace("<br>", "\n");
				}

				sb.AppendLine($"{names[1]}\n");
				sb.AppendLine($"{itemImplicits}\n");
				sb.AppendLine($"{itemMods}\n");
				sb.AppendLine($"{flavorText}\n");

				var embed = new EmbedBuilder()
							.WithOkColor()
							.WithTitle($"{names[0]}")
							.WithDescription(sb.ToString())
							.WithImageUrl(imageUrl);

				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
			}
		}

		public class Account
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("league")]
			public string League { get; set; }

			[JsonProperty("classId")]
			public int ClassId { get; set; }

			[JsonProperty("ascendancyClass")]
			public int AscendancyClass { get; set; }

			[JsonProperty("class")]
			public string Class { get; set; }

			[JsonProperty("level")]
			public int Level { get; set; }
		}
	}
}
