namespace EdaDemo.Messaging;

public sealed class RetryExhaustedException : Exception
{
    public RetryExhaustedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
