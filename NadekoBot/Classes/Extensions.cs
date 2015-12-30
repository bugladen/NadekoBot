using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Discord.Commands;
using Discord;
using Discord.Legacy;

namespace NadekoBot
{
    public static class Extensions
    {
        public static string Scramble(this string word) {

            var letters = word.ToArray();
            for (int i = 0; i < letters.Length; i++)
            {
                if (i % 3 == 0)
                {
                    continue;
                }

                if (letters[i] != ' ')
                    letters[i] = '_';
            }
            return "`"+string.Join(" ", letters)+"`";
        }

        /// <summary>
        /// Sends a message to the channel from which this command is called.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task<Message> Send(this CommandEventArgs e, string message) 
            => await e.Channel.SendMessage(message);

        /// <summary>
        /// Sends a message to the channel from which MessageEventArg came.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task Send(this MessageEventArgs e, string message)
        {
            await e.Channel.SendMessage(message);
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this Channel c, string message)
        {
            await c.SendMessage(message);
        }

        /// <summary>
        /// Sends a private message to this user.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this User u, string message)
        {
            await u.SendMessage(message);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this CommandEventArgs e, string message)
        {
            await e.Send(e.User.Mention + " " + message);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this MessageEventArgs e, string message)
        {
            await e.Send(e.User.Mention + " " + message);
        }

        /// <summary>
        /// Randomizes element order in a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (T element in source) {
                action(element);
            }
        }
    }
}
