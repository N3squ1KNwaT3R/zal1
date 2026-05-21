namespace OrderFlow.Console.Services;

public class CurrencyServiceException : Exception
{
    public int StatusCode { get; }

    public CurrencyServiceException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
