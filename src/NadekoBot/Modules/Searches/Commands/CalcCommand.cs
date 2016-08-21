using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands
{
    public partial class Searches
    {
        public static async Task Calc(IMessage msg, [Remainder] string expression)
        {
            var expr = new NCalc.Expression(expression);
            //expr.EvaluateParameter += delegate (string name, NCalc.ParameterArgs args)
            //{
            //    if (name.ToLowerInvariant() == "pi") args.Result = Math.PI;
            //};
            var result = expr.Evaluate();

            await msg.Reply(string.Format("Your expression evaluated to: {0}", expr.Error ?? result));
        }
    }
    class ExpressionContext
    {
        public double Pi { get; set; } = Math.PI;
    }

}
