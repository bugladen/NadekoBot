using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands.Models
{
    public class TimeZoneResult
    {
        public double DstOffset { get; set; }
        public double RawOffset { get; set; }

        //public string TimeZoneId { get; set; }
        public string TimeZoneName { get; set; }
    }

    public class GeolocationResult
    {

        public class GeolocationModel
        {
            public class GeometryModel
            {
                public class LocationModel
                {
                    public float Lat { get; set; }
                    public float Lng { get; set; }
                }

                public LocationModel Location { get; set; }
            }

            [JsonProperty("formatted_address")]
            public string FormattedAddress { get; set; }
            public GeometryModel Geometry { get; set; }
        }

        public GeolocationModel[] results;
    }
}
