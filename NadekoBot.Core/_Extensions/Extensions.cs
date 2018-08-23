using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Common;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class Extensions
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        // https://github.com/SixLabors/ImageSharp/tree/master/samples/AvatarWithRoundedCorner
        public static void ApplyRoundedCorners(this Image<Rgba32> img, float cornerRadius)
        {
            var corners = BuildCorners(img.Width, img.Height, cornerRadius);
            // now we have our corners time to draw them
            img.Mutate(x => x.Fill(new GraphicsOptions(true)
            {
                BlenderMode = PixelBlenderMode.Src // enforces that any part of this shape that has color is punched out of the background
            },
            Rgba32.Transparent, corners));
        }

        public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // first create a square
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // then cut out of the square a circle so we are left with a corner
            var cornerToptLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // corner is now a corner shape positions top left
            //lets make 3 more positioned correctly, we can do that by translating the orgional around the center of the image
            var center = new Vector2(imageWidth / 2, imageHeight / 2);

            float rightPos = imageWidth - cornerToptLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerToptLeft.Bounds.Height + 1;

            // move it across the width of the image - the width of the shape
            var cornerTopRight = cornerToptLeft.RotateDegree(90).Translate(rightPos, 0);
            var cornerBottomLeft = cornerToptLeft.RotateDegree(-90).Translate(0, bottomPos);
            var cornerBottomRight = cornerToptLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerToptLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }

        /// <summary>
        /// First 10 characters of teh bot token.
        /// </summary>
        public static string RedisKey(this IBotCredentials bc)
        {
            return bc.Token.Substring(0, 10);
        }

        public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> replacementFn)
        {
            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in regex.Matches(input))
            {
                sb.Append(input, lastIndex, match.Index - lastIndex)
                  .Append(await replacementFn(match).ConfigureAwait(false));

                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }

        public static void ThrowIfNull<T>(this T o, string name) where T : class
        {
            if (o == null)
                throw new ArgumentNullException(nameof(name));
        }

        public static ConcurrentDictionary<TKey, TValue> ToConcurrent<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict)
            => new ConcurrentDictionary<TKey, TValue>(dict);

        public static bool IsAuthor(this IMessage msg, IDiscordClient client) =>
            msg.Author?.Id == client.CurrentUser.Id;

        public static string RealSummary(this CommandInfo cmd, string prefix) => string.Format(cmd.Summary, prefix);
        public static string RealRemarks(this CommandInfo cmd, string prefix) => string.Join(" or ", JsonConvert.DeserializeObject<string[]>(cmd.Remarks).Select(x => Format.Code(string.Format(x, prefix))));

        public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage)
        {
            if (lastPage != null)
                return embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}"));
            else
                return embed.WithFooter(efb => efb.WithText(curPage.ToString()));
        }

        public static EmbedBuilder WithOkColor(this EmbedBuilder eb) =>
            eb.WithColor(NadekoBot.OkColor);

        public static EmbedBuilder WithErrorColor(this EmbedBuilder eb) =>
            eb.WithColor(NadekoBot.ErrorColor);

        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Func<SocketReaction, Task> reactionAdded, Func<SocketReaction, Task> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = _ => Task.CompletedTask;

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }

        public static HttpClient AddFakeHeaders(this HttpClient http)
        {
            AddFakeHeaders(http.DefaultRequestHeaders);
            return http;
        }

        public static void AddFakeHeaders(this HttpHeaders dict)
        {
            dict.Clear();
            dict.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            dict.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
        }

        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000).ConfigureAwait(false);
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });
            return msg;
        }

        public static ModuleInfo GetTopLevelModule(this ModuleInfo module)
        {
            while (module.Parent != null)
            {
                module = module.Parent;
            }
            return module;
        }

        public static void AddRange<T>(this HashSet<T> target, IEnumerable<T> elements) where T : class
        {
            foreach (var item in elements)
            {
                target.Add(item);
            }
        }

        public static void AddRange<T>(this ConcurrentHashSet<T> target, IEnumerable<T> elements) where T : class
        {
            foreach (var item in elements)
            {
                target.Add(item);
            }
        }

        public static double UnixTimestamp(this DateTime dt) => dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        public static async Task<IEnumerable<IGuildUser>> GetMembersAsync(this IRole role) =>
            (await role.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false)).Where(u => u.RoleIds.Contains(role.Id)) ?? Enumerable.Empty<IGuildUser>();

        public static string ToJson<T>(this T any, Formatting formatting = Formatting.Indented) =>
            JsonConvert.SerializeObject(any, formatting);

        public static MemoryStream ToStream(this Image<Rgba32> img, IImageFormat format = null)
        {
            var imageStream = new MemoryStream();
            if (format?.Name == "GIF")
            {
                img.SaveAsGif(imageStream);
            }
            else
            {
                img.SaveAsPng(imageStream, new PngEncoder() { CompressionLevel = 9 });
            }
            imageStream.Position = 0;
            return imageStream;
        }

        /// <summary>
        /// returns an IEnumerable with randomized element order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            // Thanks to @Joe4Evr for finding a bug in the old version of the shuffle
            using (var provider = RandomNumberGenerator.Create())
            {
                var list = items.ToList();
                var n = list.Count;
                while (n > 1)
                {
                    var box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    var k = (boxSum % n);
                    n--;
                    var value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
                return list;
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> elems, Action<T> exec)
        {
            foreach (var elem in elems)
            {
                exec(elem);
            }
            return elems;
        }

        public static Stream ToStream(this IEnumerable<byte> bytes, bool canWrite = false)
        {
            var ms = new MemoryStream(bytes as byte[] ?? bytes.ToArray(), canWrite);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public static IEnumerable<IRole> GetRoles(this IGuildUser user) =>
            user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);

        public static async Task<IMessage> SendMessageToOwnerAsync(this IGuild guild, string message)
        {
            var ownerPrivate = await (await guild.GetOwnerAsync().ConfigureAwait(false)).GetOrCreateDMChannelAsync()
                                .ConfigureAwait(false);

            return await ownerPrivate.SendMessageAsync(message).ConfigureAwait(false);
        }

        public static Image<Rgba32> Merge(this IEnumerable<Image<Rgba32>> images)
        {
            return images.Merge(out _);
        }
        public static Image<Rgba32> Merge(this IEnumerable<Image<Rgba32>> images, out IImageFormat format)
        {
            format = ImageFormats.Png;
            void DrawFrame(Image<Rgba32>[] imgArray, Image<Rgba32> imgFrame, int frameNumber)
            {
                var xOffset = 0;
                for (int i = 0; i < imgArray.Length; i++)
                {
                    var frame = imgArray[i].Frames.CloneFrame(frameNumber % imgArray[i].Frames.Count);
                    imgFrame.Mutate(x => x.DrawImage(GraphicsOptions.Default, frame, new Point(xOffset, 0)));
                    xOffset += imgArray[i].Bounds().Width;
                }
            }

            var imgs = images.ToArray();
            int frames = images.Max(x => x.Frames.Count);

            var width = imgs.Sum(img => img.Width);
            var height = imgs.Max(img => img.Height);
            var canvas = new Image<Rgba32>(width, height);
            if (frames == 1)
            {
                DrawFrame(imgs, canvas, 0);
                return canvas;
            }

            format = ImageFormats.Gif;
            for (int j = 0; j < frames; j++)
            {
                using (var imgFrame = new Image<Rgba32>(width, height))
                {
                    DrawFrame(imgs, imgFrame, j);

                    var frameToAdd = imgFrame.Frames.First();
                    frameToAdd.MetaData.DisposalMethod = SixLabors.ImageSharp.Formats.Gif.DisposalMethod.RestoreToBackground;
                    canvas.Frames.AddFrame(frameToAdd);
                }
            }
            canvas.Frames.RemoveFrame(0);
            return canvas;
        }

        public static void LogAndReset(this Stopwatch sw, string name = "")
        {
            _log.Info(name + " | " + sw.Elapsed.TotalSeconds.ToString("F2"));
            sw.Reset();
        }

        public static bool IsImage(this HttpResponseMessage msg)
        {
            if (msg.Content.Headers.ContentType.MediaType != "image/png"
                                && msg.Content.Headers.ContentType.MediaType != "image/jpeg"
                                && msg.Content.Headers.ContentType.MediaType != "image/gif")
            {
                return false;
            }
            return true;
        }

        public static long? GetImageSize(this HttpResponseMessage msg)
        {
            if (msg.Content.Headers.ContentLength == null)
            {
                return null;
            }

            return msg.Content.Headers.ContentLength / 1.MB();
        }



        public static IEnumerable<Type> LoadFrom(this IServiceCollection collection, Assembly assembly)
        {
            // list of all the types which are added with this method
            List<Type> addedTypes = new List<Type>();

            Type[] allTypes;
            try
            {
                // first, get all types in te assembly
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                _log.Warn(ex);
                return Enumerable.Empty<Type>();
            }
            // all types which have INService implementation are services
            // which are supposed to be loaded with this method
            // ignore all interfaces and abstract classes
            var services = new Queue<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract
#if GLOBAL_NADEKO
                        && x.GetTypeInfo().GetCustomAttribute<NoPublicBotAttribute>() == null
#endif
                            )
                    .ToArray());

            // we will just return those types when we're done instantiating them
            addedTypes.AddRange(services);

            // get all interfaces which inherit from INService
            // as we need to also add a service for each one of interfaces
            // so that DI works for them too
            var interfaces = new HashSet<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && x.GetTypeInfo().IsInterface));

            // keep instantiating until we've instantiated them all
            while (services.Count > 0)
            {
                var serviceType = services.Dequeue(); //get a type i need to add

                if (collection.FirstOrDefault(x => x.ServiceType == serviceType) != null) // if that type is already added, skip
                    continue;

                //also add the same type 
                var interfaceType = interfaces.FirstOrDefault(x => serviceType.GetInterfaces().Contains(x));
                if (interfaceType != null)
                {
                    addedTypes.Add(interfaceType);
                    collection.AddSingleton(interfaceType, serviceType);
                }
                else
                {
                    collection.AddSingleton(serviceType, serviceType);
                }
            }

            return addedTypes;
        }
    }
}