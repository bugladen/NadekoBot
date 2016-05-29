using NadekoBot.Classes;
using System;
using Mathos.Parser;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Searches.Commands
{
    class EvalCommand : DiscordCommand
    {
        public EvalCommand(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "evaluate")
                .Alias(Module.Prefix + "eval")
                .Description("Evaluate a mathematical expression")
                .Parameter("expression", ParameterType.Unparsed)
                .Do(EvalFunc());
        }


        private CustomParser parser = new CustomParser();
        private Func<CommandEventArgs, Task> EvalFunc() => async e =>
        {
            string expression = e.GetArg("expression")?.Trim();
            if (string.IsNullOrWhiteSpace(expression))
            {
                await e.Channel.SendMessage("Must give expression");
                return;
            }
            string answer = evaluate(expression);
            if (answer == null)
            {
                await e.Channel.SendMessage($"Expression {expression} failed to evaluate");
                return;
            }
            await e.Channel.SendMessage($"`result: {answer}`");
        };

        private string evaluate(string expression)
        {
            //check for factorial
            expression = Regex.Replace(expression, @"\d+!", x => x.Value + "0");
            try
            {
                string result = parser.Parse(expression).ToString();
                return result;
            }
            catch (OverflowException e)
            {
                return $"Overflow error on {expression}";
            }
            catch (FormatException e)
            {
                return $"\"{expression}\" was not formatted correctly";
            }
        }

        

        class CustomParser : MathParser
        {
            public CustomParser() : base()
            {
                OperatorList.Add("!");
                OperatorList.Add("_");
                OperatorAction.Add("!", (x, y) => factorial(x));
                OperatorAction.Add("_", (x, y) => 10.130M);
            }

            static decimal factorial(decimal x)
            {
                decimal y = x-1;
                while (y >0)
                {
                    x = x * y--;
                }
                return x;
            }
        }


    }
}
