namespace Core.Domain.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
}
