using MediatR;

namespace Application.Features.Lines.Commands
{
    public record UpdateLineCommand(int Id, string LineName, bool IsActive) : IRequest<bool>;
}