namespace Domain.Exceptions;

public class ConcurrentReservationException : DomainException
{
    public ConcurrentReservationException(string message)
        : base(message)
    {
    }
}