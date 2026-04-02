namespace OPS.Domain.Exceptions;

public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string entity, Guid id)
        : base($"{entity} with ID '{id}' was not found.")
    {
        ErrorCode = $"{entity.ToUpperInvariant()}_NOT_FOUND";
    }
}
