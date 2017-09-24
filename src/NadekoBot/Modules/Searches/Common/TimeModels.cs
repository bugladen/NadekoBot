using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Common
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
