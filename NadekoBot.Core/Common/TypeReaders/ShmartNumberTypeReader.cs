using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Core.Services;

namespace NadekoBot.Core.Common.TypeReaders
{
    public class ShmartNumberTypeReader : NadekoTypeReader<ShmartNumber>
    {
        public ShmartNumberTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            _context = context;
            _services = services;
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(input))
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Input is empty.");

            var i = input.Trim().ToLowerInvariant();

            if (TryHandlePercentage(services, context.User.Id, i, out var num))
                return TypeReaderResult.FromSuccess(new ShmartNumber(num, i));
            try
            {
                var expr = new NCalc.Expression(i, NCalc.EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += EvaluateParam;
                var lon = (long)(decimal.Parse(expr.Evaluate().ToString()));
                return TypeReaderResult.FromSuccess(new ShmartNumber(lon, input));
            }
            catch(Exception ex)
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, ex.Message);
            }
        }

        private void EvaluateParam(string name, NCalc.ParameterArgs args)
        {
            if (name.ToLowerInvariant() == "all")
            {
            }

            switch (name.ToLowerInvariant())
            {
                case "pi":
                    args.Result = Math.PI;
                    break;
                case "e":
                    args.Result = Math.E;
                    break;
                case "all":
                case "allin":
                    args.Result = Cur;
                    break;
                case "half":
                    args.Result = Cur / 2;
                    break;
                case "max":
                    args.Result = Max;
                    break;
                default:
                    break;
            }
        }

        private static readonly Regex percentRegex = new Regex(@"^((?<num>100|\d{1,2})%)$");
        private ICommandContext _context;
        private IServiceProvider _services;

        private long savedCur = -1;
        private long Cur
        {
            get
            {
                if (savedCur == -1)
                {
                    var _db = (DbService)_services.GetService(typeof(DbService));
                    long cur;
                    using (var uow = _db.UnitOfWork)
                    {
                        cur = uow.DiscordUsers.GetUserCurrency(_context.User.Id);
                        uow.Complete();
                    }
                    return savedCur = cur;
                }
                return savedCur;
            }
        }

        private long savedMax = -1;
        private long Max
        {
            get
            {
                if (savedMax == -1)
                {
                    var _bc = (IBotConfigProvider)_services.GetService(typeof(IBotConfigProvider));
                    savedMax = _bc.BotConfig.MaxBet;
                }
                return savedMax == 0
                    ? savedCur
                    : Max;
            }
        }

        private bool TryHandlePercentage(IServiceProvider services, ulong userId, string input, out long num)
        {
            num = 0;
            var m = percentRegex.Match(input);
            if(m.Captures.Count != 0)
            {
                if (!long.TryParse(m.Groups["num"].ToString(), out var percent))
                    return false;

                num = (long)(Cur* (percent / 100.0f));
                return true;
            }
            return false;
        }
    }
}
