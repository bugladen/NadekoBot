using Discord.Commands;
using System.Threading.Tasks;
using NadekoBot.Modules.Permissions;

namespace NadekoBot.TypeReaders
{
    /// <summary>
    /// Used instead of bool for more flexible keywords for true/false only in the permission module
    /// </summary>
    public class PermissionActionTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
        {
            input = input.ToUpperInvariant();
            switch (input)
            {
                case "1":
                case "T":
                case "TRUE":
                case "ENABLE":
                case "ENABLED":
                case "ALLOW":
                case "PERMIT":
                case "UNBAN":
                    return Task.FromResult(TypeReaderResult.FromSuccess(PermissionAction.Enable));
                case "0":
                case "F":
                case "FALSE":
                case "DENY":
                case "DISABLE":
                case "DISABLED":
                case "DISALLOW":
                case "BAN":
                    return Task.FromResult(TypeReaderResult.FromSuccess(PermissionAction.Disable));
                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Did not receive a valid boolean value"));
            }
        }
    }
}
