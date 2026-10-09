namespace Application.Features.Scanning.Commands
{
    public sealed class BatchValidationException(string field, string message) : Exception(message)
    {
        public string Field { get; } = field;
    }
}
