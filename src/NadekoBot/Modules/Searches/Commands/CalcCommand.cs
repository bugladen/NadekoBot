using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    [Group]
    public partial class Searches
    {
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public static async Task Calculate(IMessage msg, [Remainder] string expression)
        {
            try
            {
                var expr = new NCalc.Expression(expression, NCalc.EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += Expr_EvaluateParameter;
                var result = expr.Evaluate();
                await msg.Reply(string.Format("Your expression evaluated to: {0}", expr.Error ?? result));
            }
            catch (Exception e)
            {
                await msg.Reply($"Your expression failed to evaluate: {e.Message} ");
            }
        }

        private static void Expr_EvaluateParameter(string name, NCalc.ParameterArgs args)
        {
            switch (name.ToLowerInvariant()) {
                case "pi": args.Result= Math.PI;
                    break;
                case "e": args.Result = Math.E;
                    break;    
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task CalcOperations(IMessage msg)
        {
            StringBuilder builder = new StringBuilder();
            var selection = typeof(Math).GetTypeInfo().GetMethods().Except(typeof(object).GetTypeInfo().GetMethods()).Select(x =>
            {
                var name = x.Name;
                if (x.GetParameters().Any())
                {
                    name += " (" + string.Join(", ", x.GetParameters().Select(y => y.IsOptional ? $"[{y.ParameterType.Name + " " + y.Name }]" : y.ParameterType.Name + " " + y.Name)) + ")";
                }
                return name;
            });
            foreach (var method in selection) builder.AppendLine(method);
            await msg.ReplyLong(builder.ToString());
        }
    }
    class ExpressionContext
    {
        public double Pi { get; set; } = Math.PI;
    }

}
