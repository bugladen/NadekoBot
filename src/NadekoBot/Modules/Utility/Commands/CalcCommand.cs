using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    [Group]
    public partial class Utility
    {
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public static async Task Calculate(IUserMessage msg, [Remainder] string expression)
        {
            try
            {
                var expr = new NCalc.Expression(expression, NCalc.EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += Expr_EvaluateParameter;
                var result = expr.Evaluate();
                await msg.Reply(string.Format("⚙ `{0}`", expr.Error ?? result));
            }
            catch (Exception e)
            {
                await msg.Reply($"Failed to evaluate: {e.Message} ");
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

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CalcOps(IUserMessage msg)
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
