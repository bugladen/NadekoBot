using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class WaifuInfo : DbEntity
    {
        public int WaifuId { get; set; }
        public DiscordUser Waifu { get; set; }

        public int? ClaimerId { get; set; }
        public DiscordUser Claimer { get; set; }

        public int? AffinityId { get; set; }
        public DiscordUser Affinity { get; set; }

        public int Price { get; set; }

        public override string ToString()
        {
            var claimer = "no one";
            var status = "";

            var waifuUsername = Waifu.Username.TrimTo(20);
            var claimerUsername = Claimer?.Username.TrimTo(20);

            if (Claimer != null)
            {
                claimer = $"{ claimerUsername }#{Claimer.Discriminator}";
            }
            if (AffinityId == null)
            {
                status = $"... but {waifuUsername}'s heart is empty";
            }
            else if (AffinityId == ClaimerId)
            {
                status = $"... and {waifuUsername} likes {claimerUsername} too <3";
            }
            else {
                status = $"... but {waifuUsername}'s heart belongs to {Affinity.Username.TrimTo(20)}#{Affinity.Discriminator}";
            }
            return $"**{waifuUsername}#{Waifu.Discriminator}** - claimed by **{claimer}**\n\t{status}";
        }
    }
}