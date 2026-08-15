namespace OpenRestoReservationBot.Infrastructure;

public sealed class BotRequestException : Exception
{
    public BotRequestException()
    {
    }

    public BotRequestException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public BotRequestException(string message)
        : base(message)
    {
    }

    public BotRequestException(int statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public BotRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public int StatusCode { get; }
}
