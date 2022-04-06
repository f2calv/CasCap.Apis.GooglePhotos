namespace CasCap.Exceptions;

public class GooglePhotosException : Exception
{
    public GooglePhotosException() { }

    public GooglePhotosException(Error error)
        : base(error is not null && error.error is not null && error.error.message is not null ? error.error.message : "unknown")
    {
    }
}