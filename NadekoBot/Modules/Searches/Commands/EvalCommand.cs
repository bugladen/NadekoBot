using Discord.Commands;
using Mathos.Parser;
using NadekoBot.Classes;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands
{
    class CalcCommand : DiscordCommand
    {
        public CalcCommand(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "calculate")
                .Alias(Module.Prefix + "calc")
                .Description($"Evaluate a mathematical expression. | {Prefix}calc 1+1")
                .Parameter("expression", ParameterType.Unparsed)
                .Do(EvalFunc());
        }


        private CustomParser parser = new CustomParser();
        private Func<CommandEventArgs, Task> EvalFunc() => async e =>
        {
            string expression = e.GetArg("expression")?.Trim();
            if (string.IsNullOrWhiteSpace(expression))
            {
                return;
            }
            string answer = Evaluate(expression);
            if (answer == null)
            {
                await e.Channel.SendMessage($"Expression {expression} failed to evaluate");
                return;
            }
            await e.Channel.SendMessage($"⚙ `{answer}`");
        };

        private string Evaluate(string expression)
        {
            //check for factorial
            expression = Regex.Replace(expression, @"\d+!", x => x.Value + "0");
            try
            {
                string result = parser.Parse(expression).ToString();
                return result;
            }
            catch (OverflowException)
            {
                return $"Overflow error on {expression}";
            }
            catch (FormatException)
            {
                return $"\"{expression}\" was not formatted correctly";
            }
        }



        class CustomParser : MathParser
        {
            public CustomParser() : base()
            {
                OperatorList.Add("!");
                OperatorAction.Add("!", (x, y) => Factorial(x));
            }

            static decimal Factorial(decimal x)
            {
                decimal y = x - 1;
                while (y > 0)
                {
                    x = x * y--;
                }
                return x;
            }
        }


    }
}
