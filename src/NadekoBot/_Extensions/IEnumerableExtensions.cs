using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Distinct<T, U>(this IEnumerable<T> data, Func<T, U> getKey) =>
            data.GroupBy(x => getKey(x))
                .Select(x => x.First());
    }
}
