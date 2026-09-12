namespace Application.Exceptions;

public sealed class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}