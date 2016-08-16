namespace NadekoBot.Services
{
    public interface IBotCredentials
    {
        string ClientId { get; }
        string Token { get; }
        string GoogleApiKey { get; }

    }
}
