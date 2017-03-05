using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using NadekoBot.Attributes;
using NadekoBot.Modules.Utility.Models;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        //[Group]
        //public class PatreonCommands : NadekoSubmodule
        //{
        //    [NadekoCommand, Usage, Description, Aliases]
        //    [RequireContext(ContextType.Guild)]
        //    public async Task ClaimPatreonRewards([Remainder] string arg)
        //    {
        //        var pledges = await GetPledges2();
        //    }

        //    private static async Task<Pledge[]> GetPledges()
        //    {
        //        var pledges = new List<Pledge>();
        //        using (var http = new HttpClient())
        //        {
        //            http.DefaultRequestHeaders.Clear();
        //            http.DefaultRequestHeaders.Add("Authorization", "Bearer " + NadekoBot.Credentials.PatreonAccessToken);
        //            var data = new PatreonData()
        //            {
        //                Links = new Links()
        //                {
        //                    Next = "https://api.patreon.com/oauth2/api/campaigns/334038/pledges"
        //                }
        //            };
        //            do
        //            {
        //                var res =
        //                    await http.GetStringAsync(data.Links.Next)
        //                        .ConfigureAwait(false);
        //                data = JsonConvert.DeserializeObject<PatreonData>(res);
        //                pledges.AddRange(data.Data);
        //            } while (!string.IsNullOrWhiteSpace(data.Links.Next));
        //        }
        //        return pledges.Where(x => string.IsNullOrWhiteSpace(x.Attributes.declined_since)).ToArray();
        //    }

        //    private static async Task<Pledge[]> GetPledges2()
        //    {
        //        var pledges = new List<Pledge>();
        //        using (var http = new HttpClient())
        //        {
        //            http.DefaultRequestHeaders.Clear();
        //            http.DefaultRequestHeaders.Add("Authorization", "Bearer " + NadekoBot.Credentials.PatreonAccessToken);
        //            var data = new PatreonData()
        //            {
        //                Links = new Links()
        //                {
        //                    Next = "https://api.patreon.com/oauth2/api/current_user/campaigns?include=pledges"
        //                }
        //            };
        //            do
        //            {
        //                var res =
        //                    await http.GetStringAsync(data.Links.Next)
        //                        .ConfigureAwait(false);
        //                data = JsonConvert.DeserializeObject<PatreonData>(res);
        //                pledges.AddRange(data.Data);
        //            } while (!string.IsNullOrWhiteSpace(data.Links.Next));
        //        }
        //        return pledges.Where(x => string.IsNullOrWhiteSpace(x.Attributes.declined_since)).ToArray();
        //    }
        //}
    }
}
