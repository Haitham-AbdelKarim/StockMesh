namespace Application.Exceptions;

public sealed class OwnerNotOnboardedException : Exception
{
    public OwnerNotOnboardedException(string message)
        : base(message)
    {
    }
}