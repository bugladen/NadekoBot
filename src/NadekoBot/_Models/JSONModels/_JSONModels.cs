// ReSharper disable InconsistentNaming

using System.Diagnostics;

namespace NadekoBot.Classes.JSONModels
{
    public class Credentials
    {
        public string Token { get; set; } = "";
        public string ClientId { get; set; } = "170254782546575360";
        public ulong BotId { get; set; } = 1231231231231;
        public ulong[] OwnerIds { get; set; } = { 123123123123, 5675675679845 };
        public string GoogleAPIKey { get; set; } = "";
        public string SoundCloudClientID { get; set; } = "";
        public string MashapeKey { get; set; } = "";
        public string LOLAPIKey { get; set; } = "";
        public string TrelloAppKey { get; set; } = "";
        public string CarbonKey { get; set; } = "";
        public string OsuAPIKey { get; set; } = "";
    }
    [DebuggerDisplay("{items[0].id.playlistId}")]
    public class YoutubePlaylistSearch
    {
        public YtPlaylistItem[] items { get; set; }
    }
    public class YtPlaylistItem
    {
        public YtPlaylistId id { get; set; }
    }
    public class YtPlaylistId
    {
        public string kind { get; set; }
        public string playlistId { get; set; }
    }
    [DebuggerDisplay("{items[0].id.videoId}")]
    public class YoutubeVideoSearch
    {
        public YtVideoItem[] items { get; set; }
    }
    public class YtVideoItem
    {
        public YtVideoId id { get; set; }
    }
    public class YtVideoId
    {
        public string kind { get; set; }
        public string videoId { get; set; }
    }
    public class PlaylistItemsSearch
    {
        public string nextPageToken { get; set; }
        public PlaylistItem[] items { get; set; }
    }
    public class PlaylistItem
    {
        public YtVideoId contentDetails { get; set; }
    }

    #region wikpedia example
    //    {
    //    "batchcomplete": true,
    //    "query": {
    //        "normalized": [
    //            {
    //                "from": "u3fn92fb32f9yb329f32",
    //                "to": "U3fn92fb32f9yb329f32"
    //            }
    //        ],
    //        "pages": [
    //            {
    //                "ns": 0,
    //                "title": "U3fn92fb32f9yb329f32",
    //                "missing": true,
    //                "contentmodel": "wikitext",
    //                "pagelanguage": "en",
    //                "pagelanguagehtmlcode": "en",
    //                "pagelanguagedir": "ltr",
    //                "fullurl": "https://en.wikipedia.org/wiki/U3fn92fb32f9yb329f32",
    //                "editurl": "https://en.wikipedia.org/w/index.php?title=U3fn92fb32f9yb329f32&action=edit",
    //                "canonicalurl": "https://en.wikipedia.org/wiki/U3fn92fb32f9yb329f32"
    //            }
    //        ]
    //    }
    //}
    #endregion

    public class WikipediaApiModel
    {
        public WikipediaQuery Query { get; set; }
    }

    public class WikipediaQuery
    {
        public WikipediaPage[] Pages { get; set; }
    }

    public class WikipediaPage
    {
        public bool Missing { get; set; } = false;
        public string FullUrl { get; set; }
    }

    public class WoWJoke
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public override string ToString() => $"`{Question}`\n\n**{Answer}**";
    }
}

//{
// "kind": "youtube#searchListResponse",
// "etag": "\"kiOs9cZLH2FUp6r6KJ8eyq_LIOk/hCJTmyH_v57mh_MvnUFSTHfjzBs\"",
// "nextPageToken": "CAEQAA",
// "regionCode": "RS",
// "pageInfo": {
//  "totalResults": 4603,
//  "resultsPerPage": 1
// },
// "items": [
//  {
//   "kind": "youtube#searchResult",
//   "etag": "\"kiOs9cZLH2FUp6r6KJ8eyq_LIOk/iD1S35mk0xOfwTB_8lpPZ9u-Vzc\"",
//   "id": {
//    "kind": "youtube#playlist",
//    "playlistId": "PLs_KC2CCxJVMfOBnIyW5Kbu_GciNiYNAI"
//   },
//   "snippet": {
//    "publishedAt": "2016-04-14T11:35:29.000Z",
//    "channelId": "UCMLwm18Qa20L2L-HGpgC3jQ",
//    "title": "Popular Videos - Otorimonogatari & mousou express",
//    "description": "",
//    "thumbnails": {
//     "default": {
//      "url": "https://i.ytimg.com/vi/2FeptLky2mU/default.jpg",
//      "width": 120,
//      "height": 90
//     },
//     "medium": {
//      "url": "https://i.ytimg.com/vi/2FeptLky2mU/mqdefault.jpg",
//      "width": 320,
//      "height": 180
//     },
//     "high": {
//      "url": "https://i.ytimg.com/vi/2FeptLky2mU/hqdefault.jpg",
//      "width": 480,
//      "height": 360
//     }
//    },
//    "channelTitle": "Otorimonogatari - Topic",
//    "liveBroadcastContent": "none"
//   }
//  }
// ]
//}