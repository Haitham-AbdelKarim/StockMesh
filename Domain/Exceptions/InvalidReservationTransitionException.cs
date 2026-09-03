namespace Domain.Exceptions;

public class InvalidReservationTransitionException : DomainException
{
    public InvalidReservationTransitionException(string message)
        : base(message)
    {
    }
}