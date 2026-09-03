namespace Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string message)
        : base(message)
    {
    }
}